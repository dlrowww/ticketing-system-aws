# Full Seed Data Script
# Seeds 50 tickets with comprehensive test data
# Includes automatic database cleanup and sequence reset

param(
    [switch]$SkipCleanup
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Full Seed Data Setup (49 tickets)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean database (unless skipped)
if (-not $SkipCleanup) {
    Write-Host "[1/4] Cleaning database..." -ForegroundColor Yellow
    
    $cleanScript = @'
DELETE FROM "TicketHistories";
DELETE FROM "TicketFileContents";
DELETE FROM "TicketFiles";
DELETE FROM "TicketComments";
DELETE FROM "Tickets";
DELETE FROM "Users" WHERE "Email" != 'admin@ironpack.pl';
DELETE FROM "Categories";
ALTER SEQUENCE "Tickets_TicketId_seq" RESTART WITH 1;
ALTER SEQUENCE "Users_UserId_seq" RESTART WITH 2;
ALTER SEQUENCE "Categories_CategoryId_seq" RESTART WITH 1;
ALTER SEQUENCE "TicketComments_CommentId_seq" RESTART WITH 1;
ALTER SEQUENCE "TicketFiles_FileId_seq" RESTART WITH 1;
'@

    $cleanScript | docker exec -i ticketing-system-db-1 psql -U admin -d ticketing_system
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Database cleanup failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Database cleaned and sequences reset" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[1/4] Skipping database cleanup (--SkipCleanup flag set)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Set environment variable for full seed
Write-Host "[2/4] Configuring full seed mode..." -ForegroundColor Yellow
$env:SEED_DATA_FILE = "SeedData/full-seed-data.json"
Write-Host "  Set SEED_DATA_FILE = 'SeedData/full-seed-data.json'" -ForegroundColor Green
Write-Host ""

# Step 3: Kill existing backend process
Write-Host "[3/4] Stopping existing backend..." -ForegroundColor Yellow
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "  Backend stopped" -ForegroundColor Green
Write-Host ""

# Step 4: Start backend to trigger seeding
Write-Host "[4/4] Starting backend to seed data..." -ForegroundColor Yellow
Write-Host "  This will seed 50 tickets (may take 10-15 seconds)" -ForegroundColor Gray
Write-Host ""

Push-Location backend\TicketingSystem.Api

# Run backend, wait for seeding, then stop
$process = Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru -RedirectStandardOutput "..\..\seed-full-output.log"

# Wait for seeding to complete (look for specific log message)
$maxWait = 30  # seconds
$waited = 0
$seeded = $false

Write-Host "  Waiting for seeding to complete..." -ForegroundColor Gray

while ($waited -lt $maxWait -and -not $seeded) {
    Start-Sleep -Seconds 1
    $waited++
    
    if (Test-Path "..\..\seed-full-output.log") {
        $log = Get-Content "..\..\seed-full-output.log" -Raw
        
        # Check for completion messages
        if ($log -match "Created ticket #49" -or $log -match "SEEDING COMPLETED") {
            $seeded = $true
            Write-Host "  Seeding completed successfully!" -ForegroundColor Green
            break
        }
        
        # Check for errors
        if ($log -match "fail|error|exception" -and $log -notmatch "FailFast") {
            Write-Host "  ERROR detected in logs" -ForegroundColor Red
            $seeded = $false
            break
        }
    }
    
    # Show progress dots
    if ($waited % 5 -eq 0) {
        Write-Host "  Still waiting... ($waited seconds)" -ForegroundColor Gray
    }
}

# Stop the backend process
Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue

Pop-Location

Write-Host ""

if ($seeded) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "SUCCESS: Full seed data loaded!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run verification tests:" -ForegroundColor White
    Write-Host "     dotnet test --filter SeedDataVerificationTests" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Start backend normally:" -ForegroundColor White
    Write-Host "     cd backend\TicketingSystem.Api" -ForegroundColor Gray
    Write-Host "     dotnet watch" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Browse to http://localhost:5192/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "Database now contains:" -ForegroundColor Cyan
    Write-Host "  - 50 tickets" -ForegroundColor White
    Write-Host "  - 8 users" -ForegroundColor White
    Write-Host "  - 3 categories" -ForegroundColor White
    Write-Host "  - 33 tickets with comments" -ForegroundColor White
    Write-Host "  - 16 tickets with attachments" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "ERROR: Seeding may have failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check logs:" -ForegroundColor Yellow
    Write-Host "  seed-full-output.log" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Manual seeding:" -ForegroundColor Yellow
    Write-Host "  cd backend\TicketingSystem.Api" -ForegroundColor Gray
    Write-Host "  `$env:SEED_DATA_FILE = 'SeedData/full-seed-data.json'" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
    Write-Host ""
    exit 1
}
