using System.Text.Json;
using DevopsCommander.Shared.Infrastructure;
using DevopsCommander.Shared.Interfaces;
using DevopsCommander.Shared.Models;
using DevopsCommander.Shared.Validation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI / Swagger
builder.Services.AddOpenApi();

// Register DevOps Commander Shared Services (SQLite store + Channel queue)
builder.Services.AddDevopsCommanderServices();

// Host the background worker directly inside the API process for all-in-one execution
builder.Services.AddHostedService<DevopsCommander.Worker.Worker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// ── Web Dashboard ───────────────────────────────────────────────────
app.MapGet("/", () => Results.Content(DevopsCommander.McpServer.Dashboard.Html, "text/html"))
   .WithName("Dashboard")
   .ExcludeFromDescription();

app.MapGet("/dashboard", () => Results.Content(DevopsCommander.McpServer.Dashboard.Html, "text/html"))
   .WithName("DashboardPage")
   .ExcludeFromDescription();

// ── REST API Endpoints ──────────────────────────────────────────────

// POST /api/deploy
app.MapPost("/api/deploy", async (DeployRequest request, IDeploymentStore store, IDeploymentQueue queue) =>
{
    var validation = DeployRequestValidator.Validate(request);
    if (validation.IsFailure)
    {
        return Results.BadRequest(new { error = validation.Error });
    }

    var validRequest = validation.Value;
    var deploymentId = validRequest.DeploymentId ?? Guid.NewGuid();
    var requestWithId = validRequest with { DeploymentId = deploymentId };

    var state = new DeploymentState
    {
        Id = deploymentId,
        Request = requestWithId,
        Status = DeployStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow
    };
    state.LogInfo("API", "Deployment request accepted and enqueued.");

    await store.SaveAsync(state);
    await queue.EnqueueAsync(requestWithId);

    var response = new DeployResponse
    {
        DeploymentId = deploymentId,
        Status = DeployStatus.Pending,
        Message = $"Deployment queued successfully for '{requestWithId.ProjectName}' ({requestWithId.Environment}) in {requestWithId.Region}.",
        CreatedAt = state.CreatedAt
    };

    return Results.Accepted($"/api/deploy/{deploymentId}", response);
})
.WithName("CreateDeployment")
.WithSummary("Queues a new infrastructure deployment")
.WithDescription("Validates the payload, creates pending state in SQLite, and enqueues to background worker.");

// GET /api/deploy/{id}
app.MapGet("/api/deploy/{id:guid}", async (Guid id, IDeploymentStore store) =>
{
    var state = await store.GetByIdAsync(id);
    if (state == null)
    {
        return Results.NotFound(new { error = $"Deployment with ID '{id}' was not found." });
    }
    return Results.Ok(state);
})
.WithName("GetDeploymentStatus")
.WithSummary("Gets deployment status and audit log history by ID");

// GET /api/deployments
app.MapGet("/api/deployments", async (IDeploymentStore store) =>
{
    var list = await store.ListAsync();
    return Results.Ok(list);
})
.WithName("ListDeployments")
.WithSummary("Lists all historical and current deployments");

// ── MCP Tool Handlers for AI Agents (Claude / Copilot) ──────────────

object[] toolsMetadata =
[
    new
    {
        name = "deploy_infrastructure",
        description = "Provisions cloud infrastructure (container, vm, or serverless) for a given project and environment.",
        inputSchema = new
        {
            type = "object",
            properties = new
            {
                projectName = new { type = "string", description = "Name of the project/service (e.g. my-service)" },
                environment = new { type = "string", description = "Target environment: dev, staging, or production" },
                region = new { type = "string", description = "Cloud region, e.g. us-east-1, eu-west-1" },
                infrastructureType = new { type = "string", description = "Type of infrastructure: container, vm, or serverless" },
                parameters = new { type = "object", description = "Key-value configuration parameters" }
            },
            required = new[] { "projectName", "environment", "region", "infrastructureType" }
        }
    },
    new
    {
        name = "get_deployment_status",
        description = "Retrieves current status, timestamps, and full execution log history for a deployment by its UUID.",
        inputSchema = new
        {
            type = "object",
            properties = new
            {
                deploymentId = new { type = "string", description = "The UUID of the deployment" }
            },
            required = new[] { "deploymentId" }
        }
    },
    new
    {
        name = "list_deployments",
        description = "Returns a list of all deployments, including status and timestamps.",
        inputSchema = new
        {
            type = "object",
            properties = new { }
        }
    }
];

// GET /mcp/tools
app.MapGet("/mcp/tools", () => Results.Ok(new { tools = toolsMetadata }))
   .WithName("McpListTools");

// POST /mcp/tools/call
app.MapPost("/mcp/tools/call", async (McpCallRequest call, IDeploymentStore store, IDeploymentQueue queue) =>
{
    if (call == null || string.IsNullOrWhiteSpace(call.Name))
    {
        return Results.BadRequest(new { error = "Tool name is required." });
    }

    switch (call.Name.ToLowerInvariant())
    {
        case "deploy_infrastructure":
        {
            var args = call.Arguments ?? new Dictionary<string, object>();
            var projectName = args.TryGetValue("projectName", out var p) ? p?.ToString() ?? "" : "";
            var environment = args.TryGetValue("environment", out var e) ? e?.ToString() ?? "" : "";
            var region = args.TryGetValue("region", out var r) ? r?.ToString() ?? "" : "";
            var infraType = args.TryGetValue("infrastructureType", out var i) ? i?.ToString() ?? "" : "";
            var parameters = new Dictionary<string, string>();

            if (args.TryGetValue("parameters", out var rawParams) && rawParams is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in jsonElem.EnumerateObject())
                {
                    parameters[prop.Name] = prop.Value.ToString();
                }
            }

            var req = new DeployRequest
            {
                ProjectName = projectName,
                Environment = environment,
                Region = region,
                InfrastructureType = infraType,
                Parameters = parameters,
                RequestedBy = "mcp-agent"
            };

            var validation = DeployRequestValidator.Validate(req);
            if (validation.IsFailure)
            {
                return Results.Ok(new { isError = true, content = new[] { new { type = "text", text = $"Validation error: {validation.Error}" } } });
            }

            var deploymentId = Guid.NewGuid();
            var validReq = req with { DeploymentId = deploymentId };

            var state = new DeploymentState
            {
                Id = deploymentId,
                Request = validReq,
                Status = DeployStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            state.LogInfo("MCP", "Triggered via MCP tool deploy_infrastructure.");

            await store.SaveAsync(state);
            await queue.EnqueueAsync(validReq);

            return Results.Ok(new
            {
                isError = false,
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Deployment {deploymentId} queued successfully for {projectName} ({environment}) in {region}."
                    }
                }
            });
        }

        case "get_deployment_status":
        {
            var args = call.Arguments ?? new Dictionary<string, object>();
            if (!args.TryGetValue("deploymentId", out var rawId) || !Guid.TryParse(rawId?.ToString(), out var id))
            {
                return Results.Ok(new { isError = true, content = new[] { new { type = "text", text = "Invalid or missing deploymentId." } } });
            }

            var state = await store.GetByIdAsync(id);
            if (state == null)
            {
                return Results.Ok(new { isError = true, content = new[] { new { type = "text", text = $"Deployment {id} not found." } } });
            }

            return Results.Ok(new
            {
                isError = false,
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            });
        }

        case "list_deployments":
        {
            var list = await store.ListAsync();
            return Results.Ok(new
            {
                isError = false,
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            });
        }

        default:
            return Results.BadRequest(new { error = $"Unknown tool '{call.Name}'." });
    }
})
.WithName("McpCallTool");

app.Run();

// ── Models for MCP Tool Invocations ─────────────────────────────────
public sealed record McpCallRequest
{
    public string Name { get; init; } = "";
    public Dictionary<string, object>? Arguments { get; init; }
}

public partial class Program { }
