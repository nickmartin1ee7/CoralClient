using System.IO;
using CoralClient.Model;
using Microsoft.EntityFrameworkCore;
using Xamarin.Essentials;

namespace CoralClient.DbContext
{
    public class ServerProfileContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<ServerProfile> ServerProfiles { get; set; }

        public ServerProfileContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "storage.db");

            optionsBuilder
                .UseSqlite($"Filename={dbPath}");
        }
    }
}
