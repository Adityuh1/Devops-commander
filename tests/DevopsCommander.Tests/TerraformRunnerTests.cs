using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Tests
{
    public class TerraformRunnerTests : IDisposable
    {
        private readonly string _workspaceDir;

        public TerraformRunnerTests()
        {
            _workspaceDir = Path.Combine(Path.GetTempPath(), $"DevopsCommander_TestRuns_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_workspaceDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_workspaceDir))
            {
                try { Directory.Delete(_workspaceDir, recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_ContainerTemplate_ShouldRunInitAndApplySuccessfully()
        {
            // Arrange
            var runner = new TerraformRunner(workspaceRoot: _workspaceDir);

            var deploymentId = Guid.NewGuid();
            var state = new DeploymentState
            {
                Id = deploymentId,
                Request = new DeployRequest
                {
                    DeploymentId = deploymentId,
                    ProjectName = "test-container-svc",
                    Environment = "dev",
                    Region = "us-east-1",
                    InfrastructureType = "container",
                    Parameters = { ["replica_count"] = "2" }
                }
            };

            var logsRecorded = 0;

            // Act
            var result = await runner.ExecuteAsync(
                state,
                (phase, log) =>
                {
                    logsRecorded++;
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess, $"Terraform execution failed: {result.Match(_ => "", err => err)}");
            Assert.True(logsRecorded > 0);

            // Verify generated files exist in the run directory
            var runDir = Path.Combine(_workspaceDir, deploymentId.ToString("N"));
            Assert.True(Directory.Exists(runDir));
            Assert.True(File.Exists(Path.Combine(runDir, "terraform.tfvars.json")));
            Assert.True(File.Exists(Path.Combine(runDir, "main.tf")));
        }

        [Fact]
        public async Task ExecuteAsync_VmTemplate_ShouldRunInitAndApplySuccessfully()
        {
            // Arrange
            var runner = new TerraformRunner(workspaceRoot: _workspaceDir);

            var deploymentId = Guid.NewGuid();
            var state = new DeploymentState
            {
                Id = deploymentId,
                Request = new DeployRequest
                {
                    DeploymentId = deploymentId,
                    ProjectName = "test-vm-svc",
                    Environment = "staging",
                    Region = "eu-west-1",
                    InfrastructureType = "vm"
                }
            };

            // Act
            var result = await runner.ExecuteAsync(
                state,
                (phase, log) => Task.CompletedTask,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess, $"Terraform execution failed: {result.Match(_ => "", err => err)}");
        }

        [Fact]
        public async Task ExecuteAsync_InvalidInfrastructureType_ShouldFailFast()
        {
            // Arrange
            var runner = new TerraformRunner(workspaceRoot: _workspaceDir);

            var state = new DeploymentState
            {
                Request = new DeployRequest
                {
                    ProjectName = "unknown-svc",
                    Environment = "dev",
                    Region = "us-east-1",
                    InfrastructureType = "nonexistent-type"
                }
            };

            // Act
            var result = await runner.ExecuteAsync(
                state,
                (phase, log) => Task.CompletedTask,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("template directory not found", result.Error);
        }
    }
}
