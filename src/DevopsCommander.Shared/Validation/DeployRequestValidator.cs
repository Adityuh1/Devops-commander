using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Validation
{
    /// <summary>
    /// Validates incoming deployment requests before they are persisted or queued.
    /// Fast-fails on missing or ambiguous inputs so failures are returned immediately to the caller.
    /// </summary>
    public static class DeployRequestValidator
    {
        private static readonly HashSet<string> AllowedEnvironments = new(StringComparer.OrdinalIgnoreCase)
        {
            "dev", "development", "staging", "stage", "production", "prod"
        };

        private static readonly HashSet<string> AllowedInfrastructureTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "container", "vm", "virtualmachine", "serverless"
        };

        private static readonly Regex ProjectNameRegex = new(@"^[a-zA-Z0-9\-_]{2,64}$", RegexOptions.Compiled);

        public static Result<DeployRequest> Validate(DeployRequest? request)
        {
            if (request == null)
            {
                return Result<DeployRequest>.Failure("Deploy request payload cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.ProjectName))
            {
                return Result<DeployRequest>.Failure("ProjectName is required.");
            }

            if (!ProjectNameRegex.IsMatch(request.ProjectName.Trim()))
            {
                return Result<DeployRequest>.Failure(
                    $"Invalid ProjectName '{request.ProjectName}'. It must be 2-64 characters containing only letters, numbers, dashes, and underscores.");
            }

            if (string.IsNullOrWhiteSpace(request.Environment))
            {
                return Result<DeployRequest>.Failure("Environment is required (e.g., 'dev', 'staging', 'production').");
            }

            if (!AllowedEnvironments.Contains(request.Environment.Trim()))
            {
                return Result<DeployRequest>.Failure(
                    $"Invalid Environment '{request.Environment}'. Allowed values are: 'dev', 'staging', 'production'.");
            }

            if (string.IsNullOrWhiteSpace(request.Region))
            {
                return Result<DeployRequest>.Failure("Region is required (e.g., 'us-east-1', 'eu-west-1', 'ap-south-1').");
            }

            if (string.IsNullOrWhiteSpace(request.InfrastructureType))
            {
                return Result<DeployRequest>.Failure("InfrastructureType is required (e.g., 'container', 'vm', 'serverless').");
            }

            if (!AllowedInfrastructureTypes.Contains(request.InfrastructureType.Trim()))
            {
                return Result<DeployRequest>.Failure(
                    $"Invalid InfrastructureType '{request.InfrastructureType}'. Allowed values are: 'container', 'vm', 'serverless'.");
            }

            return Result<DeployRequest>.Success(request);
        }
    }
}
