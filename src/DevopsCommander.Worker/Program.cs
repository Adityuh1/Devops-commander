using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register DevOps Commander services (IDeploymentStore, IDeploymentQueue)
builder.Services.AddDevopsCommanderServices();

// Register the worker background service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
