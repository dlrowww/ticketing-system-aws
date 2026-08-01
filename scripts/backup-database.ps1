<#
.SYNOPSIS
    PostgreSQL Database Backup Script for Ticketing System

.DESCRIPTION
    Creates a compressed backup of the PostgreSQL database using pg_dump.
    Supports both Docker and native PostgreSQL installations.
    Implements 30-day retention policy with automatic cleanup.

.PARAMETER BackupDir
    Directory where backups will be stored. Default: ../backups

.PARAMETER DatabaseName
    Name of the database to backup. Default: ticketing

.PARAMETER RetentionDays
    Number of days to retain backups. Default: 30

.PARAMETER DockerContainer
    Name of the Docker container running PostgreSQL. If specified, uses docker exec.
    Default: ticketing-pg

.PARAMETER UseDocker
    Switch to use Docker container for backup. Default: $true

.EXAMPLE
    .\backup-database.ps1
    Creates a backup using default settings (Docker container)

.EXAMPLE
    .\backup-database.ps1 -UseDocker $false -Host localhost -Port 5432 -Username postgres
    Creates a backup from native PostgreSQL installation

.NOTES
    Version: 1.0
    Author: Ticketing System
    Date: January 3, 2026
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupDir = "../backups",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "ticketing",
    
    [Parameter(Mandatory=$false)]
    [int]$RetentionDays = 30,
    
    [Parameter(Mandatory=$false)]
    [string]$DockerContainer = "ticketing-pg",
    
    [Parameter(Mandatory=$false)]
    [bool]$UseDocker = $true,
    
    [Parameter(Mandatory=$false)]
    [string]$Host = "localhost",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 5432,
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "postgres"
)

# Error handling
$ErrorActionPreference = "Stop"

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Resolve backup directory
$BackupPath = Join-Path $ScriptDir $BackupDir
if (-not (Test-Path $BackupPath)) {
    Write-Host "Creating backup directory: $BackupPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
}

# Generate backup filename with timestamp
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = "ticketing_backup_$Timestamp.sql"
$BackupFilePath = Join-Path $BackupPath $BackupFile
$CompressedFile = "$BackupFilePath.gz"

Write-Host "=== PostgreSQL Backup Script ===" -ForegroundColor Cyan
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Backup file: $BackupFile" -ForegroundColor White
Write-Host "Retention: $RetentionDays days" -ForegroundColor White
Write-Host ""

try {
    # Create backup using pg_dump
    if ($UseDocker) {
        Write-Host "Creating backup from Docker container: $DockerContainer" -ForegroundColor Yellow
        
        # Check if container is running
        $containerStatus = docker ps --filter "name=$DockerContainer" --format "{{.Names}}"
        if ($containerStatus -ne $DockerContainer) {
            throw "Docker container '$DockerContainer' is not running. Start it with: docker start $DockerContainer"
        }
        
        # Execute pg_dump inside Docker container
        Write-Host "Running pg_dump..." -ForegroundColor Gray
        docker exec $DockerContainer pg_dump -U postgres -d $DatabaseName --verbose --no-owner --no-acl | Out-File -FilePath $BackupFilePath -Encoding UTF8
        
    } else {
        Write-Host "Creating backup from native PostgreSQL installation" -ForegroundColor Yellow
        Write-Host "Host: ${Host}:${Port}" -ForegroundColor Gray
        
        # Check if pg_dump is available
        $pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue
        if (-not $pgDump) {
            throw "pg_dump not found in PATH. Please install PostgreSQL client tools or add to PATH."
        }
        
        # Set PGPASSWORD environment variable (for non-interactive backup)
        Write-Host "Note: You may be prompted for password. Consider using .pgpass file for automation." -ForegroundColor Yellow
        
        # Execute pg_dump
        Write-Host "Running pg_dump..." -ForegroundColor Gray
        pg_dump -h $Host -p $Port -U $Username -d $DatabaseName --verbose --no-owner --no-acl | Out-File -FilePath $BackupFilePath -Encoding UTF8
    }
    
    # Check if backup was created
    if (-not (Test-Path $BackupFilePath)) {
        throw "Backup file was not created: $BackupFilePath"
    }
    
    $BackupSize = (Get-Item $BackupFilePath).Length / 1MB
    Write-Host "Backup created successfully: $([math]::Round($BackupSize, 2)) MB" -ForegroundColor Green
    
    # Compress backup using gzip (if available)
    Write-Host "Compressing backup..." -ForegroundColor Yellow
    
    # Check if gzip is available (from Git for Windows or WSL)
    $gzip = Get-Command gzip -ErrorAction SilentlyContinue
    if ($gzip) {
        gzip -f $BackupFilePath
        
        if (Test-Path $CompressedFile) {
            $CompressedSize = (Get-Item $CompressedFile).Length / 1MB
            $CompressionRatio = [math]::Round((1 - ($CompressedSize / $BackupSize)) * 100, 1)
            Write-Host "Compressed to: $([math]::Round($CompressedSize, 2)) MB (saved $CompressionRatio%)" -ForegroundColor Green
        }
    } else {
        Write-Host "gzip not found. Backup will not be compressed." -ForegroundColor Yellow
        Write-Host "Install gzip via Git for Windows or WSL for compression." -ForegroundColor Gray
    }
    
    # Cleanup old backups (retention policy)
    Write-Host ""
    Write-Host "Applying retention policy: $RetentionDays days" -ForegroundColor Yellow
    
    $CutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $OldBackups = Get-ChildItem -Path $BackupPath -Filter "ticketing_backup_*.sql*" | Where-Object { $_.LastWriteTime -lt $CutoffDate }
    
    if ($OldBackups.Count -gt 0) {
        Write-Host "Found $($OldBackups.Count) old backup(s) to delete:" -ForegroundColor Gray
        foreach ($oldBackup in $OldBackups) {
            Write-Host "  - $($oldBackup.Name) ($(Get-Date $oldBackup.LastWriteTime -Format 'yyyy-MM-dd HH:mm'))" -ForegroundColor Gray
            Remove-Item $oldBackup.FullName -Force
        }
        Write-Host "Old backups deleted successfully" -ForegroundColor Green
    } else {
        Write-Host "No old backups to delete" -ForegroundColor Gray
    }
    
    # Summary
    Write-Host ""
    Write-Host "=== Backup Summary ===" -ForegroundColor Cyan
    Write-Host "Status: SUCCESS" -ForegroundColor Green
    Write-Host "Backup file: $BackupFile$(if (Test-Path $CompressedFile) {'.gz'})" -ForegroundColor White
    Write-Host "Location: $BackupPath" -ForegroundColor White
    Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
    
    # List all current backups
    $AllBackups = Get-ChildItem -Path $BackupPath -Filter "ticketing_backup_*.sql*" | Sort-Object LastWriteTime -Descending
    Write-Host ""
    Write-Host "Current backups ($($AllBackups.Count)):" -ForegroundColor Cyan
    foreach ($backup in $AllBackups) {
        $backupAge = ((Get-Date) - $backup.LastWriteTime).Days
        $backupSizeMB = [math]::Round($backup.Length / 1MB, 2)
        Write-Host "  - $($backup.Name) - $backupSizeMB MB - $backupAge days old" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Backup completed successfully!" -ForegroundColor Green
    exit 0
    
} catch {
    Write-Host ""
    Write-Host "=== Backup Failed ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    if ($UseDocker) {
        Write-Host "  1. Check if Docker container is running: docker ps" -ForegroundColor Gray
        Write-Host "  2. Start container: docker start $DockerContainer" -ForegroundColor Gray
        Write-Host "  3. Check container logs: docker logs $DockerContainer" -ForegroundColor Gray
    } else {
        Write-Host "  1. Check if PostgreSQL is running: pg_isready -h $Host -p $Port" -ForegroundColor Gray
        Write-Host "  2. Verify credentials in .pgpass or connection string" -ForegroundColor Gray
        Write-Host "  3. Ensure pg_dump is in PATH: Get-Command pg_dump" -ForegroundColor Gray
    }
    
    exit 1
}
