using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using DevopsCommander.Shared.Models;
using DevopsCommander.Shared.Interfaces;

namespace DevopsCommander.Shared.Infrastructure
{
    /// <summary>
    /// Thread‑safe asynchronous deployment queue based on System.Threading.Channels.
    /// </summary>
    public class ChannelDeploymentQueue : IDeploymentQueue, IAsyncDisposable
    {
        private readonly Channel<DeployRequest> _channel;

        public ChannelDeploymentQueue(int capacity = 1000)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<DeployRequest>(options);
        }

        public async Task EnqueueAsync(DeployRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await _channel.Writer.WriteAsync(request);
        }

        public async Task<DeployRequest?> DequeueAsync()
        {
            // Wait for an item or completion.
            var result = await _channel.Reader.ReadAsync();
            return result;
        }

        public void Complete()
        {
            _channel.Writer.Complete();
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();
            // Drain remaining items if any.
            while (await _channel.Reader.WaitToReadAsync())
            {
                while (_channel.Reader.TryRead(out _)) { }
            }
        }
    }
}
