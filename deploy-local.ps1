#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy Ticketing System locally with Docker Compose
.DESCRIPTION
    This script deploys the full ticketing system stack (database, API, frontend) 
    locally using docker-compose.local.yml with demo data seeding.
#>

param(
    [switch]$Clean,
    [switch]$Build,
    [switch]$Logs,
    [switch]$Stop,
    [switch]$Status
)

$ErrorActionPreference = "Stop"
$ComposeFile = "docker-compose.local.yml"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " IronPack Ticketing System - Local Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check Docker is running
try {
    docker ps | Out-Null
} catch {
    Write-Host "[ERROR] Docker is not running!" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
    exit 1
}

if ($Status) {
    Write-Host "[Container Status]" -ForegroundColor Cyan
    docker compose -f $ComposeFile ps
    exit 0
}

if ($Stop) {
    Write-Host "[Stopping containers...]" -ForegroundColor Yellow
    docker compose -f $ComposeFile down
    Write-Host "[SUCCESS] Containers stopped" -ForegroundColor Green
    exit 0
}

if ($Clean) {
    Write-Host "[Cleaning up - removing containers AND volumes]" -ForegroundColor Yellow
    Write-Host "[WARNING] This will DELETE all data in the database!" -ForegroundColor Red
    $confirm = Read-Host "Type 'yes' to confirm"
    if ($confirm -ne "yes") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    docker compose -f $ComposeFile down -v
    Write-Host "[SUCCESS] Cleanup complete" -ForegroundColor Green
    exit 0
}

if ($Logs) {
    Write-Host "[Showing logs - Ctrl+C to exit]" -ForegroundColor Cyan
    docker compose -f $ComposeFile logs -f
    exit 0
}

# Default: Deploy
Write-Host "[Starting deployment...]" -ForegroundColor Green
Write-Host ""

if ($Build) {
    Write-Host "[Building containers...]" -ForegroundColor Cyan
    docker compose -f $ComposeFile up -d --build
} else {
    docker compose -f $ComposeFile up -d
}

Write-Host ""
Write-Host "[Waiting for services to be ready...]" -ForegroundColor Cyan
Start-Sleep -Seconds 5

# Check health
$maxAttempts = 30
$attempt = 0
$apiReady = $false

while ($attempt -lt $maxAttempts -and -not $apiReady) {
    $attempt++
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 2 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
        }
    } catch {
        Write-Host "  Attempt $attempt/$maxAttempts - API not ready yet..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "[Service URLs]" -ForegroundColor Cyan
Write-Host "  Frontend:  http://localhost:8081" -ForegroundColor White
Write-Host "  API:       http://localhost:8080" -ForegroundColor White
Write-Host "  Swagger:   http://localhost:8080/swagger" -ForegroundColor White
Write-Host "  Health:    http://localhost:8080/health" -ForegroundColor White
Write-Host ""
Write-Host "[Demo Login Credentials]" -ForegroundColor Cyan
Write-Host "  Admin:       admin@ironpack.pl / IronPack2026!" -ForegroundColor White
Write-Host "  Team Leader: teamlead1@ironpack.pl / IronPack2026!" -ForegroundColor White
Write-Host "  Support:     support1@ironpack.pl / IronPack2026!" -ForegroundColor White
Write-Host "  Employee:    employee1@ironpack.pl / IronPack2026!" -ForegroundColor White
Write-Host ""
Write-Host "[Demo Data]" -ForegroundColor Cyan
Write-Host "  49 tickets seeded with attachments and comments" -ForegroundColor White
Write-Host "  Check: TEST-DATA-REFERENCE.txt (in API container)" -ForegroundColor White
Write-Host ""
Write-Host "[Useful Commands]" -ForegroundColor Cyan
Write-Host "  View logs:     .\deploy-local.ps1 -Logs" -ForegroundColor White
Write-Host "  Stop:          .\deploy-local.ps1 -Stop" -ForegroundColor White
Write-Host "  Clean & reset: .\deploy-local.ps1 -Clean" -ForegroundColor White
Write-Host "  Rebuild:       .\deploy-local.ps1 -Build" -ForegroundColor White
Write-Host "  Status:        .\deploy-local.ps1 -Status" -ForegroundColor White
Write-Host ""

if ($apiReady) {
    Write-Host "[SUCCESS] All services are healthy and ready!" -ForegroundColor Green
} else {
    Write-Host "[WARNING] API might still be starting. Check logs with: .\deploy-local.ps1 -Logs" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Ready for university demo!" -ForegroundColor Cyan
Write-Host ""
