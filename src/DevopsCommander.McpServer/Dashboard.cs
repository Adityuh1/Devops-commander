namespace DevopsCommander.McpServer;

public static class Dashboard
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>DevOps Commander | Autonomous Cloud Infrastructure Orchestrator</title>
    <style>
        :root {
            --bg-primary: #0b0f19;
            --bg-secondary: #111827;
            --bg-card: #1e293b;
            --bg-card-hover: #26354a;
            --border: #334155;
            --border-focus: #3b82f6;
            --text-primary: #f8fafc;
            --text-secondary: #94a3b8;
            --text-muted: #64748b;
            --accent-blue: #3b82f6;
            --accent-blue-hover: #2563eb;
            --accent-green: #10b981;
            --accent-green-bg: rgba(16, 185, 129, 0.15);
            --accent-yellow: #f59e0b;
            --accent-yellow-bg: rgba(245, 158, 11, 0.15);
            --accent-red: #ef4444;
            --accent-red-bg: rgba(239, 68, 68, 0.15);
            --accent-purple: #8b5cf6;
            --accent-cyan: #06b6d4;
            --font-mono: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
            --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            background-color: var(--bg-primary);
            color: var(--text-primary);
            font-family: var(--font-sans);
            line-height: 1.5;
            padding: 24px;
            min-height: 100vh;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
        }

        /* Header */
        header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 24px;
            border-bottom: 1px solid var(--border);
            margin-bottom: 24px;
            flex-wrap: wrap;
            gap: 16px;
        }

        .logo-area {
            display: flex;
            align-items: center;
            gap: 14px;
        }

        .logo-badge {
            background: linear-gradient(135deg, #2563eb, #7c3aed);
            color: white;
            font-weight: 800;
            font-size: 20px;
            width: 48px;
            height: 48px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 12px;
            box-shadow: 0 4px 14px rgba(37, 99, 235, 0.4);
        }

        h1 {
            font-size: 24px;
            font-weight: 700;
            letter-spacing: -0.5px;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .subtitle {
            font-size: 13px;
            color: var(--text-secondary);
        }

        .system-badges {
            display: flex;
            gap: 10px;
            flex-wrap: wrap;
        }

        .badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 5px 12px;
            border-radius: 9999px;
            font-size: 12px;
            font-weight: 600;
            border: 1px solid var(--border);
            background-color: var(--bg-secondary);
        }

        .badge-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
        }

        .badge-dot.green { background-color: var(--accent-green); box-shadow: 0 0 8px var(--accent-green); }
        .badge-dot.blue { background-color: var(--accent-blue); }
        .badge-dot.yellow { background-color: var(--accent-yellow); box-shadow: 0 0 8px var(--accent-yellow); }

        /* Stats Grid */
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 16px;
            margin-bottom: 28px;
        }

        .stat-card {
            background-color: var(--bg-secondary);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 18px 20px;
            transition: transform 0.2s, border-color 0.2s;
        }

        .stat-card:hover {
            transform: translateY(-2px);
            border-color: var(--border-focus);
        }

        .stat-label {
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: var(--text-secondary);
            margin-bottom: 6px;
        }

        .stat-value {
            font-size: 28px;
            font-weight: 700;
            color: var(--text-primary);
        }

        .stat-sub {
            font-size: 11px;
            color: var(--text-muted);
            margin-top: 4px;
        }

        /* Main Workspace Grid */
        .main-layout {
            display: grid;
            grid-template-columns: 440px 1fr;
            gap: 24px;
        }

        @media (max-width: 1024px) {
            .main-layout {
                grid-template-columns: 1fr;
            }
        }

        .card {
            background-color: var(--bg-secondary);
            border: 1px solid var(--border);
            border-radius: 14px;
            overflow: hidden;
            display: flex;
            flex-direction: column;
        }

        .card-header {
            padding: 18px 22px;
            border-bottom: 1px solid var(--border);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .card-title {
            font-size: 16px;
            font-weight: 700;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .card-body {
            padding: 22px;
            flex: 1;
        }

        /* Tab Navigation */
        .tabs {
            display: flex;
            gap: 8px;
            margin-bottom: 18px;
            border-bottom: 1px solid var(--border);
            padding-bottom: 10px;
        }

        .tab-btn {
            background: none;
            border: none;
            color: var(--text-secondary);
            font-size: 13px;
            font-weight: 600;
            padding: 6px 14px;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
        }

        .tab-btn:hover {
            color: var(--text-primary);
            background-color: var(--bg-card);
        }

        .tab-btn.active {
            color: white;
            background-color: var(--accent-blue);
        }

        /* Preset Cards */
        .presets-title {
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            color: var(--text-secondary);
            margin-bottom: 10px;
        }

        .preset-buttons {
            display: grid;
            grid-template-columns: 1fr;
            gap: 8px;
            margin-bottom: 20px;
        }

        .preset-btn {
            background-color: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 10px 14px;
            text-align: left;
            cursor: pointer;
            color: var(--text-primary);
            font-size: 13px;
            transition: all 0.2s;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .preset-btn:hover {
            background-color: var(--bg-card-hover);
            border-color: var(--accent-blue);
            transform: translateX(3px);
        }

        .preset-desc {
            font-size: 11px;
            color: var(--text-secondary);
            margin-top: 2px;
        }

        /* Form Controls */
        .form-group {
            margin-bottom: 14px;
        }

        label {
            display: block;
            font-size: 12px;
            font-weight: 600;
            color: var(--text-secondary);
            margin-bottom: 6px;
        }

        input, select, textarea {
            width: 100%;
            background-color: var(--bg-primary);
            border: 1px solid var(--border);
            color: var(--text-primary);
            padding: 10px 12px;
            border-radius: 8px;
            font-size: 13px;
            font-family: inherit;
            transition: border-color 0.2s;
        }

        input:focus, select:focus, textarea:focus {
            outline: none;
            border-color: var(--border-focus);
            box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
        }

        .btn-primary {
            width: 100%;
            background: linear-gradient(135deg, #2563eb, #1d4ed8);
            color: white;
            border: none;
            padding: 12px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 700;
            cursor: pointer;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            margin-top: 10px;
        }

        .btn-primary:hover {
            background: linear-gradient(135deg, #1d4ed8, #1e40af);
            box-shadow: 0 4px 12px rgba(37, 99, 235, 0.35);
        }

        .btn-primary:disabled {
            opacity: 0.6;
            cursor: not-allowed;
        }

        .btn-sm {
            background-color: var(--bg-card);
            border: 1px solid var(--border);
            color: var(--text-primary);
            padding: 5px 12px;
            border-radius: 6px;
            font-size: 12px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        }

        .btn-sm:hover {
            background-color: var(--bg-card-hover);
            border-color: var(--border-focus);
        }

        /* Deployments List */
        .table-responsive {
            overflow-x: auto;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
            text-align: left;
        }

        th {
            background-color: rgba(30, 41, 59, 0.5);
            padding: 12px 14px;
            font-weight: 600;
            color: var(--text-secondary);
            border-bottom: 1px solid var(--border);
            text-transform: uppercase;
            font-size: 11px;
            letter-spacing: 0.5px;
        }

        td {
            padding: 14px;
            border-bottom: 1px solid var(--border);
            color: var(--text-primary);
        }

        tr:hover td {
            background-color: rgba(30, 41, 59, 0.3);
        }

        /* Status Pills */
        .status-pill {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 3px 10px;
            border-radius: 9999px;
            font-size: 11px;
            font-weight: 700;
        }

        .status-completed {
            background-color: var(--accent-green-bg);
            color: var(--accent-green);
            border: 1px solid rgba(16, 185, 129, 0.3);
        }

        .status-provisioning {
            background-color: rgba(59, 130, 246, 0.15);
            color: var(--accent-blue);
            border: 1px solid rgba(59, 130, 246, 0.3);
            animation: pulse 2s infinite;
        }

        .status-pending {
            background-color: var(--accent-yellow-bg);
            color: var(--accent-yellow);
            border: 1px solid rgba(245, 158, 11, 0.3);
        }

        .status-failed {
            background-color: var(--accent-red-bg);
            color: var(--accent-red);
            border: 1px solid rgba(239, 68, 68, 0.3);
        }

        @keyframes pulse {
            0% { opacity: 0.7; }
            50% { opacity: 1; }
            100% { opacity: 0.7; }
        }

        /* Terminal Window */
        .terminal-container {
            margin-top: 24px;
            background-color: #030712;
            border: 1px solid var(--border);
            border-radius: 12px;
            overflow: hidden;
            display: flex;
            flex-direction: column;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
        }

        .terminal-header {
            background-color: #111827;
            padding: 10px 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 1px solid var(--border);
        }

        .terminal-dots {
            display: flex;
            gap: 6px;
        }

        .terminal-dot {
            width: 10px;
            height: 10px;
            border-radius: 50%;
        }

        .terminal-dot.red { background-color: #ef4444; }
        .terminal-dot.yellow { background-color: #f59e0b; }
        .terminal-dot.green { background-color: #10b981; }

        .terminal-title {
            font-size: 12px;
            font-family: var(--font-mono);
            color: var(--text-secondary);
        }

        .terminal-body {
            padding: 16px;
            font-family: var(--font-mono);
            font-size: 12px;
            color: #d1d5db;
            max-height: 420px;
            overflow-y: auto;
            white-space: pre-wrap;
            line-height: 1.6;
        }

        .log-entry {
            margin-bottom: 4px;
            display: flex;
            gap: 10px;
        }

        .log-time {
            color: #6b7280;
            user-select: none;
        }

        .log-phase {
            font-weight: 700;
            padding: 1px 6px;
            border-radius: 4px;
            font-size: 10px;
        }

        .phase-Worker { background: rgba(139, 92, 246, 0.2); color: #c4b5fd; }
        .phase-Preparation { background: rgba(6, 182, 212, 0.2); color: #67e8f9; }
        .phase-TerraformInit { background: rgba(245, 158, 11, 0.2); color: #fde68a; }
        .phase-TerraformApply { background: rgba(16, 185, 129, 0.2); color: #a7f3d0; }
        .phase-Pipeline { background: rgba(59, 130, 246, 0.2); color: #bfdbfe; }
        .phase-API { background: rgba(100, 116, 139, 0.2); color: #cbd5e1; }

        .log-msg {
            flex: 1;
            word-break: break-all;
        }

        .empty-state {
            text-align: center;
            padding: 40px 20px;
            color: var(--text-secondary);
        }

        .empty-icon {
            font-size: 36px;
            margin-bottom: 10px;
        }
    </style>
</head>
<body>
    <div class="container">
        <!-- Top Navigation & System Status -->
        <header>
            <div class="logo-area">
                <div class="logo-badge">⚡</div>
                <div>
                    <h1>DevOps Commander <span style="font-size: 13px; font-weight: 500; background: rgba(59,130,246,0.2); color: #60a5fa; padding: 2px 8px; border-radius: 6px; border: 1px solid rgba(59,130,246,0.4)">Live Control Plane</span></h1>
                    <div class="subtitle">Autonomous AI-Powered Cloud Infrastructure Engine (Terraform &middot; .NET 10 &middot; MCP)</div>
                </div>
            </div>
            <div class="system-badges">
                <div class="badge"><span class="badge-dot green"></span> REST API Active</div>
                <div class="badge"><span class="badge-dot green"></span> Worker Queue (Channels)</div>
                <div class="badge"><span class="badge-dot green"></span> SQLite DB Connected</div>
                <div class="badge"><span class="badge-dot blue"></span> Terraform CLI v1.10</div>
                <button class="btn-sm" id="btn-refresh" onclick="fetchDeployments(true)">🔄 Refresh</button>
            </div>
        </header>

        <!-- Stats Overview Cards -->
        <div class="stats-grid">
            <div class="stat-card">
                <div class="stat-label">Total Provisioned</div>
                <div class="stat-value" id="stat-total">0</div>
                <div class="stat-sub">Deployments registered in SQLite</div>
            </div>
            <div class="stat-card">
                <div class="stat-label">Successful Deploys</div>
                <div class="stat-value" style="color: var(--accent-green);" id="stat-success">0</div>
                <div class="stat-sub">Completed via Terraform apply</div>
            </div>
            <div class="stat-card">
                <div class="stat-label">Queue / In-Flight</div>
                <div class="stat-value" style="color: var(--accent-blue);" id="stat-active">0</div>
                <div class="stat-sub">Pending or Provisioning</div>
            </div>
            <div class="stat-card">
                <div class="stat-label">System Architecture</div>
                <div class="stat-value" style="font-size: 18px; color: var(--accent-purple); padding-top: 6px;">Async FIFO Queue</div>
                <div class="stat-sub">HTTP 202 &middot; Channels &middot; Durable</div>
            </div>
        </div>

        <!-- Main Workspace -->
        <div class="main-layout">
            <!-- Left Column: Deploy Controls & MCP Tester -->
            <div class="card">
                <div class="card-header">
                    <div class="card-title">🚀 Dispatch Center</div>
                    <div class="tabs">
                        <button class="tab-btn active" id="tab-btn-rest" onclick="switchTab('rest')">Direct REST</button>
                        <button class="tab-btn" id="tab-btn-mcp" onclick="switchTab('mcp')">AI MCP Tool</button>
                    </div>
                </div>
                <div class="card-body">
                    <!-- REST Tab -->
                    <div id="tab-rest">
                        <div class="presets-title">One-Click Demonstration Presets</div>
                        <div class="preset-buttons">
                            <button class="preset-btn" onclick="fillPreset('nginx-cluster', 'dev', 'us-east-1', 'container', '{\"replica_count\": \"3\", \"container_image\": \"nginx:alpine\"}')">
                                <div>
                                    <strong>🐳 Nginx Microservice Cluster</strong>
                                    <div class="preset-desc">3 Replicas &middot; Container &middot; us-east-1</div>
                                </div>
                                <span style="color: var(--accent-blue);">Deploy &rarr;</span>
                            </button>
                            <button class="preset-btn" onclick="fillPreset('payments-vm', 'staging', 'us-west-2', 'vm', '{\"instance_type\": \"t3.medium\", \"os_image\": \"ubuntu-24.04\"}')">
                                <div>
                                    <strong>🖥️ Cloud Compute Virtual Machine</strong>
                                    <div class="preset-desc">t3.medium &middot; Linux VM &middot; us-west-2</div>
                                </div>
                                <span style="color: var(--accent-blue);">Deploy &rarr;</span>
                            </button>
                            <button class="preset-btn" onclick="fillPreset('auth-lambda', 'production', 'eu-west-1', 'serverless', '{\"runtime\": \"nodejs20.x\", \"memory_size\": \"512\"}')">
                                <div>
                                    <strong>⚡ Serverless Lambda Function</strong>
                                    <div class="preset-desc">Node.js 20 &middot; 512MB &middot; eu-west-1</div>
                                </div>
                                <span style="color: var(--accent-blue);">Deploy &rarr;</span>
                            </button>
                        </div>

                        <form id="deploy-form" onsubmit="submitDeploy(event)">
                            <div class="form-group">
                                <label for="projName">Project Name</label>
                                <input type="text" id="projName" value="live-demo-cluster" required>
                            </div>
                            <div class="form-row">
                                <div class="form-group">
                                    <label for="env">Environment</label>
                                    <select id="env">
                                        <option value="dev">dev</option>
                                        <option value="staging">staging</option>
                                        <option value="production">production</option>
                                    </select>
                                </div>
                                <div class="form-group">
                                    <label for="infraType">Infrastructure Type</label>
                                    <select id="infraType">
                                        <option value="container">container</option>
                                        <option value="vm">vm</option>
                                        <option value="serverless">serverless</option>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="region">Cloud Region</label>
                                <input type="text" id="region" value="us-east-1" required>
                            </div>
                            <div class="form-group">
                                <label for="parameters">Parameters (JSON)</label>
                                <textarea id="parameters" rows="3" style="font-family: var(--font-mono); font-size: 11px;">{"replica_count": "3", "container_image": "nginx:alpine"}</textarea>
                            </div>
                            <button type="submit" class="btn-primary" id="btn-submit-deploy">
                                <span>⚡ Dispatch Deployment (POST /api/deploy)</span>
                            </button>
                        </form>
                    </div>

                    <!-- AI MCP Tab -->
                    <div id="tab-mcp" style="display: none;">
                        <div style="font-size: 12px; color: var(--text-secondary); margin-bottom: 14px; background: rgba(59,130,246,0.1); padding: 10px; border-radius: 8px; border: 1px solid rgba(59,130,246,0.2);">
                            <strong>🤖 Model Context Protocol (MCP) Simulator</strong><br>
                            Simulates AI assistants (GitHub Copilot / Claude Desktop) calling DevOps Commander tools directly.
                        </div>
                        <div class="form-group">
                            <label for="mcpTool">Select AI MCP Tool</label>
                            <select id="mcpTool" onchange="updateMcpPayload()">
                                <option value="deploy_infrastructure">deploy_infrastructure (Provision resources)</option>
                                <option value="list_deployments">list_deployments (Query all records)</option>
                                <option value="get_deployment_status">get_deployment_status (Query status by ID)</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label for="mcpArgs">Tool Arguments (JSON)</label>
                            <textarea id="mcpArgs" rows="6" style="font-family: var(--font-mono); font-size: 11px;">{
  "projectName": "ai-copilot-cluster",
  "environment": "staging",
  "region": "us-west-2",
  "infrastructureType": "container",
  "parameters": {
    "replica_count": "2"
  }
}</textarea>
                        </div>
                        <button type="button" class="btn-primary" onclick="callMcpTool()" style="background: linear-gradient(135deg, #7c3aed, #6d28d9);">
                            <span>🤖 Invoke MCP Tool (POST /mcp/tools/call)</span>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Right Column: Live Deployments Table & Terminal Logs -->
            <div>
                <div class="card">
                    <div class="card-header">
                        <div class="card-title">📜 Deployment Audit Ledger (SQLite)</div>
                        <div style="font-size: 12px; color: var(--text-secondary);">Click any row to inspect live logs</div>
                    </div>
                    <div class="card-body" style="padding: 0;">
                        <div class="table-responsive">
                            <table id="deployments-table">
                                <thead>
                                    <tr>
                                        <th>Status</th>
                                        <th>Project</th>
                                        <th>Env / Region</th>
                                        <th>Type</th>
                                        <th>Created</th>
                                        <th>Action</th>
                                    </tr>
                                </thead>
                                <tbody id="deployments-tbody">
                                    <tr>
                                        <td colspan="6" class="empty-state">
                                            <div class="empty-icon">⏳</div>
                                            Loading deployment records from SQLite...
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Live Streaming Terminal Console -->
                <div class="terminal-container" id="terminal-box">
                    <div class="terminal-header">
                        <div class="terminal-dots">
                            <div class="terminal-dot red"></div>
                            <div class="terminal-dot yellow"></div>
                            <div class="terminal-dot green"></div>
                        </div>
                        <div class="terminal-title" id="terminal-title">Terminal: Select a deployment to stream live Terraform logs</div>
                        <button class="btn-sm" onclick="clearTerminal()">Clear</button>
                    </div>
                    <div class="terminal-body" id="terminal-logs">
                        <div style="color: #64748b;">// Real-time execution logs from SQLite and TerraformRunner will stream here automatically.</div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let deployments = [];
        let activeDeploymentId = null;
        let pollTimer = null;

        function switchTab(tab) {
            document.getElementById('tab-rest').style.display = tab === 'rest' ? 'block' : 'none';
            document.getElementById('tab-mcp').style.display = tab === 'mcp' ? 'block' : 'none';
            document.getElementById('tab-btn-rest').className = tab === 'rest' ? 'tab-btn active' : 'tab-btn';
            document.getElementById('tab-btn-mcp').className = tab === 'mcp' ? 'tab-btn active' : 'tab-btn';
        }

        function fillPreset(name, env, region, type, params) {
            document.getElementById('projName').value = name;
            document.getElementById('env').value = env;
            document.getElementById('region').value = region;
            document.getElementById('infraType').value = type;
            document.getElementById('parameters').value = params;
        }

        function updateMcpPayload() {
            const tool = document.getElementById('mcpTool').value;
            const argsEl = document.getElementById('mcpArgs');
            if (tool === 'deploy_infrastructure') {
                argsEl.value = JSON.stringify({
                    projectName: 'copilot-agent-cluster',
                    environment: 'staging',
                    region: 'us-west-2',
                    infrastructureType: 'container',
                    parameters: { replica_count: '2' }
                }, null, 2);
            } else if (tool === 'get_deployment_status') {
                argsEl.value = JSON.stringify({
                    deploymentId: activeDeploymentId || "00000000-0000-0000-0000-000000000000"
                }, null, 2);
            } else {
                argsEl.value = "{}";
            }
        }

        async function fetchDeployments(userTriggered = false) {
            try {
                const res = await fetch('/api/deployments');
                if (!res.ok) return;
                deployments = await res.json();
                renderDeployments();
                updateStats();

                if (activeDeploymentId) {
                    await fetchDeploymentLogs(activeDeploymentId);
                }
            } catch (err) {
                console.error("Failed to fetch deployments:", err);
            }
        }

        function updateStats() {
            document.getElementById('stat-total').innerText = deployments.length;
            const successCount = deployments.filter(d => d.status === 2 || d.status === "Completed").length;
            const activeCount = deployments.filter(d => d.status === 0 || d.status === 1 || d.status === "Pending" || d.status === "Provisioning").length;
            document.getElementById('stat-success').innerText = successCount;
            document.getElementById('stat-active').innerText = activeCount;
        }

        function getStatusBadge(status) {
            // Status enum: 0=Pending, 1=Provisioning, 2=Completed, 3=Failed, 4=Cancelled
            if (status === 0 || status === 'Pending') {
                return '<span class="status-pill status-pending">🟡 Pending</span>';
            } else if (status === 1 || status === 'Provisioning') {
                return '<span class="status-pill status-provisioning">🔵 Provisioning...</span>';
            } else if (status === 2 || status === 'Completed') {
                return '<span class="status-pill status-completed">🟢 Completed</span>';
            } else if (status === 3 || status === 'Failed') {
                return '<span class="status-pill status-failed">🔴 Failed</span>';
            } else {
                return '<span class="status-pill status-failed">⚪ Cancelled</span>';
            }
        }

        function renderDeployments() {
            const tbody = document.getElementById('deployments-tbody');
            if (!deployments || deployments.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="empty-state"><div class="empty-icon">📂</div>No deployments found. Dispatch one above!</td></tr>';
                return;
            }

            tbody.innerHTML = deployments.map(d => {
                const req = d.request || {};
                const createdTime = d.createdAt ? new Date(d.createdAt).toLocaleTimeString() : 'N/A';
                const isSelected = d.id === activeDeploymentId;
                return `
                    <tr style="cursor: pointer; ${isSelected ? 'background-color: rgba(59, 130, 246, 0.15);' : ''}" onclick="selectDeployment('${d.id}')">
                        <td>${getStatusBadge(d.status)}</td>
                        <td><strong>${escapeHtml(req.projectName || 'Unnamed')}</strong></td>
                        <td>${escapeHtml(req.environment || 'N/A')} / <span style="color: var(--text-muted);">${escapeHtml(req.region || 'N/A')}</span></td>
                        <td><span style="font-family: var(--font-mono); font-size: 11px;">${escapeHtml(req.infrastructureType || 'N/A')}</span></td>
                        <td><span style="color: var(--text-muted);">${createdTime}</span></td>
                        <td>
                            <button class="btn-sm" onclick="event.stopPropagation(); selectDeployment('${d.id}')">
                                🔍 Logs
                            </button>
                        </td>
                    </tr>
                `;
            }).join('');
        }

        async function selectDeployment(id) {
            activeDeploymentId = id;
            document.getElementById('terminal-title').innerText = 'Terminal: Deployment ' + id;
            renderDeployments();
            await fetchDeploymentLogs(id);
        }

        async function fetchDeploymentLogs(id) {
            try {
                const res = await fetch('/api/deploy/' + id);
                if (!res.ok) return;
                const state = await res.json();
                const logsBox = document.getElementById('terminal-logs');

                if (!state.executionLog || state.executionLog.length === 0) {
                    logsBox.innerHTML = '<div style="color: #64748b;">No log lines recorded yet. Waiting for background worker...</div>';
                    return;
                }

                logsBox.innerHTML = state.executionLog.map(item => {
                    const time = item.timestamp ? new Date(item.timestamp).toLocaleTimeString() : '';
                    const phase = item.phase || 'Info';
                    return `
                        <div class="log-entry">
                            <span class="log-time">[${time}]</span>
                            <span class="log-phase phase-${escapeHtml(phase)}">${escapeHtml(phase)}</span>
                            <span class="log-msg">${escapeHtml(item.message)}</span>
                        </div>
                    `;
                }).join('');

                logsBox.scrollTop = logsBox.scrollHeight;
            } catch (err) {
                console.error("Error fetching logs for " + id, err);
            }
        }

        async function submitDeploy(e) {
            e.preventDefault();
            const btn = document.getElementById('btn-submit-deploy');
            btn.disabled = true;
            btn.innerHTML = '<span>⏳ Enqueuing to Background Worker...</span>';

            let parameters = {};
            try {
                parameters = JSON.parse(document.getElementById('parameters').value || '{}');
            } catch {
                alert("Parameters must be valid JSON");
                btn.disabled = false;
                btn.innerHTML = '<span>⚡ Dispatch Deployment (POST /api/deploy)</span>';
                return;
            }

            const payload = {
                projectName: document.getElementById('projName').value,
                environment: document.getElementById('env').value,
                region: document.getElementById('region').value,
                infrastructureType: document.getElementById('infraType').value,
                parameters: parameters,
                requestedBy: "web-dashboard"
            };

            try {
                const res = await fetch('/api/deploy', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });

                if (res.status === 202) {
                    const data = await res.json();
                    activeDeploymentId = data.deploymentId;
                    await fetchDeployments();
                    selectDeployment(data.deploymentId);
                } else {
                    const err = await res.json();
                    alert("Validation Error: " + (err.error || JSON.stringify(err)));
                }
            } catch (err) {
                alert("Network error: " + err.message);
            } finally {
                btn.disabled = false;
                btn.innerHTML = '<span>⚡ Dispatch Deployment (POST /api/deploy)</span>';
            }
        }

        async function callMcpTool() {
            const tool = document.getElementById('mcpTool').value;
            let args = {};
            try {
                args = JSON.parse(document.getElementById('mcpArgs').value || '{}');
            } catch {
                alert("Arguments must be valid JSON");
                return;
            }

            const payload = { name: tool, arguments: args };
            try {
                const res = await fetch('/mcp/tools/call', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const result = await res.json();
                alert("MCP Response:\n" + JSON.stringify(result, null, 2));
                await fetchDeployments();
            } catch (err) {
                alert("MCP Call Error: " + err.message);
            }
        }

        function clearTerminal() {
            document.getElementById('terminal-logs').innerHTML = '<div style="color: #64748b;">// Cleared. Waiting for updates...</div>';
        }

        function escapeHtml(text) {
            if (!text) return '';
            return text.toString()
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        }

        // Initialize & Poll every 2 seconds
        fetchDeployments();
        setInterval(fetchDeployments, 2000);
    </script>
</body>
</html>
""";
}
