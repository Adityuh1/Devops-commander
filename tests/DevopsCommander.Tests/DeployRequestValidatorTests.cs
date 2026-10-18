using Xunit;
using DevopsCommander.Shared.Models;
using DevopsCommander.Shared.Validation;

namespace DevopsCommander.Tests
{
    public class DeployRequestValidatorTests
    {
        private static DeployRequest CreateValidRequest() =>
            new()
            {
                ProjectName = "payment-gateway",
                Environment = "production",
                Region = "us-east-1",
                InfrastructureType = "container"
            };

        [Fact]
        public void Validate_WithValidRequest_ShouldSucceed()
        {
            var req = CreateValidRequest();
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsSuccess);
            Assert.Equal("payment-gateway", result.Value.ProjectName);
        }

        [Fact]
        public void Validate_WithNullRequest_ShouldFail()
        {
            var result = DeployRequestValidator.Validate(null);

            Assert.True(result.IsFailure);
            Assert.Contains("cannot be null", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("a")] // too short (<2)
        [InlineData("my project with spaces")]
        [InlineData("invalid@char!")]
        public void Validate_WithInvalidProjectName_ShouldFail(string projectName)
        {
            var req = CreateValidRequest() with { ProjectName = projectName };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsFailure);
            Assert.Contains("ProjectName", result.Error);
        }

        [Theory]
        [InlineData("dev")]
        [InlineData("staging")]
        [InlineData("production")]
        [InlineData("prod")]
        public void Validate_WithAllowedEnvironments_ShouldSucceed(string env)
        {
            var req = CreateValidRequest() with { Environment = env };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("")]
        [InlineData("local_test")]
        [InlineData("qa2")]
        public void Validate_WithInvalidEnvironment_ShouldFail(string env)
        {
            var req = CreateValidRequest() with { Environment = env };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsFailure);
            Assert.Contains("Environment", result.Error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithEmptyRegion_ShouldFail(string region)
        {
            var req = CreateValidRequest() with { Region = region };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsFailure);
            Assert.Contains("Region", result.Error);
        }

        [Theory]
        [InlineData("container")]
        [InlineData("vm")]
        [InlineData("serverless")]
        public void Validate_WithValidInfrastructureType_ShouldSucceed(string infraType)
        {
            var req = CreateValidRequest() with { InfrastructureType = infraType };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("")]
        [InlineData("baremetal")]
        [InlineData("quantum")]
        public void Validate_WithInvalidInfrastructureType_ShouldFail(string infraType)
        {
            var req = CreateValidRequest() with { InfrastructureType = infraType };
            var result = DeployRequestValidator.Validate(req);

            Assert.True(result.IsFailure);
            Assert.Contains("InfrastructureType", result.Error);
        }
    }
}
