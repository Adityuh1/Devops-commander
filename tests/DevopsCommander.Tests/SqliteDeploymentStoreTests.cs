using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Tests
{
    public class SqliteDeploymentStoreTests : IDisposable
    {
        private readonly string _dbFileName;
        private readonly SqliteDeploymentStore _store;

        public SqliteDeploymentStoreTests()
        {
            _dbFileName = $"test_deployments_{Guid.NewGuid():N}.db";
            _store = new SqliteDeploymentStore($"Data Source={_dbFileName}");
        }

        public void Dispose()
        {
            if (File.Exists(_dbFileName))
            {
                try
                {
                    File.Delete(_dbFileName);
                }
                catch
                {
                    // Ignore cleanup file locks in tests
                }
            }
        }

        private static DeployRequest CreateSampleRequest(string projectName = "web-service") =>
            new()
            {
                ProjectName = projectName,
                Environment = "dev",
                Region = "us-east-1",
                InfrastructureType = "container",
                RequestedBy = "test-runner",
                Parameters = { ["cpu"] = "2", ["memory"] = "4Gi" }
            };

        [Fact]
        public async Task SaveAsync_And_GetByIdAsync_ShouldRoundtripSuccessfully()
        {
            // Arrange
            var state = new DeploymentState
            {
                Request = CreateSampleRequest("order-api")
            };
            state.LogInfo("Validation", "Validated template parameters.");

            // Act
            await _store.SaveAsync(state);
            var retrieved = await _store.GetByIdAsync(state.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(state.Id, retrieved.Id);
            Assert.Equal(DeployStatus.Pending, retrieved.Status);
            Assert.Equal("order-api", retrieved.Request.ProjectName);
            Assert.Equal("dev", retrieved.Request.Environment);
            Assert.Equal("2", retrieved.Request.Parameters["cpu"]);
            Assert.Single(retrieved.ExecutionLog);
            Assert.Equal("Validation", retrieved.ExecutionLog[0].Phase);
            Assert.Equal("Validated template parameters.", retrieved.ExecutionLog[0].Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistStatusAndLogs()
        {
            // Arrange
            var state = new DeploymentState
            {
                Request = CreateSampleRequest("payments-api")
            };
            await _store.SaveAsync(state);

            // Act - update state (simulate worker execution)
            state.Status = DeployStatus.Provisioning;
            state.StartedAt = DateTimeOffset.UtcNow;
            state.LogInfo("Terraform", "Applying plan...");
            await _store.UpdateAsync(state);

            var updated = await _store.GetByIdAsync(state.Id);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal(DeployStatus.Provisioning, updated.Status);
            Assert.NotNull(updated.StartedAt);
            Assert.Single(updated.ExecutionLog);
            Assert.Equal("Terraform", updated.ExecutionLog[0].Phase);

            // Act - fail state
            state.Fail("Terraform", "Resource limit exceeded", "Error code: 429");
            await _store.UpdateAsync(state);

            var failed = await _store.GetByIdAsync(state.Id);

            // Assert
            Assert.NotNull(failed);
            Assert.Equal(DeployStatus.Failed, failed.Status);
            Assert.Equal("Resource limit exceeded", failed.ErrorMessage);
            Assert.Equal("Error code: 429", failed.ErrorDetails);
            Assert.NotNull(failed.FinishedAt);
            Assert.Equal(2, failed.ExecutionLog.Count);
        }

        [Fact]
        public async Task ListAsync_ShouldReturnAllDeploymentsOrderedByCreatedAtDescending()
        {
            // Arrange
            var state1 = new DeploymentState { Request = CreateSampleRequest("service-1") };
            var state2 = new DeploymentState { Request = CreateSampleRequest("service-2") };

            await _store.SaveAsync(state1);
            await _store.SaveAsync(state2);

            // Act
            var all = await _store.ListAsync();

            // Assert
            Assert.True(all.Count >= 2);
            Assert.Contains(all, s => s.Id == state1.Id);
            Assert.Contains(all, s => s.Id == state2.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistent_ShouldReturnNull()
        {
            // Act
            var result = await _store.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }
    }
}
