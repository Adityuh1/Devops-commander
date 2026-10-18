using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DevopsCommander.Shared;
using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;
using DevopsCommander.Worker;

namespace DevopsCommander.Tests
{
    public class WorkerLifecycleTests : IDisposable
    {
        private readonly string _testDb;
        private readonly SqliteDeploymentStore _store;
        private readonly ChannelDeploymentQueue _queue;

        public WorkerLifecycleTests()
        {
            _testDb = $"test_worker_{Guid.NewGuid():N}.db";
            _store = new SqliteDeploymentStore($"Data Source={_testDb}");
            _queue = new ChannelDeploymentQueue(100);
        }

        public void Dispose()
        {
            if (File.Exists(_testDb))
            {
                try { File.Delete(_testDb); } catch { }
            }
        }

        private class MockTerraformRunner(bool shouldSucceed, string? errorMessage = null) : ITerraformRunner
        {
            public async Task<Result<bool>> ExecuteAsync(
                DeploymentState state,
                Func<string, string, Task> onLogAsync,
                CancellationToken cancellationToken = default)
            {
                await onLogAsync("TerraformInit", "Initializing mock provider...");
                await onLogAsync("TerraformApply", "Applying simulated cloud changes...");

                return shouldSucceed
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure(errorMessage ?? "Terraform apply exited with code 1");
            }
        }

        [Fact]
        public async Task Worker_ProcessDeployment_OnSuccess_ShouldTransitionToCompleted()
        {
            // Arrange
            var runner = new MockTerraformRunner(shouldSucceed: true);
            var worker = new Worker.Worker(NullLogger<Worker.Worker>.Instance, _queue, _store, runner);

            var req = new DeployRequest
            {
                ProjectName = "checkout-api",
                Environment = "dev",
                Region = "us-east-1",
                InfrastructureType = "container"
            };

            var deploymentId = Guid.NewGuid();
            var reqWithId = req with { DeploymentId = deploymentId };

            // Act
            await worker.ProcessDeploymentAsync(reqWithId, CancellationToken.None);

            // Assert
            var finalState = await _store.GetByIdAsync(deploymentId);
            Assert.NotNull(finalState);
            Assert.Equal(DeployStatus.Completed, finalState.Status);
            Assert.NotNull(finalState.StartedAt);
            Assert.NotNull(finalState.FinishedAt);
            Assert.True(finalState.FinishedAt >= finalState.StartedAt);

            // Check log entries
            Assert.Contains(finalState.ExecutionLog, e => e.Phase == "Worker");
            Assert.Contains(finalState.ExecutionLog, e => e.Phase == "TerraformInit");
            Assert.Contains(finalState.ExecutionLog, e => e.Phase == "TerraformApply");
            Assert.Contains(finalState.ExecutionLog, e => e.Phase == "Pipeline");
        }

        [Fact]
        public async Task Worker_ProcessDeployment_OnFailure_ShouldTransitionToFailed_And_RecordError()
        {
            // Arrange
            var runner = new MockTerraformRunner(shouldSucceed: false, "Quota limit reached");
            var worker = new Worker.Worker(NullLogger<Worker.Worker>.Instance, _queue, _store, runner);

            var req = new DeployRequest
            {
                ProjectName = "failing-service",
                Environment = "production",
                Region = "us-east-1",
                InfrastructureType = "vm"
            };

            var deploymentId = Guid.NewGuid();
            var reqWithId = req with { DeploymentId = deploymentId };

            // Act
            await worker.ProcessDeploymentAsync(reqWithId, CancellationToken.None);

            // Assert
            var finalState = await _store.GetByIdAsync(deploymentId);
            Assert.NotNull(finalState);
            Assert.Equal(DeployStatus.Failed, finalState.Status);
            Assert.Equal("Quota limit reached", finalState.ErrorMessage);
            Assert.NotNull(finalState.FinishedAt);
            Assert.Contains(finalState.ExecutionLog, e => e.Level == "Error");
        }
    }
}
