# ============================================
# Check Database State Script
# ============================================
# Purpose: Quickly check how many tickets and users exist in the database
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DATABASE STATE CHECK" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Create temporary SQL file for complex queries
$tempSqlFile = [System.IO.Path]::GetTempFileName()
$sqlContent = @"
SELECT 
    'Tickets' as "Entity",
    COUNT(*)::text as "Count",
    CASE 
        WHEN COUNT(*) = 0 THEN 'Empty - Ready for seeding'
        WHEN COUNT(*) = 25 THEN 'Clean seed (25 tickets)'
        ELSE 'Unexpected count - may have duplicates'
    END as "Status"
FROM "Tickets"
UNION ALL
SELECT 
    'Users',
    COUNT(*)::text,
    CASE 
        WHEN COUNT(*) = 1 THEN 'Only admin user (no demo data)'
        WHEN COUNT(*) = 9 THEN 'Clean seed (8 demo users + admin)'
        ELSE 'Unexpected count'
    END
FROM "Users"
UNION ALL
SELECT 
    'Categories',
    COUNT(*)::text,
    CASE 
        WHEN COUNT(*) = 0 THEN 'Empty - should have 3 categories'
        WHEN COUNT(*) = 3 THEN 'Correct (IT, Logistics, Administration)'
        ELSE 'Unexpected count'
    END
FROM "Categories"
UNION ALL
SELECT 
    'Comments',
    COUNT(*)::text,
    '(varies based on seed data)'
FROM "TicketComments"
UNION ALL
SELECT 
    'Attachments',
    COUNT(*)::text,
    '(varies based on seed data)'
FROM "TicketFiles";
"@

Set-Content -Path $tempSqlFile -Value $sqlContent -Encoding UTF8

try {
    Write-Host "Querying database..." -ForegroundColor Green
    Write-Host ""
    
    # Execute main query
    Get-Content $tempSqlFile | docker exec -i ticketing-system-db-1 psql -U admin -d ticketing_system
    
    Write-Host ""
    
    # Tickets by Status query
    $statusSql = @"
SELECT 
    CASE "Status"
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Open'
        WHEN 2 THEN 'InProcess'
        WHEN 3 THEN 'Resolved'
        WHEN 4 THEN 'Cancelled'
        WHEN 5 THEN 'Postponed'
        WHEN 6 THEN 'Returned'
        ELSE 'Unknown'
    END as "Status",
    COUNT(*)::text as "Count"
FROM "Tickets"
GROUP BY "Status"
ORDER BY "Status";
"@
    
    Set-Content -Path $tempSqlFile -Value $statusSql -Encoding UTF8
    
    Write-Host "Tickets by Status:" -ForegroundColor Cyan
    Get-Content $tempSqlFile | docker exec -i ticketing-system-db-1 psql -U admin -d ticketing_system
    Write-Host ""
    
    Write-Host "✅ Database state check complete!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
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
