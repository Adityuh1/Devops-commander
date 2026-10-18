# DevOps Commander – Live Terminal Demonstration Script
param(
    [string]$BaseUrl = "http://localhost:5076",
    [string]$ProjectName = "live-demo-cluster",
    [string]$Environment = "dev",
    [string]$Region = "us-east-1",
    [string]$InfraType = "container"
)

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "       DevOps Commander – End-to-End Live Demo        " -ForegroundColor Yellow
Write-Host "========================================================`n" -ForegroundColor Cyan

# Check if server is running
try {
    $healthCheck = Invoke-RestMethod -Uri "$BaseUrl/api/deployments" -Method Get -TimeoutSec 3 -ErrorAction Stop
    Write-Host "[OK] Connected to DevOps Commander API at $BaseUrl`n" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Server is not running on $BaseUrl." -ForegroundColor Yellow
    Write-Host "Please start the server in another terminal window using:" -ForegroundColor White
    Write-Host "  dotnet run --project src/DevopsCommander.McpServer`n" -ForegroundColor Cyan
    exit 1
}

# 1. Enqueue deployment
Write-Host "Step 1: Enqueuing deployment request..." -ForegroundColor Yellow
$payload = @{
    projectName        = $ProjectName
    environment        = $Environment
    region             = $Region
    infrastructureType = $InfraType
    parameters         = @{
        replica_count   = "3"
        container_image = "nginx:alpine"
    }
    requestedBy        = "live-demo-script"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/deploy" -Method Post -Body $payload -ContentType "application/json"
    $deploymentId = $response.deploymentId
    Write-Host "  -> HTTP 202 Accepted!" -ForegroundColor Green
    Write-Host "  -> Deployment ID: $deploymentId" -ForegroundColor White
    Write-Host "  -> Initial Status: $($response.status)" -ForegroundColor White
    Write-Host "  -> Message: $($response.message)`n" -ForegroundColor Gray
} catch {
    Write-Host "[ERROR] Failed to enqueue deployment: $_" -ForegroundColor Red
    exit 1
}

# 2. Poll status in real time
Write-Host "Step 2: Polling deployment status & streaming execution logs..." -ForegroundColor Yellow
$seenLogs = 0
$isFinished = $false
$maxRetries = 30
$retries = 0

while (-not $isFinished -and $retries -lt $maxRetries) {
    Start-Sleep -Seconds 1
    $retries++

    $state = Invoke-RestMethod -Uri "$BaseUrl/api/deploy/$deploymentId" -Method Get
    $status = $state.status

    # Print new logs
    if ($state.executionLog -and $state.executionLog.Count -gt $seenLogs) {
        for ($i = $seenLogs; $i -lt $state.executionLog.Count; $i++) {
            $log = $state.executionLog[$i]
            $phaseColor = switch ($log.phase) {
                "Worker"         { "Magenta" }
                "Preparation"    { "Cyan" }
                "TerraformInit"  { "Yellow" }
                "TerraformApply" { "Green" }
                default          { "Gray" }
            }
            Write-Host "  [$($log.timestamp)] [$($log.phase)] $($log.message)" -ForegroundColor $phaseColor
        }
        $seenLogs = $state.executionLog.Count
    }

    if ($status -eq "Completed" -or $status -eq "Failed" -or $status -eq "Cancelled") {
        $isFinished = $true
        Write-Host "`nStep 3: Final State Reached!" -ForegroundColor Yellow
        if ($status -eq "Completed") {
            Write-Host "  [SUCCESS] Deployment $deploymentId COMPLETED!" -ForegroundColor Green
        } else {
            Write-Host "  [FAILED] Deployment $deploymentId FAILED: $($state.errorMessage)" -ForegroundColor Red
        }
        Write-Host "  -> Started At:  $($state.startedAt)" -ForegroundColor Gray
        Write-Host "  -> Finished At: $($state.finishedAt)" -ForegroundColor Gray
    }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "               Demonstration Complete!                 " -ForegroundColor Green
Write-Host "========================================================`n" -ForegroundColor Cyan
