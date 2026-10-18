using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Worker
{
    /// <summary>
    /// Background worker service that dequeues deployment requests,
    /// drives the lifecycle state machine, and executes Terraform provisioning.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDeploymentQueue _queue;
        private readonly IDeploymentStore _store;
        private readonly ITerraformRunner _terraformRunner;

        public Worker(
            ILogger<Worker> logger,
            IDeploymentQueue queue,
            IDeploymentStore store,
            ITerraformRunner terraformRunner)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _terraformRunner = terraformRunner ?? throw new ArgumentNullException(nameof(terraformRunner));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DevopsCommander Worker started listening for deployment jobs.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _queue.DequeueAsync(stoppingToken);
                    if (request == null)
                    {
                        // Queue completed or cancelled
                        break;
                    }

                    await ProcessDeploymentAsync(request, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker shutdown requested. Exiting execution loop.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception processing deployment loop: {Message}", ex.Message);
                }
            }

            _logger.LogInformation("DevopsCommander Worker stopped.");
        }

        public async Task ProcessDeploymentAsync(DeployRequest request, CancellationToken cancellationToken)
        {
            var deploymentId = request.DeploymentId ?? Guid.NewGuid();
            _logger.LogInformation("Starting deployment execution for {ProjectName} ({DeploymentId})", request.ProjectName, deploymentId);

            // 1. Retrieve or initialize deployment state in persistence store
            var state = await _store.GetByIdAsync(deploymentId);
            if (state == null)
            {
                state = new DeploymentState
                {
                    Id = deploymentId,
                    Request = request,
                    Status = DeployStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _store.SaveAsync(state);
            }

            // 2. Transition state to Provisioning
            state.Status = DeployStatus.Provisioning;
            state.StartedAt = DateTimeOffset.UtcNow;
            state.LogInfo("Worker", $"Worker picked up deployment for {request.ProjectName} on environment '{request.Environment}'.");
            await _store.UpdateAsync(state);

            try
            {
                // 3. Execute Terraform runner with real-time log persistence
                var result = await _terraformRunner.ExecuteAsync(
                    state,
                    async (phase, logMessage) =>
                    {
                        state.LogInfo(phase, logMessage);
                        await _store.UpdateAsync(state);
                    },
                    cancellationToken);

                if (result.IsSuccess)
                {
                    // 4a. Mark Completed
                    state.Complete();
                    _logger.LogInformation("Deployment {DeploymentId} completed successfully.", deploymentId);
                }
                else
                {
                    // 4b. Mark Failed
                    state.Fail("Terraform", result.Error);
                    _logger.LogWarning("Deployment {DeploymentId} failed: {Error}", deploymentId, result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                state.Status = DeployStatus.Cancelled;
                state.FinishedAt = DateTimeOffset.UtcNow;
                state.LogWarning("Worker", "Deployment execution was cancelled by system shutdown.");
                _logger.LogWarning("Deployment {DeploymentId} was cancelled.", deploymentId);
            }
            catch (Exception ex)
            {
                state.Fail("Worker", $"Unexpected execution failure: {ex.Message}", ex.StackTrace);
                _logger.LogError(ex, "Fatal error running deployment {DeploymentId}: {Message}", deploymentId, ex.Message);
            }
            finally
            {
                await _store.UpdateAsync(state);
            }
        }
    }
}
