# ============================================
# Clean Database Script
# ============================================
# Purpose: Delete all demo data to allow re-seeding with fresh 25 tickets
# Use this when you need to reset the database completely
# Parameters:
#   -Force              Skip confirmation prompt (auto-approve cleanup)
#   -DbContainer        PostgreSQL container name (default: ticketing-system-db-1)
# ============================================

param(
    [switch]$Force,
    [string]$DbContainer = 'ticketing-system-db-1'
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TICKETING SYSTEM - DATABASE CLEANUP" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "⚠️  WARNING: This will DELETE all tickets, users (except admin), categories, and related data!" -ForegroundColor Yellow
Write-Host ""

if (-not $Force) {
    $confirmation = Read-Host "Are you sure you want to continue? Type 'YES' to confirm"
    if ($confirmation -ne "YES") {
        Write-Host "❌ Cleanup cancelled." -ForegroundColor Red
        exit
    }
} else {
    Write-Host "Using -Force flag, proceeding without confirmation..." -ForegroundColor Green
}

Write-Host ""
Write-Host "Connecting to PostgreSQL database..." -ForegroundColor Green

# Create temporary SQL file
$tempSqlFile = [System.IO.Path]::GetTempFileName()
$sqlCommands = @"
-- Delete in correct order (respects foreign keys)
DELETE FROM "TicketHistories";
DELETE FROM "TicketFileContents";
DELETE FROM "TicketFiles";
DELETE FROM "TicketComments";
DELETE FROM "Tickets";
DELETE FROM "Users" WHERE "Email" != 'admin@ironpack.pl';
DELETE FROM "Categories";

-- Reset ID sequences to start from 1
ALTER SEQUENCE "Tickets_TicketId_seq" RESTART WITH 1;
ALTER SEQUENCE "Users_UserId_seq" RESTART WITH 2;
ALTER SEQUENCE "Categories_CategoryId_seq" RESTART WITH 1;
ALTER SEQUENCE "TicketComments_CommentId_seq" RESTART WITH 1;

-- Verify counts
SELECT 'Tickets' as "Table", COUNT(*) as "Count" FROM "Tickets"
UNION ALL
SELECT 'Users', COUNT(*) FROM "Users"
UNION ALL
SELECT 'Categories', COUNT(*) FROM "Categories"
UNION ALL
SELECT 'Comments', COUNT(*) FROM "TicketComments"
UNION ALL
SELECT 'Attachments', COUNT(*) FROM "TicketFiles";
"@

Set-Content -Path $tempSqlFile -Value $sqlCommands -Encoding UTF8

# Execute SQL via docker
try {
    Write-Host "Executing cleanup SQL commands..." -ForegroundColor Green
    Get-Content $tempSqlFile | docker exec -i ticketing-system-db-1 psql -U admin -d ticketing_system
    
    Write-Host ""
    Write-Host "✅ Database cleaned successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Restart the backend API server (dotnet watch)" -ForegroundColor White
    Write-Host "2. Watch console for seeding message (should see '25 tickets')" -ForegroundColor White
    Write-Host "3. Login and verify exactly 25 tickets exist" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ Error during cleanup: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Make sure PostgreSQL container is running: docker ps" -ForegroundColor White
    Write-Host "2. If container not running: docker start ticketing-system-db-1" -ForegroundColor White
    Write-Host "3. Container name may vary (ticketing-db or ticketing-system-db-1)" -ForegroundColor White
    Write-Host "4. Check container with: docker ps --filter 'ancestor=postgres:16'" -ForegroundColor White
}
finally {
    # Clean up temp file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}
