# Model Context Protocol (MCP) Client Setup Guide

DevOps Commander implements the [Model Context Protocol (MCP)](https://modelcontextprotocol.io), allowing AI assistants such as **GitHub Copilot** and **Claude Desktop** to autonomously provision and monitor cloud infrastructure using natural language.

---

## 1. Registering with Claude Desktop

Claude Desktop connects to MCP servers to provide AI models with local tool execution capabilities.

### Configuration Path
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`  
  *(Typically `C:\Users\<Username>\AppData\Roaming\Claude\claude_desktop_config.json`)*
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

### Configuration Snippet
Add `devops-commander` to the `mcpServers` object:

```json
{
  "mcpServers": {
    "devops-commander": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Users\\HP\\OneDrive\\Desktop\\Projects\\Devops-commander\\src\\DevopsCommander.McpServer\\DevopsCommander.McpServer.csproj",
        "--urls",
        "http://localhost:5076"
      ]
    }
  }
}
```

Or, if the server is already running over HTTP, configure the HTTP endpoint proxy or launch script.

---

## 2. Registering with GitHub Copilot (VS Code)

GitHub Copilot supports MCP tools in VS Code via workspace settings or MCP extensions.

### Configuration Path
In your workspace, create or edit `.vscode/mcp.json`:

```json
{
  "servers": {
    "devops-commander": {
      "type": "http",
      "url": "http://localhost:5076/mcp"
    }
  }
}
```

---

## 3. Available Tools Exposed to AI Models

Once registered, your AI assistant will automatically discover and use the following tools:

### `deploy_infrastructure`
- **Description**: Provisions cloud infrastructure (container, vm, or serverless) for a given project and environment.
- **Parameters**:
  - `projectName` (string, required): e.g. `"order-processing-api"`
  - `environment` (string, required): `"dev"`, `"staging"`, or `"production"`
  - `region` (string, required): e.g. `"us-east-1"`, `"eu-west-1"`
  - `infrastructureType` (string, required): `"container"`, `"vm"`, or `"serverless"`
  - `parameters` (object, optional): e.g. `{"replica_count": "2"}`

### `get_deployment_status`
- **Description**: Retrieves current status, timestamps, and full execution log history for a deployment by its UUID.
- **Parameters**:
  - `deploymentId` (string, required): The UUID of the deployment.

### `list_deployments`
- **Description**: Returns a list of all historical and active deployments.

---

## 4. Example AI Prompts

You can now ask Claude or GitHub Copilot natural language queries like:

- *"Deploy an order service to staging in us-east-1 as a container with 3 replicas."*
- *"Check the deployment status of deployment ID 4f7c8b1a-..."*
- *"List all recent infrastructure deployments and tell me if any failed."*
