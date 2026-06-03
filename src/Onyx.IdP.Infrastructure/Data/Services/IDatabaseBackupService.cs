using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Onyx.IdP.Infrastructure.Data.Services
{
    public interface IDatabaseBackupService
    {
        Task<bool> CreateBackupAsync(string targetDirectory, string databaseName, CancellationToken cancellationToken = default);
    }

    internal class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(ApplicationDbContext context, ILogger<DatabaseBackupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CreateBackupAsync(string targetDirectory, string databaseName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{databaseName}_{timestamp}.bak";
                string fullPath = Path.Combine(targetDirectory, fileName);

                string sql = $@"BACKUP DATABASE [{databaseName}] TO DISK = '{fullPath}' WITH FORMAT, MEDIANAME = 'OnyxBackups', NAME = 'Full Backup of {databaseName}';";

                await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

                _logger.LogInformation("Successfully backed up database {DatabaseName} to {Path}", databaseName, fullPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to back up database {DatabaseName}", databaseName);
                return false;
            }
        }
    }
}
