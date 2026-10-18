using Microsoft.Extensions.DependencyInjection;
using DevopsCommander.Shared.Interfaces;

namespace DevopsCommander.Shared.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the core DevOps Commander shared services (SQLite store and Channels queue) in DI.
        /// </summary>
        public static IServiceCollection AddDevopsCommanderServices(
            this IServiceCollection services,
            string connectionString = "Data Source=deployments.db",
            int queueCapacity = 1000)
        {
            services.AddSingleton<IDeploymentStore>(_ => new SqliteDeploymentStore(connectionString));
            services.AddSingleton<IDeploymentQueue>(_ => new ChannelDeploymentQueue(queueCapacity));
            services.AddSingleton<ITerraformRunner>(_ => new TerraformRunner());
            return services;
        }
    }
}
