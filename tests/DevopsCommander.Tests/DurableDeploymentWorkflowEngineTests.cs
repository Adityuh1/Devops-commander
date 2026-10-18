using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DevopsCommander.Shared;
using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Tests
{
    public class DurableDeploymentWorkflowEngineTests : IDisposable
    {
        private readonly string _testDb;
        private readonly SqliteDeploymentStore _store;

        public DurableDeploymentWorkflowEngineTests()
        {
            _testDb = $"test_workflow_{Guid.NewGuid():N}.db";
            _store = new SqliteDeploymentStore($"Data Source={_testDb}");
        }

        public void Dispose()
        {
            if (File.Exists(_testDb))
            {
                try { File.Delete(_testDb); } catch { }
            }
        }

        private class FlakyTerraformRunner : ITerraformRunner
        {
            private readonly int _failTimes;
            public int ExecutionAttempts { get; private set; }

            public FlakyTerraformRunner(int failTimes)
            {
                _failTimes = failTimes;
            }

            public async Task<Result<bool>> ExecuteAsync(
                DeploymentState state,
                Func<string, string, Task> onLogAsync,
                CancellationToken cancellationToken = default)
            {
                ExecutionAttempts++;
                await onLogAsync("Terraform", $"Execution attempt {ExecutionAttempts}");

                if (ExecutionAttempts <= _failTimes)
                {
                    return Result<bool>.Failure($"Transient network timeout on attempt {ExecutionAttempts}");
                }

                return Result<bool>.Success(true);
            }
        }

        [Fact]
        public async Task Workflow_WithTransientFailure_ShouldRetryAndSucceed()
        {
            // Arrange (fails on attempt 1, succeeds on attempt 2)
            var runner = new FlakyTerraformRunner(failTimes: 1);
            var engine = new DurableDeploymentWorkflowEngine(_store, runner);

            var req = new DeployRequest
            {
                ProjectName = "retry-service",
                Environment = "staging",
                Region = "us-east-1",
                InfrastructureType = "container"
            };

            var policy = new WorkflowRetryPolicy
            {
                MaxAttempts = 3,
                InitialInterval = TimeSpan.FromMilliseconds(50),
                BackoffCoefficient = 1.5
            };

            var deploymentId = Guid.NewGuid();
            var state = new DeploymentState { Id = deploymentId, Request = req };
            await _store.SaveAsync(state);

            // Act
            await engine.ExecuteWorkflowLifecycleAsync(state, policy, CancellationToken.None);

            // Assert
            var finalState = await _store.GetByIdAsync(deploymentId);
            Assert.NotNull(finalState);
            Assert.Equal(DeployStatus.Completed, finalState.Status);
            Assert.Equal(2, runner.ExecutionAttempts);
            Assert.Contains(finalState.ExecutionLog, l => l.Phase == "WorkflowRetry");
            Assert.Contains(finalState.ExecutionLog, l => l.Phase == "WorkflowEngine" && l.Message.Contains("succeeded"));
        }

        [Fact]
        public async Task Workflow_ExhaustingAllRetries_ShouldTransitionToFailed()
        {
            // Arrange (fails every attempt)
            var runner = new FlakyTerraformRunner(failTimes: 5);
            var engine = new DurableDeploymentWorkflowEngine(_store, runner);

            var req = new DeployRequest
            {
                ProjectName = "always-fail-service",
                Environment = "production",
                Region = "eu-west-1",
                InfrastructureType = "vm"
            };

            var policy = new WorkflowRetryPolicy
            {
                MaxAttempts = 2,
                InitialInterval = TimeSpan.FromMilliseconds(50),
                BackoffCoefficient = 1.0
            };

            var deploymentId = Guid.NewGuid();
            var state = new DeploymentState { Id = deploymentId, Request = req };
            await _store.SaveAsync(state);

            // Act
            await engine.ExecuteWorkflowLifecycleAsync(state, policy, CancellationToken.None);

            // Assert
            var finalState = await _store.GetByIdAsync(deploymentId);
            Assert.NotNull(finalState);
            Assert.Equal(DeployStatus.Failed, finalState.Status);
            Assert.Equal(2, runner.ExecutionAttempts);
            Assert.Contains(finalState.ExecutionLog, l => l.Phase == "WorkflowEngine" && l.Message.Contains("Workflow failed"));
        }

        [Fact]
        public async Task StartWorkflowAsync_WithInvalidPayload_ShouldReturnFailureImmediately()
        {
            // Arrange
            var runner = new FlakyTerraformRunner(failTimes: 0);
            var engine = new DurableDeploymentWorkflowEngine(_store, runner);

            var invalidReq = new DeployRequest
            {
                ProjectName = "invalid!",
                Environment = "invalid_env",
                Region = "",
                InfrastructureType = "bad_type"
            };

            // Act
            var result = await engine.StartWorkflowAsync(invalidReq);

            // Assert
            Assert.True(result.IsFailure);
        }
    }
}
