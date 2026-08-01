using Quartz;
using System.Diagnostics;

namespace TicketingSystem.Api.Jobs;

/// <summary>
/// Quartz.NET job that executes automated database backups.
/// Runs the PowerShell backup script at scheduled intervals.
/// </summary>
public class DatabaseBackupJob : IJob
{
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseBackupJob(ILogger<DatabaseBackupJob> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobStartTime = DateTime.UtcNow;
        _logger.LogInformation("Starting automated database backup job at {Time}", jobStartTime);

        try
        {
            // Get backup configuration
            var backupScriptPath = _configuration["Backup:ScriptPath"] ?? "../../../scripts/backup-database.ps1";
            var backupDir = _configuration["Backup:Directory"] ?? "../../../backups";
            var retentionDays = _configuration.GetValue<int>("Backup:RetentionDays", 30);
            var useDocker = _configuration.GetValue<bool>("Backup:UseDocker", true);
            var dockerContainer = _configuration["Backup:DockerContainer"] ?? "ticketing-pg";

            // Resolve script path relative to current directory
            var scriptFullPath = Path.GetFullPath(backupScriptPath);
            
            if (!File.Exists(scriptFullPath))
            {
                _logger.LogError("Backup script not found at path: {ScriptPath}", scriptFullPath);
                throw new FileNotFoundException($"Backup script not found: {scriptFullPath}");
            }

            _logger.LogInformation("Executing backup script: {ScriptPath}", scriptFullPath);
            _logger.LogInformation("Backup configuration: Directory={BackupDir}, Retention={RetentionDays} days, UseDocker={UseDocker}",
                backupDir, retentionDays, useDocker);

            // Prepare PowerShell process
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe", // PowerShell Core (cross-platform)
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptFullPath}\" -BackupDir \"{backupDir}\" -RetentionDays {retentionDays} -UseDocker ${useDocker} -DockerContainer \"{dockerContainer}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // If pwsh.exe not found, try powershell.exe (Windows PowerShell)
            if (!IsPowerShellAvailable("pwsh.exe"))
            {
                _logger.LogWarning("PowerShell Core (pwsh.exe) not found, falling back to Windows PowerShell (powershell.exe)");
                psi.FileName = "powershell.exe";
            }

            using var process = new Process { StartInfo = psi };
            
            // Capture output
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("Backup script output: {Output}", e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogWarning("Backup script error: {Error}", e.Data);
                }
            };

            // Start process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout (5 minutes)
            var timeout = TimeSpan.FromMinutes(5);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), context.CancellationToken);

            if (!completed)
            {
                process.Kill();
                throw new TimeoutException($"Backup script timed out after {timeout.TotalMinutes} minutes");
            }

            var exitCode = process.ExitCode;
            var duration = DateTime.UtcNow - jobStartTime;

            if (exitCode == 0)
            {
                _logger.LogInformation("Database backup completed successfully in {Duration:F2} seconds", duration.TotalSeconds);
                
                // Log summary from script output
                var output = outputBuilder.ToString();
                if (output.Contains("Backup file:"))
                {
                    var backupFileMatch = System.Text.RegularExpressions.Regex.Match(output, @"Backup file: (.+?)[\r\n]");
                    if (backupFileMatch.Success)
                    {
                        _logger.LogInformation("Backup file created: {BackupFile}", backupFileMatch.Groups[1].Value.Trim());
                    }
                }
            }
            else
            {
                var errorOutput = errorBuilder.ToString();
                _logger.LogError("Database backup failed with exit code {ExitCode}. Error: {Error}", exitCode, errorOutput);
                throw new Exception($"Backup script failed with exit code {exitCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automated database backup");
            
            // Re-throw to mark job as failed (Quartz will handle retries if configured)
            throw new JobExecutionException(ex);
        }
    }

    private static bool IsPowerShellAvailable(string executable)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "-Command \"Write-Host 'Test'\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            return process != null;
        }
        catch
        {
            return false;
        }
    }
}
