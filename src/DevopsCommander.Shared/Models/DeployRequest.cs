namespace DevopsCommander.Shared.Models;

/// <summary>
/// Represents an incoming deployment request from the MCP client (AI agent).
/// Contains all the information needed to provision infrastructure.
/// </summary>
public sealed record DeployRequest
{
    /// <summary>
    /// Unique identifier for this deployment. If not provided, one is assigned on creation.
    /// </summary>
    public Guid? DeploymentId { get; init; }

    /// <summary>
    /// Human-readable name for the project being deployed (e.g., "my-web-app").
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Target cloud environment: "dev", "staging", or "production".
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Cloud region to deploy into (e.g., "us-east-1", "europe-west1").
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// The type of infrastructure to provision (e.g., "container", "vm", "serverless").
    /// </summary>
    public required string InfrastructureType { get; init; }

    /// <summary>
    /// Optional key-value parameters passed to the Terraform template
    /// (e.g., instance size, replica count).
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// Who or what initiated this deployment (e.g., "copilot", "claude", "manual").
    /// Defaults to "unknown".
    /// </summary>
    public string RequestedBy { get; init; } = "unknown";
}
