namespace DevopsCommander.Shared.Models;

/// <summary>
/// The full state of a deployment job, tracked from creation through completion.
/// This is the central entity persisted in the state store and returned by query APIs.
/// </summary>
public sealed class DeploymentState
{
    /// <summary>
    /// Unique identifier for this deployment (assigned on creation).
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The original request that created this deployment.
    /// </summary>
    public required DeployRequest Request { get; init; }

    /// <summary>
    /// Current lifecycle status of the deployment.
    /// </summary>
    public DeployStatus Status { get; set; } = DeployStatus.Pending;

    /// <summary>
    /// UTC timestamp when the deployment was created / enqueued.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the worker started executing this deployment.
    /// Null if still pending.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the deployment reached a terminal state (Completed, Failed, Cancelled).
    /// Null if still in progress.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// Ordered list of execution log entries recorded during the pipeline run.
    /// The worker appends entries as it transitions between phases.
    /// </summary>
    public List<ExecutionLogEntry> ExecutionLog { get; init; } = [];

    /// <summary>
    /// If the deployment failed, this contains the top-level error message.
    /// Null for non-failed deployments.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// If the deployment failed, this optionally contains the full exception details
    /// (stack trace, inner exceptions). Only populated in non-production environments.
    /// </summary>
    public string? ErrorDetails { get; set; }

    // ── Convenience methods ──────────────────────────────────────────

    /// <summary>
    /// Adds a timestamped info-level log entry for the given phase.
    /// </summary>
    public void LogInfo(string phase, string message) =>
        ExecutionLog.Add(new ExecutionLogEntry { Phase = phase, Message = message });

    /// <summary>
    /// Adds a timestamped warning-level log entry for the given phase.
    /// </summary>
    public void LogWarning(string phase, string message) =>
        ExecutionLog.Add(new ExecutionLogEntry { Phase = phase, Message = message, Level = "Warning" });

    /// <summary>
    /// Adds a timestamped error-level log entry for the given phase.
    /// </summary>
    public void LogError(string phase, string message) =>
        ExecutionLog.Add(new ExecutionLogEntry { Phase = phase, Message = message, Level = "Error" });

    /// <summary>
    /// Transitions the deployment to the <see cref="DeployStatus.Failed"/> state,
    /// recording the error and a log entry.
    /// </summary>
    public void Fail(string phase, string errorMessage, string? errorDetails = null)
    {
        Status = DeployStatus.Failed;
        FinishedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        LogError(phase, errorMessage);
    }

    /// <summary>
    /// Transitions the deployment to the <see cref="DeployStatus.Completed"/> state.
    /// </summary>
    public void Complete()
    {
        Status = DeployStatus.Completed;
        FinishedAt = DateTimeOffset.UtcNow;
        LogInfo("Pipeline", "Deployment completed successfully.");
    }
}
