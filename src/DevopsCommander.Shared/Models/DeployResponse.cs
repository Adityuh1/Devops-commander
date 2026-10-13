namespace DevopsCommander.Shared.Models;

/// <summary>
/// The response returned by the API after a deployment is enqueued.
/// Gives the caller a deployment ID to poll for status updates.
/// </summary>
public sealed record DeployResponse
{
    /// <summary>
    /// Unique identifier for the newly created deployment.
    /// Use this to query status via GET /api/deploy/{id}.
    /// </summary>
    public required Guid DeploymentId { get; init; }

    /// <summary>
    /// Current status at the time of the response (typically <see cref="DeployStatus.Pending"/>).
    /// </summary>
    public required DeployStatus Status { get; init; }

    /// <summary>
    /// Human-readable message confirming acceptance (e.g., "Deployment queued successfully").
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// UTC timestamp when the deployment was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
