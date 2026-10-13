namespace DevopsCommander.Shared.Models;

/// <summary>
/// The lifecycle status of a deployment job.
/// Transitions: Pending → Provisioning → Completed | Failed | Cancelled
/// </summary>
public enum DeployStatus
{
    /// <summary>Job is queued and waiting to be picked up by the worker.</summary>
    Pending,

    /// <summary>Worker is actively running the deployment pipeline.</summary>
    Provisioning,

    /// <summary>Deployment completed successfully.</summary>
    Completed,

    /// <summary>Deployment failed due to an error during execution.</summary>
    Failed,

    /// <summary>Deployment was cancelled before or during execution.</summary>
    Cancelled
}
