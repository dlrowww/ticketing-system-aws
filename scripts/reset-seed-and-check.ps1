# ============================================
# Reset Database, Seed Data, and Check State
# ============================================
# Orchestrates: clean-database → seed-full → check-database-state
# Parameters:
#   -Seed              'demo' or 'full' (default: demo)
#   -SkipCleanup       Skip database cleanup step (default: false)
#   -Force             Auto-approve cleanup confirmation (default: false)
#   -DbContainer       PostgreSQL container name (default: ticketing-system-db-1)
# 
# Examples:
#   ./reset-seed-and-check.ps1                    # Demo data, with confirmation
#   ./reset-seed-and-check.ps1 -Seed full -Force  # Full data, auto-approve
#   ./reset-seed-and-check.ps1 -SkipCleanup       # Skip cleanup, just reseed
# ============================================

param(
    [ValidateSet('demo','full')]
    [string]$Seed = 'demo',
    [switch]$SkipCleanup,
    [switch]$Force,
    [string]$DbContainer = 'ticketing-system-db-1'
)

$scriptDir = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $scriptDir -Parent
$scriptsPath = Join-Path $repoRoot 'scripts'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RESET + SEED ($Seed) + DB CHECK" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean database (unless skipped)
if (-not $SkipCleanup) {
    Write-Host "[1/3] Running database cleanup..." -ForegroundColor Yellow
    Write-Host ""
    
    $cleanScript = Join-Path $scriptsPath 'clean-database.ps1'
    if (-not (Test-Path $cleanScript)) {
        Write-Host "❌ Error: clean-database.ps1 not found at $cleanScript" -ForegroundColor Red
        exit 1
    }
    
    & $cleanScript -DbContainer $DbContainer -Force:$Force
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Database cleanup failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
} else {
    Write-Host "[1/3] Skipping cleanup (--SkipCleanup set)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Seed database
Write-Host "[2/3] Seeding database with $Seed data..." -ForegroundColor Yellow
Write-Host ""

$seedScript = Join-Path $scriptsPath 'seed-full.ps1'
if (-not (Test-Path $seedScript)) {
    Write-Host "❌ Error: seed-full.ps1 not found at $seedScript" -ForegroundColor Red
    exit 1
}

$seedArgs = @()
if ($SkipCleanup) {
    $seedArgs += '-SkipCleanup'
}

# Set environment variable for demo vs full seed
if ($Seed -eq 'full') {
    $env:SEED_DATA_FILE = 'SeedData/full-seed-data.json'
} else {
    $env:SEED_DATA_FILE = 'SeedData/demo-data.json'
}

Write-Host "Set SEED_DATA_FILE=$($env:SEED_DATA_FILE)" -ForegroundColor Green

# Call seed script (it stops dotnet, starts backend, waits for completion, then stops backend)
& $seedScript @seedArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Seeding failed" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Check database state
Write-Host "[3/3] Running database state check..." -ForegroundColor Yellow
Write-Host ""

$checkScript = Join-Path $scriptsPath 'check-database-state.ps1'
if (-not (Test-Path $checkScript)) {
    Write-Host "❌ Error: check-database-state.ps1 not found at $checkScript" -ForegroundColor Red
    exit 1
}

& $checkScript

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "✅ Reset, seed, and check completed!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press Enter to exit" -ForegroundColor Cyan
[void](Read-Host)
