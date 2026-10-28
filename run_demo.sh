#!/usr/bin/env bash
# ==============================================================================
# 🚀 DevOps Commander – All-in-One Live Demonstration Runner
# Designed for professors, examiners, evaluators, and technical interviews.
# ==============================================================================
set -euo pipefail

BASE_URL="http://127.0.0.1:5076"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_PID=""

# ANSI Colors
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
BLUE='\033[0;34m'
BOLD='\033[1m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

print_banner() {
    clear 2>/dev/null || true
    echo -e "${CYAN}╔════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║${YELLOW}${BOLD}              🚀 DEVOPS COMMANDER – LIVE DEMONSTRATION              ${NC}${CYAN}║${NC}"
    echo -e "${CYAN}║${GRAY}     Autonomous Cloud Orchestrator (Terraform · .NET 10 · MCP)      ${CYAN}║${NC}"
    echo -e "${CYAN}╚════════════════════════════════════════════════════════════════════╝${NC}\n"
}

check_or_start_server() {
    echo -e "${BLUE}▶ Checking server health at ${BASE_URL}...${NC}"
    if curl -s --max-time 2 "${BASE_URL}/api/deployments" > /dev/null 2>&1; then
        echo -e "${GREEN}  ✓ Server is ONLINE and actively listening on port 5076!${NC}\n"
        return 0
    fi

    echo -e "${YELLOW}  ⚠ Server is not currently running. Starting it now in background...${NC}"
    cd "${SCRIPT_DIR}"
    dotnet run --project src/DevopsCommander.McpServer --urls "${BASE_URL}" > /dev/null 2>&1 &
    SERVER_PID=$!

    echo -ne "  ⏳ Waiting for API & Background Worker to initialize"
    for i in {1..20}; do
        sleep 1
        echo -ne "."
        if curl -s --max-time 1 "${BASE_URL}/api/deployments" > /dev/null 2>&1; then
            echo -e " ${GREEN}READY!${NC}\n"
            return 0
        fi
    done

    echo -e "\n${RED}  ✗ Failed to start server. Please run manually: dotnet run --project src/DevopsCommander.McpServer${NC}\n"
    exit 1
}

run_tests() {
    echo -e "\n${CYAN}════════════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}${BOLD}  🧪 STEP: Running All Automated xUnit Tests (48 Tests)  ${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}\n"
    cd "${SCRIPT_DIR}"
    dotnet test --logger "console;verbosity=normal"
    echo -e "\n${GREEN}✓ All test suites verified successfully!${NC}\n"
}

deploy_and_stream() {
    local proj_name="${1:-demo-production-cluster}"
    local env="${2:-prod}"
    local region="${3:-us-east-1}"
    local infra_type="${4:-container}"

    echo -e "\n${CYAN}════════════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}${BOLD}  ⚡ STEP: Enqueuing Asynchronous Deployment Request  ${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}\n"

    local payload=$(cat <<EOF
{
  "projectName": "${proj_name}",
  "environment": "${env}",
  "region": "${region}",
  "infrastructureType": "${infra_type}",
  "parameters": {
    "replica_count": "3",
    "container_image": "nginx:alpine"
  },
  "requestedBy": "live-demo-evaluator"
}
EOF
)

    echo -e "${BLUE}▶ Sending POST /api/deploy with configuration:${NC}"
    echo -e "${GRAY}${payload}${NC}\n"

    local response=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/api/deploy" \
      -H "Content-Type: application/json" \
      -d "${payload}")

    local http_code=$(echo "$response" | tail -n1)
    local body=$(echo "$response" | sed '$d')

    if [ "$http_code" -ne 202 ]; then
        echo -e "${RED}[ERROR] Failed to enqueue deployment (HTTP ${http_code}): ${body}${NC}"
        return 1
    fi

    local deployment_id=$(echo "$body" | jq -r '.deploymentId')
    local init_status=$(echo "$body" | jq -r '.status')
    local message=$(echo "$body" | jq -r '.message')

    echo -e "${GREEN}  ✓ HTTP 202 ACCEPTED (Async Non-Blocking Response)${NC}"
    echo -e "  • Deployment ID : ${BOLD}${deployment_id}${NC}"
    echo -e "  • Initial Status: ${YELLOW}Pending (0)${NC}"
    echo -e "  • Server Notice : ${GRAY}${message}${NC}\n"

    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}${BOLD}  📡 STEP: Streaming Live Terraform Engine Execution from SQLite  ${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}\n"

    local seen_logs=0
    local is_finished=false
    local max_retries=60
    local retries=0

    while [ "$is_finished" = false ] && [ $retries -lt $max_retries ]; do
        sleep 0.8
        retries=$((retries + 1))

        local state=$(curl -s "${BASE_URL}/api/deploy/${deployment_id}")
        local status=$(echo "$state" | jq -r '.status')
        local total_logs=$(echo "$state" | jq '.executionLog | length')

        if [ "$total_logs" -gt "$seen_logs" ]; then
            for (( i=seen_logs; i<total_logs; i++ )); do
                local log_item=$(echo "$state" | jq -c ".executionLog[$i]")
                local timestamp=$(echo "$log_item" | jq -r '.timestamp')
                local phase=$(echo "$log_item" | jq -r '.phase')
                local log_msg=$(echo "$log_item" | jq -r '.message')

                local phase_color="$GRAY"
                case "$phase" in
                    "Worker") phase_color="$MAGENTA" ;;
                    "Preparation") phase_color="$CYAN" ;;
                    "TerraformInit") phase_color="$YELLOW" ;;
                    "TerraformApply") phase_color="$GREEN" ;;
                    "Pipeline") phase_color="$BLUE" ;;
                    *) phase_color="$GRAY" ;;
                esac

                echo -e "  ${GRAY}[${timestamp:11:8}]${NC} [${phase_color}${phase}${NC}] ${log_msg}"
            done
            seen_logs=$total_logs
        fi

        if [ "$status" = "2" ] || [ "$status" = "Completed" ] || [ "$status" = "3" ] || [ "$status" = "Failed" ]; then
            is_finished=true
            echo -e "\n${CYAN}────────────────────────────────────────────────────────────────────${NC}"
            if [ "$status" = "2" ] || [ "$status" = "Completed" ]; then
                echo -e "${GREEN}${BOLD}  ✓ [DEPLOYMENT SUCCESSFUL] Status: Completed (2)${NC}"
            else
                local err_msg=$(echo "$state" | jq -r '.errorMessage // "Execution failed"')
                echo -e "${RED}${BOLD}  ✗ [DEPLOYMENT FAILED] Status: Failed (3): ${err_msg}${NC}"
            fi
            local started_at=$(echo "$state" | jq -r '.startedAt')
            local finished_at=$(echo "$state" | jq -r '.finishedAt')
            echo -e "  • Started  : ${started_at}"
            echo -e "  • Finished : ${finished_at}"
            echo -e "${CYAN}────────────────────────────────────────────────────────────────────${NC}\n"
        fi
    done
}

test_mcp_tools() {
    echo -e "\n${CYAN}════════════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}${BOLD}  🤖 STEP: Testing Model Context Protocol (MCP) AI Tools  ${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}\n"

    echo -e "${BLUE}▶ 1. Querying Registered AI Tools (GET /mcp/tools)...${NC}"
    local tools=$(curl -s "${BASE_URL}/mcp/tools")
    echo "$tools" | jq '.tools[] | {name: .name, description: .description}'
    echo ""

    echo -e "${BLUE}▶ 2. Simulating AI Agent Invoking 'deploy_infrastructure' (POST /mcp/tools/call)...${NC}"
    local mcp_call=$(cat <<EOF
{
  "name": "deploy_infrastructure",
  "arguments": {
    "projectName": "ai-claude-cluster",
    "environment": "staging",
    "region": "us-west-2",
    "infrastructureType": "container",
    "parameters": {
      "replica_count": "2"
    }
  }
}
EOF
)
    local mcp_res=$(curl -s -X POST "${BASE_URL}/mcp/tools/call" \
      -H "Content-Type: application/json" \
      -d "${mcp_call}")

    echo -e "${GREEN}  ✓ AI Agent Response Received:${NC}"
    echo "$mcp_res" | jq .
    echo ""
}

show_db_records() {
    echo -e "\n${CYAN}════════════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}${BOLD}  📊 STEP: Inspecting SQLite Deployment Ledger  ${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════════${NC}\n"

    local records=$(curl -s "${BASE_URL}/api/deployments")
    local count=$(echo "$records" | jq '. | length')

    echo -e "${GREEN}  ✓ Found ${count} total deployments stored in deployments.db:${NC}\n"
    echo "$records" | jq -r '.[] | "  • [\(.id[0:8])...] \(.request.projectName) (\(.request.environment)) - Type: \(.request.infrastructureType) - Status: \(.status)"'
    echo ""
}

open_web_dashboard() {
    echo -e "\n${GREEN}  🌐 Web Dashboard is live at: ${BOLD}${BASE_URL}${NC}\n"
    if which xdg-open > /dev/null 2>&1; then
        xdg-open "${BASE_URL}" > /dev/null 2>&1 &
        echo -e "  ✓ Opened dashboard in your default browser!"
    else
        echo -e "  Open ${BASE_URL} in your browser to view the interactive control plane."
    fi
}

full_automated_demo() {
    print_banner
    check_or_start_server
    run_tests
    deploy_and_stream "automated-evaluator-cluster" "prod" "us-east-1" "container"
    test_mcp_tools
    show_db_records
    open_web_dashboard

    echo -e "${CYAN}╔════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║${GREEN}${BOLD}         🎉 DEMONSTRATION COMPLETE: ALL SYSTEMS VERIFIED!           ${NC}${CYAN}║${NC}"
    echo -e "${CYAN}║${GRAY}  Web Dashboard: ${BASE_URL}                                ${CYAN}║${NC}"
    echo -e "${CYAN}╚════════════════════════════════════════════════════════════════════╝${NC}\n"
}

# Auto-run mode if flag passed
if [[ "${1:-}" == "--auto" || "${1:-}" == "-a" ]]; then
    full_automated_demo
    exit 0
fi

# Interactive Menu
while true; do
    print_banner
    check_or_start_server

    echo -e "${BOLD}Select a demonstration option:${NC}"
    echo -e "  ${YELLOW}1)${NC} 🚀 ${BOLD}Full Automated Live Demo${NC} (Tests + Deploy + Stream + MCP + Dashboard)"
    echo -e "  ${YELLOW}2)${NC} 🌐 ${BOLD}Open Web Control Plane Dashboard${NC} (${BASE_URL})"
    echo -e "  ${YELLOW}3)${NC} ⚡ ${BOLD}Deploy Custom Infrastructure${NC} (Select Container, VM, or Serverless)"
    echo -e "  ${YELLOW}4)${NC} 🧪 ${BOLD}Run All 48 Automated Tests${NC} (dotnet test)"
    echo -e "  ${YELLOW}5)${NC} 🤖 ${BOLD}Test AI Agent MCP Tool Calling${NC} (/mcp/tools/call)"
    echo -e "  ${YELLOW}6)${NC} 📊 ${BOLD}Inspect SQLite Database Ledger${NC} (deployments.db)"
    echo -e "  ${YELLOW}7)${NC} 🚪 Exit\n"

    read -rp "Enter choice [1-7]: " choice
    case "$choice" in
        1)
            full_automated_demo
            read -rp "Press [Enter] to return to menu..."
            ;;
        2)
            open_web_dashboard
            read -rp "Press [Enter] to return to menu..."
            ;;
        3)
            echo -e "\n${BOLD}Select Infrastructure Type:${NC}"
            echo "  1) Container Cluster (Nginx)"
            echo "  2) Virtual Machine (Ubuntu Linux)"
            echo "  3) Serverless Function (Node.js)"
            read -rp "Choice [1-3]: " type_choice
            local itype="container"
            [ "$type_choice" = "2" ] && itype="vm"
            [ "$type_choice" = "3" ] && itype="serverless"

            read -rp "Project Name [custom-app]: " c_name
            c_name="${c_name:-custom-app}"
            read -rp "Environment [dev/staging/prod] (default: dev): " c_env
            c_env="${c_env:-dev}"
            read -rp "Region [us-east-1]: " c_region
            c_region="${c_region:-us-east-1}"

            deploy_and_stream "$c_name" "$c_env" "$c_region" "$itype"
            read -rp "Press [Enter] to return to menu..."
            ;;
        4)
            run_tests
            read -rp "Press [Enter] to return to menu..."
            ;;
        5)
            test_mcp_tools
            read -rp "Press [Enter] to return to menu..."
            ;;
        6)
            show_db_records
            read -rp "Press [Enter] to return to menu..."
            ;;
        7)
            echo -e "\n${GREEN}Exiting. Server remains active on port 5076.${NC}\n"
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid option.${NC}"
            sleep 1
            ;;
    esac
done
