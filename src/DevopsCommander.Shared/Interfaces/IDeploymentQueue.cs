using System;
using System.Threading;
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
        Task EnqueueAsync(DeployRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dequeues the next deployment request. Returns null when the queue is completed or cancelled.
        /// </summary>
        Task<DeployRequest?> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Signals that no more items will be added – useful for graceful shutdown.
        /// </summary>
        void Complete();
    }
}
