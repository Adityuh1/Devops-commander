using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Tests
{
    public class McpServerEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly string _testDb;

        public McpServerEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _testDb = $"test_api_{Guid.NewGuid():N}.db";
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Isolated test services
                    services.AddSingleton<IDeploymentStore>(_ => new DevopsCommander.Shared.Infrastructure.SqliteDeploymentStore($"Data Source={_testDb}"));
                    services.AddSingleton<IDeploymentQueue>(_ => new DevopsCommander.Shared.Infrastructure.ChannelDeploymentQueue(100));
                });
            });
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            if (File.Exists(_testDb))
            {
                try { File.Delete(_testDb); } catch { }
            }
        }

        [Fact]
        public async Task PostDeploy_WithValidPayload_ShouldReturn202Accepted_And_BeQueryable()
        {
            // Arrange
            var request = new DeployRequest
            {
                ProjectName = "order-service",
                Environment = "staging",
                Region = "us-west-2",
                InfrastructureType = "container",
                Parameters = { ["cpu"] = "1" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/deploy", request);

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var deployResponse = await response.Content.ReadFromJsonAsync<DeployResponse>();
            Assert.NotNull(deployResponse);
            Assert.NotEqual(Guid.Empty, deployResponse.DeploymentId);
            Assert.Equal(DeployStatus.Pending, deployResponse.Status);

            // Query status by ID
            var getResponse = await _client.GetAsync($"/api/deploy/{deployResponse.DeploymentId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var state = await getResponse.Content.ReadFromJsonAsync<DeploymentState>();
            Assert.NotNull(state);
            Assert.Equal(deployResponse.DeploymentId, state.Id);
            Assert.Equal("order-service", state.Request.ProjectName);
            Assert.NotEmpty(state.ExecutionLog);
        }

        [Fact]
        public async Task PostDeploy_WithInvalidPayload_ShouldReturn400BadRequest()
        {
            // Arrange (missing Environment and Region)
            var invalidRequest = new
            {
                ProjectName = "bad-request"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/deploy", invalidRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetDeployById_NonExistent_ShouldReturn404NotFound()
        {
            // Act
            var response = await _client.GetAsync($"/api/deploy/{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetDeployments_ShouldReturnList()
        {
            // Act
            var response = await _client.GetAsync("/api/deployments");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task McpTools_List_ShouldReturnRegisteredTools()
        {
            // Act
            var response = await _client.GetAsync("/mcp/tools");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("deploy_infrastructure", content);
            Assert.Contains("get_deployment_status", content);
            Assert.Contains("list_deployments", content);
        }

        [Fact]
        public async Task McpTools_Call_DeployInfrastructure_ShouldEnqueueAndReturnText()
        {
            // Arrange
            var callRequest = new
            {
                Name = "deploy_infrastructure",
                Arguments = new
                {
                    projectName = "ai-service",
                    environment = "dev",
                    region = "eu-west-1",
                    infrastructureType = "container"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/mcp/tools/call", callRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("queued successfully", content);
            Assert.Contains("ai-service", content);
        }
    }
}
