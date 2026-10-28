#!/usr/bin/env bash
# DevOps Commander – Live Terminal Demonstration Script for Linux / Ubuntu
set -euo pipefail

BASE_URL="${1:-http://localhost:5076}"
PROJECT_NAME="${2:-ubuntu-live-cluster}"
ENVIRONMENT="${3:-dev}"
REGION="${4:-us-east-1}"
INFRA_TYPE="${5:-container}"

# ANSI color codes
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "\n${CYAN}========================================================${NC}"
echo -e "${YELLOW}       DevOps Commander – End-to-End Live Demo        ${NC}"
echo -e "${CYAN}========================================================${NC}\n"

# Check if server is running
if ! curl -s --max-time 3 "${BASE_URL}/api/deployments" > /dev/null 2>&1; then
    echo -e "${YELLOW}[WARN] Server is not running on ${BASE_URL}.${NC}"
    echo -e "Please start the server in another terminal window using:"
    echo -e "${CYAN}  dotnet run --project src/DevopsCommander.McpServer${NC}\n"
    exit 1
fi

echo -e "${GREEN}[OK] Connected to DevOps Commander API at ${BASE_URL}${NC}\n"

# 1. Enqueue deployment
echo -e "${YELLOW}Step 1: Enqueuing deployment request...${NC}"
PAYLOAD=$(cat <<EOF
{
  "projectName": "${PROJECT_NAME}",
  "environment": "${ENVIRONMENT}",
  "region": "${REGION}",
  "infrastructureType": "${INFRA_TYPE}",
  "parameters": {
    "replica_count": "3",
    "container_image": "nginx:alpine"
  },
  "requestedBy": "live-demo-bash-script"
}
EOF
)

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/api/deploy" \
  -H "Content-Type: application/json" \
  -d "${PAYLOAD}")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -ne 202 ]; then
    echo -e "${RED}[ERROR] Failed to enqueue deployment (HTTP ${HTTP_CODE}): ${BODY}${NC}"
    exit 1
fi

DEPLOYMENT_ID=$(echo "$BODY" | jq -r '.deploymentId')
INITIAL_STATUS=$(echo "$BODY" | jq -r '.status')
MESSAGE=$(echo "$BODY" | jq -r '.message')

echo -e "${GREEN}  -> HTTP 202 Accepted!${NC}"
echo -e "  -> Deployment ID: ${DEPLOYMENT_ID}"
echo -e "  -> Initial Status: ${INITIAL_STATUS}"
echo -e "${GRAY}  -> Message: ${MESSAGE}${NC}\n"

# 2. Poll status in real time
echo -e "${YELLOW}Step 2: Polling deployment status & streaming execution logs...${NC}"
SEEN_LOGS=0
IS_FINISHED=false
MAX_RETRIES=40
RETRIES=0

while [ "$IS_FINISHED" = false ] && [ $RETRIES -lt $MAX_RETRIES ]; do
    sleep 1
    RETRIES=$((RETRIES + 1))

    STATE=$(curl -s "${BASE_URL}/api/deploy/${DEPLOYMENT_ID}")
    STATUS=$(echo "$STATE" | jq -r '.status')
    TOTAL_LOGS=$(echo "$STATE" | jq '.executionLog | length')

    if [ "$TOTAL_LOGS" -gt "$SEEN_LOGS" ]; then
        for (( i=SEEN_LOGS; i<TOTAL_LOGS; i++ )); do
            LOG_ITEM=$(echo "$STATE" | jq -c ".executionLog[$i]")
            TIMESTAMP=$(echo "$LOG_ITEM" | jq -r '.timestamp')
            PHASE=$(echo "$LOG_ITEM" | jq -r '.phase')
            LOG_MSG=$(echo "$LOG_ITEM" | jq -r '.message')

            case "$PHASE" in
                "Worker") PHASE_COLOR="$MAGENTA" ;;
                "Preparation") PHASE_COLOR="$CYAN" ;;
                "TerraformInit") PHASE_COLOR="$YELLOW" ;;
                "TerraformApply") PHASE_COLOR="$GREEN" ;;
                *) PHASE_COLOR="$GRAY" ;;
            esac

            echo -e "  [${TIMESTAMP}] [${PHASE_COLOR}${PHASE}${NC}] ${LOG_MSG}"
        done
        SEEN_LOGS=$TOTAL_LOGS
    fi

    # Status 2 = Completed, 3 = Failed, 4 = Cancelled
    if [ "$STATUS" = "2" ] || [ "$STATUS" = "Completed" ] || [ "$STATUS" = "3" ] || [ "$STATUS" = "Failed" ] || [ "$STATUS" = "4" ] || [ "$STATUS" = "Cancelled" ]; then
        IS_FINISHED=true
        echo -e "\n${YELLOW}Step 3: Final State Reached!${NC}"
        if [ "$STATUS" = "2" ] || [ "$STATUS" = "Completed" ]; then
            echo -e "${GREEN}  [SUCCESS] Deployment ${DEPLOYMENT_ID} COMPLETED!${NC}"
        else
            ERR_MSG=$(echo "$STATE" | jq -r '.errorMessage // "Unknown error"')
            echo -e "${RED}  [FAILED] Deployment ${DEPLOYMENT_ID} FAILED: ${ERR_MSG}${NC}"
        fi
        STARTED_AT=$(echo "$STATE" | jq -r '.startedAt')
        FINISHED_AT=$(echo "$STATE" | jq -r '.finishedAt')
        echo -e "${GRAY}  -> Started At:  ${STARTED_AT}${NC}"
        echo -e "${GRAY}  -> Finished At: ${FINISHED_AT}${NC}"
    fi
done

echo -e "\n${CYAN}========================================================${NC}"
echo -e "${GREEN}               Demonstration Complete!                 ${NC}"
echo -e "${CYAN}========================================================${NC}\n"
