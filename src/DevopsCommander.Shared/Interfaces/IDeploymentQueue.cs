using System;
using System.Threading.Tasks;
using DevopsCommander.Shared.Models;

namespace DevopsCommander.Shared.Interfaces
{
    /// <summary>
    /// Abstraction for a thread‑safe, asynchronous queue that workers consume deployment jobs from.
    /// Implementations may use System.Threading.Channels, a message broker, etc.
    /// </summary>
    public interface IDeploymentQueue
    {
        /// <summary>
        /// Enqueues a new deployment request.
        /// </summary>
        Task EnqueueAsync(DeployRequest request);

        /// <summary>
        /// Dequeues the next deployment request. Returns null when the queue is completed.
        /// </summary>
        Task<DeployRequest?> DequeueAsync();

        /// <summary>
        /// Signals that no more items will be added – useful for graceful shutdown.
        /// </summary>
        void Complete();
    }
}
