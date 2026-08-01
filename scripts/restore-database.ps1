<#
.SYNOPSIS
    PostgreSQL Database Restoration Script for Ticketing System

.DESCRIPTION
    Restores a PostgreSQL database from a backup file created by backup-database.ps1.
    Supports both compressed (.gz) and uncompressed (.sql) backup files.
    Can restore to Docker container or native PostgreSQL installation.

.PARAMETER BackupFile
    Path to the backup file to restore (required)

.PARAMETER DatabaseName
    Name of the database to restore to. Default: ticketing

.PARAMETER DockerContainer
    Name of the Docker container running PostgreSQL. If specified, uses docker exec.
    Default: ticketing-pg

.PARAMETER UseDocker
    Switch to use Docker container for restoration. Default: $true

.PARAMETER DropExisting
    Switch to drop existing database before restoration. Default: $false
    WARNING: This will delete all current data!

.PARAMETER Host
    PostgreSQL host (for native installation). Default: localhost

.PARAMETER Port
    PostgreSQL port (for native installation). Default: 5432

.PARAMETER Username
    PostgreSQL username. Default: postgres

.EXAMPLE
    .\restore-database.ps1 -BackupFile ..\backups\ticketing_backup_20260103_120000.sql.gz
    Restores database from compressed backup using Docker

.EXAMPLE
    .\restore-database.ps1 -BackupFile ..\backups\ticketing_backup_20260103_120000.sql -DropExisting $true
    Drops existing database and restores from backup

.EXAMPLE
    .\restore-database.ps1 -BackupFile backup.sql -UseDocker $false -Host localhost -Username postgres
    Restores to native PostgreSQL installation

.NOTES
    Version: 1.0
    Author: Ticketing System
    Date: January 3, 2026
    
    WARNING: Database restoration will overwrite existing data!
    Always create a backup before restoration if you want to preserve current data.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "ticketing",
    
    [Parameter(Mandatory=$false)]
    [string]$DockerContainer = "ticketing-pg",
    
    [Parameter(Mandatory=$false)]
    [bool]$UseDocker = $true,
    
    [Parameter(Mandatory=$false)]
    [bool]$DropExisting = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$Host = "localhost",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 5432,
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "postgres"
)

# Error handling
$ErrorActionPreference = "Stop"

Write-Host "=== PostgreSQL Database Restoration ===" -ForegroundColor Cyan
Write-Host ""

# Check if backup file exists
if (-not (Test-Path $BackupFile)) {
    Write-Host "Error: Backup file not found: $BackupFile" -ForegroundColor Red
    exit 1
}

$BackupFilePath = Resolve-Path $BackupFile
$BackupFileSize = [math]::Round((Get-Item $BackupFilePath).Length / 1MB, 2)
$IsCompressed = $BackupFilePath -match '\.gz$'

Write-Host "Backup file: $BackupFilePath" -ForegroundColor White
Write-Host "File size: $BackupFileSize MB" -ForegroundColor White
Write-Host "Compressed: $(if ($IsCompressed) {'Yes'} else {'No'})" -ForegroundColor White
Write-Host "Target database: $DatabaseName" -ForegroundColor White
Write-Host ""

# Confirmation prompt
if ($DropExisting) {
    Write-Host "WARNING: This will DROP the existing database '$DatabaseName' and all its data!" -ForegroundColor Red
    Write-Host "Press Ctrl+C to cancel, or press Enter to continue..." -ForegroundColor Yellow
    Read-Host
} else {
    Write-Host "WARNING: This will restore data into '$DatabaseName'. Existing data may be overwritten." -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to cancel, or press Enter to continue..." -ForegroundColor Yellow
    Read-Host
}

Write-Host ""

try {
    # Decompress if needed
    $SqlFile = $BackupFilePath
    $TempFile = $null
    
    if ($IsCompressed) {
        Write-Host "Decompressing backup file..." -ForegroundColor Yellow
        
        # Check if gunzip is available
        $gunzip = Get-Command gunzip -ErrorAction SilentlyContinue
        if (-not $gunzip) {
            throw "gunzip not found. Please install gzip/gunzip (available in Git for Windows or WSL)."
        }
        
        # Create temporary decompressed file
        $TempFile = Join-Path $env:TEMP "ticketing_restore_temp.sql"
        
        # Decompress to temp file
        gunzip -c $BackupFilePath | Out-File -FilePath $TempFile -Encoding UTF8
        
        $SqlFile = $TempFile
        Write-Host "Decompressed successfully" -ForegroundColor Green
    }
    
    # Restore database
    if ($UseDocker) {
        Write-Host "Restoring to Docker container: $DockerContainer" -ForegroundColor Yellow
        
        # Check if container is running
        $containerStatus = docker ps --filter "name=$DockerContainer" --format "{{.Names}}"
        if ($containerStatus -ne $DockerContainer) {
            throw "Docker container '$DockerContainer' is not running. Start it with: docker start $DockerContainer"
        }
        
        # Drop existing database if requested
        if ($DropExisting) {
            Write-Host "Dropping existing database..." -ForegroundColor Yellow
            docker exec $DockerContainer psql -U postgres -c "DROP DATABASE IF EXISTS $DatabaseName;"
            docker exec $DockerContainer psql -U postgres -c "CREATE DATABASE $DatabaseName;"
            Write-Host "Database recreated" -ForegroundColor Green
        }
        
        # Copy SQL file to container
        Write-Host "Copying backup file to container..." -ForegroundColor Gray
        docker cp $SqlFile "${DockerContainer}:/tmp/restore.sql"
        
        # Execute restoration
        Write-Host "Restoring database (this may take a while)..." -ForegroundColor Yellow
        docker exec $DockerContainer psql -U postgres -d $DatabaseName -f /tmp/restore.sql --quiet
        
        # Cleanup temp file in container
        docker exec $DockerContainer rm /tmp/restore.sql
        
    } else {
        Write-Host "Restoring to native PostgreSQL installation" -ForegroundColor Yellow
        Write-Host "Host: ${Host}:${Port}" -ForegroundColor Gray
        
        # Check if psql is available
        $psql = Get-Command psql -ErrorAction SilentlyContinue
        if (-not $psql) {
            throw "psql not found in PATH. Please install PostgreSQL client tools or add to PATH."
        }
        
        # Drop existing database if requested
        if ($DropExisting) {
            Write-Host "Dropping existing database..." -ForegroundColor Yellow
            psql -h $Host -p $Port -U $Username -d postgres -c "DROP DATABASE IF EXISTS $DatabaseName;"
            psql -h $Host -p $Port -U $Username -d postgres -c "CREATE DATABASE $DatabaseName;"
            Write-Host "Database recreated" -ForegroundColor Green
        }
        
        # Execute restoration
        Write-Host "Restoring database (this may take a while)..." -ForegroundColor Yellow
        Get-Content $SqlFile | psql -h $Host -p $Port -U $Username -d $DatabaseName --quiet
    }
    
    Write-Host ""
    Write-Host "=== Restoration Summary ===" -ForegroundColor Cyan
    Write-Host "Status: SUCCESS" -ForegroundColor Green
    Write-Host "Database: $DatabaseName" -ForegroundColor White
    Write-Host "Backup file: $(Split-Path -Leaf $BackupFilePath)" -ForegroundColor White
    Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
    Write-Host ""
    Write-Host "Database restored successfully!" -ForegroundColor Green
    
    # Cleanup temp file
    if ($TempFile -and (Test-Path $TempFile)) {
        Remove-Item $TempFile -Force
    }
    
    exit 0
    
} catch {
    Write-Host ""
    Write-Host "=== Restoration Failed ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    if ($UseDocker) {
        Write-Host "  1. Check if Docker container is running: docker ps" -ForegroundColor Gray
        Write-Host "  2. Start container: docker start $DockerContainer" -ForegroundColor Gray
        Write-Host "  3. Check container logs: docker logs $DockerContainer" -ForegroundColor Gray
        Write-Host "  4. Verify backup file is valid SQL dump" -ForegroundColor Gray
    } else {
        Write-Host "  1. Check if PostgreSQL is running: pg_isready -h $Host -p $Port" -ForegroundColor Gray
        Write-Host "  2. Verify credentials in .pgpass or connection string" -ForegroundColor Gray
        Write-Host "  3. Ensure psql is in PATH: Get-Command psql" -ForegroundColor Gray
        Write-Host "  4. Verify backup file is valid SQL dump" -ForegroundColor Gray
    }
    
    # Cleanup temp file
    if ($TempFile -and (Test-Path $TempFile)) {
        Remove-Item $TempFile -Force -ErrorAction SilentlyContinue
    }
    
    exit 1
}
