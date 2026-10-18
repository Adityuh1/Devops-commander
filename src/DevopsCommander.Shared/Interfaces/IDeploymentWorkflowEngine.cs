using System;
using System.Threading;
using System.Threading.Tasks;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Interfaces
{
    /// <summary>
    /// Configuration for automatic step retry policies in durable deployment workflows.
    /// </summary>
    public sealed record WorkflowRetryPolicy
    {
        public int MaxAttempts { get; init; } = 3;
        public TimeSpan InitialInterval { get; init; } = TimeSpan.FromSeconds(2);
        public double BackoffCoefficient { get; init; } = 2.0;
        public TimeSpan MaxInterval { get; init; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Abstraction for a durable workflow engine (inspired by Temporal) that provides
    /// crash-resilient execution, state preservation, and automatic step retry policies.
    /// </summary>
    public interface IDeploymentWorkflowEngine
    {
        /// <summary>
        /// Initiates a durable workflow for an infrastructure deployment request.
        /// </summary>
        Task<Result<Guid>> StartWorkflowAsync(
            DeployRequest request,
            WorkflowRetryPolicy? retryPolicy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the current workflow state.
        /// </summary>
        Task<DeploymentState?> GetWorkflowStateAsync(Guid deploymentId);
    }
}
