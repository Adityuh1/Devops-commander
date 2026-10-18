using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;
using DevopsCommander.Shared.Validation;

namespace DevopsCommander.Shared.Infrastructure
{
    /// <summary>
    /// Implements durable deployment workflow orchestration with checkpointing,
    /// crash resilience, and configurable automatic step retry policies (Temporal pattern).
    /// </summary>
    public class DurableDeploymentWorkflowEngine : IDeploymentWorkflowEngine
    {
        private readonly IDeploymentStore _store;
        private readonly ITerraformRunner _terraformRunner;
        private readonly ILogger<DurableDeploymentWorkflowEngine> _logger;

        public DurableDeploymentWorkflowEngine(
            IDeploymentStore store,
            ITerraformRunner terraformRunner,
            ILogger<DurableDeploymentWorkflowEngine>? logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _terraformRunner = terraformRunner ?? throw new ArgumentNullException(nameof(terraformRunner));
            _logger = logger ?? NullLogger<DurableDeploymentWorkflowEngine>.Instance;
        }

        public async Task<Result<Guid>> StartWorkflowAsync(
            DeployRequest request,
            WorkflowRetryPolicy? retryPolicy = null,
            CancellationToken cancellationToken = default)
        {
            var validation = DeployRequestValidator.Validate(request);
            if (validation.IsFailure)
            {
                return Result<Guid>.Failure(validation.Error);
            }

            var validReq = validation.Value;
            var deploymentId = validReq.DeploymentId ?? Guid.NewGuid();
            var reqWithId = validReq with { DeploymentId = deploymentId };

            // Initialize and checkpoint workflow state
            var state = new DeploymentState
            {
                Id = deploymentId,
                Request = reqWithId,
                Status = DeployStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            state.LogInfo("WorkflowEngine", "Durable workflow instance initialized.");
            await _store.SaveAsync(state);

            // Execute workflow in background with durable state tracking
            _ = Task.Run(async () =>
            {
                await ExecuteWorkflowLifecycleAsync(state, retryPolicy ?? new WorkflowRetryPolicy(), cancellationToken);
            }, cancellationToken);

            return Result<Guid>.Success(deploymentId);
        }

        public async Task ExecuteWorkflowLifecycleAsync(
            DeploymentState state,
            WorkflowRetryPolicy policy,
            CancellationToken cancellationToken)
        {
            state.Status = DeployStatus.Provisioning;
            state.StartedAt = DateTimeOffset.UtcNow;
            state.LogInfo("WorkflowEngine", $"Workflow started with retry policy (MaxAttempts={policy.MaxAttempts}).");
            await _store.UpdateAsync(state);

            var attempt = 1;
            var delay = policy.InitialInterval;

            while (attempt <= policy.MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    state.LogInfo("WorkflowExecution", $"Executing provisioning step (Attempt {attempt} of {policy.MaxAttempts})...");
                    await _store.UpdateAsync(state);

                    var result = await _terraformRunner.ExecuteAsync(
                        state,
                        async (phase, message) =>
                        {
                            state.LogInfo(phase, message);
                            await _store.UpdateAsync(state);
                        },
                        cancellationToken);

                    if (result.IsSuccess)
                    {
                        state.Complete();
                        state.LogInfo("WorkflowEngine", "Durable workflow execution succeeded.");
                        await _store.UpdateAsync(state);
                        break;
                    }

                    // Attempt failed
                    if (attempt < policy.MaxAttempts)
                    {
                        state.LogWarning("WorkflowRetry", $"Step attempt {attempt} failed: {result.Error}. Retrying in {delay.TotalSeconds:F1}s...");
                        await _store.UpdateAsync(state);

                        await Task.Delay(delay, cancellationToken);
                        delay = TimeSpan.FromTicks((long)(delay.Ticks * policy.BackoffCoefficient));
                        if (delay > policy.MaxInterval) delay = policy.MaxInterval;
                    }
                    else
                    {
                        state.Fail("WorkflowEngine", $"Workflow failed after {attempt} attempts: {result.Error}");
                        await _store.UpdateAsync(state);
                    }
                }
                catch (OperationCanceledException)
                {
                    state.Status = DeployStatus.Cancelled;
                    state.FinishedAt = DateTimeOffset.UtcNow;
                    state.LogWarning("WorkflowEngine", "Workflow execution was cancelled.");
                    await _store.UpdateAsync(state);
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= policy.MaxAttempts)
                    {
                        state.Fail("WorkflowEngine", $"Unexpected error on attempt {attempt}: {ex.Message}", ex.StackTrace);
                        await _store.UpdateAsync(state);
                        return;
                    }

                    state.LogWarning("WorkflowRetry", $"Exception on attempt {attempt}: {ex.Message}. Retrying...");
                    await _store.UpdateAsync(state);
                    await Task.Delay(delay, cancellationToken);
                }

                attempt++;
            }
        }

        public async Task<DeploymentState?> GetWorkflowStateAsync(Guid deploymentId)
        {
            return await _store.GetByIdAsync(deploymentId);
        }
    }
}
