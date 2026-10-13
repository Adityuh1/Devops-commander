namespace DevopsCommander.Shared.Models;

/// <summary>
/// A single timestamped entry in the deployment execution log.
/// The worker appends entries as it progresses through the pipeline,
/// giving the API (and the AI agent) real-time visibility into what is happening.
/// </summary>
public sealed record ExecutionLogEntry
{
    /// <summary>
    /// UTC timestamp when this log entry was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The pipeline phase this entry relates to (e.g., "Validation", "TerraformInit", "TerraformApply").
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    /// Human-readable message describing what happened.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity level: "Info", "Warning", or "Error".
    /// </summary>
    public string Level { get; init; } = "Info";
}
