using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Interfaces
{
    /// <summary>
    /// Abstraction for a persistence store that tracks deployment state and logs.
    /// Implementations may use SQLite, a file, or any other durable storage.
    /// </summary>
    public interface IDeploymentStore
    {
        /// <summary>
        /// Saves a new deployment state.
        /// </summary>
        Task SaveAsync(DeploymentState state);

        /// <summary>
        /// Updates an existing deployment entry.
        /// </summary>
        Task UpdateAsync(DeploymentState state);

        /// <summary>
        /// Retrieves a deployment by its identifier.
        /// </summary>
        Task<DeploymentState?> GetByIdAsync(Guid deploymentId);

        /// <summary>
        /// Returns all deployments (optionally filtered).
        /// </summary>
        Task<IReadOnlyList<DeploymentState>> ListAsync();
    }
}
