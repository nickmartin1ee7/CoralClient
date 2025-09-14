using System.IO;
using CoralClientMobileApp.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.DbContext
{
    public class ServerProfileContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly ILogger<ServerProfileContext>? _logger;
        
        public DbSet<ServerProfile> ServerProfiles { get; set; }

        public ServerProfileContext()
        {
            
        }

        public ServerProfileContext(ILogger<ServerProfileContext> logger)
        {
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
            await Database.EnsureCreatedAsync();

    #if DEBUG
            if (!ServerProfiles.Any())
            {
                ServerProfiles.AddRange(
                [
                    new ServerProfile { Uri = "sample1.example.com", MinecraftPort = 25565, RconPort = 25575, Password = "pass1" },
                    new ServerProfile { Uri = "sample2.example.com", MinecraftPort = 25566, RconPort = 25576, Password = "pass2" },
                    new ServerProfile { Uri = "sample3.example.com", MinecraftPort = 25567, RconPort = 25577, Password = "pass3" },
                    new ServerProfile { Uri = "sample4.example.com", MinecraftPort = 25568, RconPort = 25578, Password = "pass4" },
                    new ServerProfile { Uri = "sample5.example.com", MinecraftPort = 25569, RconPort = 25579, Password = "pass5" }
                ]);

                await SaveChangesAsync();
            }
    #endif

            _logger?.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
            _logger?.LogError(ex, "Failed to initialize database");
            throw;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = GetDatabasePath();
            _logger?.LogInformation("Configuring database with path: {DbPath}", dbPath);

            optionsBuilder
                .UseSqlite($"Filename={dbPath}")
                .EnableSensitiveDataLogging() // For debugging
                .LogTo(msg => _logger.LogDebug(msg)); // For debugging
        }

        private string GetDatabasePath()
        {
            // Use Environment.GetFolderPath for cross-platform compatibility
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            // Fallback to current directory if LocalApplicationData is not available
            if (string.IsNullOrEmpty(appDataPath))
            {
                appDataPath = Directory.GetCurrentDirectory();
            }
            
            // Ensure the directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            var dbPath = Path.Combine(appDataPath, "storage.db");
            _logger?.LogInformation("Database path resolved to: {DbPath}", dbPath);
            return dbPath;
        }
    }
}
