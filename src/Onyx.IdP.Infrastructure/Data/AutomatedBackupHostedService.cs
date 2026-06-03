using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Onyx.IdP.Infrastructure.Data.Services;

namespace Onyx.IdP.Infrastructure.Data
{
    internal class AutomatedBackupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<AutomatedBackupHostedService> _logger;
        private readonly IOptionsMonitor<BackupSettingsOptions> _optionsMonitor;
        private readonly string _dbName = "OnyxIdP";

        public AutomatedBackupHostedService(
            IServiceProvider serviceProvider,
            ILogger<AutomatedBackupHostedService> logger,
            IHostApplicationLifetime appLifetime,
            IOptionsMonitor<BackupSettingsOptions> optionsMonitor)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _appLifetime = appLifetime;
            _optionsMonitor = optionsMonitor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLifetime.ApplicationStopping.Register(() =>
            {
                var config = _optionsMonitor.CurrentValue;
                if (config.IsEnabled && !string.IsNullOrWhiteSpace(config.BackupPath))
                {
                    _logger.LogInformation("Application shutting down. Triggering final backup...");
                    RunBackupSynchronously(config);
                }
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                var config = _optionsMonitor.CurrentValue;

                if (config.IsEnabled && !string.IsNullOrWhiteSpace(config.BackupPath) && config.IntervalHours > 0)
                {
                    _logger.LogInformation("Running scheduled database backup...");
                    await RunBackupAsync(config, stoppingToken);

                    try
                    {
                        // Sleep for the configured interval (e.g., 4 hours)
                        await Task.Delay(TimeSpan.FromHours(config.IntervalHours), stoppingToken);
                    }
                    catch (TaskCanceledException) { /* App is shutting down */ }
                }
                else
                {
                    // If backups are disabled or path is missing, sleep for 5 minutes and check the config again
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (TaskCanceledException) { /* App is shutting down */ }
                }
            }
        }

        private async Task RunBackupAsync(BackupSettingsOptions config, CancellationToken cancellationToken)
        {
            // Create a scope to resolve the DbContext-dependent backup service
            using var scope = _serviceProvider.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

            bool success = await backupService.CreateBackupAsync(config.BackupPath, _dbName, cancellationToken);

            if (success)
            {
                CleanUpOldBackups(config.BackupPath, _dbName);
            }
        }

        private void RunBackupSynchronously(BackupSettingsOptions config)
        {
            // Fire-and-forget synchronous wrapper for the shutdown event
            RunBackupAsync(config, CancellationToken.None).GetAwaiter().GetResult();
        }

        private void CleanUpOldBackups(string directory, string dbPrefix)
        {
            var dirInfo = new DirectoryInfo(directory);
            if (!dirInfo.Exists) return;

            // Keep the last 7 days of backups to prevent filling up the hard drive
            var oldFiles = dirInfo.GetFiles($"{dbPrefix}_*.bak")
                .Where(f => f.CreationTime < DateTime.Now.AddDays(-7));

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup file: {FileName}", file.Name);
                }
            }
        }
    }

    public class BackupSettingsOptions
    {
        public bool IsEnabled { get; set; } = false;
        public string BackupPath { get; set; } = string.Empty;
        public int IntervalHours { get; set; } = 4;
    }
}
