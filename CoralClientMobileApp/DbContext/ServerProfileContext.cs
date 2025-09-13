using System.IO;
using CoralClientMobileApp.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace CoralClientMobileApp.DbContext
{
    public class ServerProfileContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<ServerProfile> ServerProfiles { get; set; }

        public ServerProfileContext()
        {
            
        }

        public async Task InitializeDatabaseAsync()
        {
            await Database.EnsureCreatedAsync();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = GetDatabasePath();

            optionsBuilder
                .UseSqlite($"Filename={dbPath}")
                .EnableSensitiveDataLogging() // For debugging
                .LogTo(System.Console.WriteLine); // For debugging
        }

        private static string GetDatabasePath()
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
            System.Console.WriteLine($"Database path: {dbPath}");
            return dbPath;
        }
    }
}
