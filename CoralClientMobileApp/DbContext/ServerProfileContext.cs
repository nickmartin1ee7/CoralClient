using System;
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
        public DbSet<CustomCommand> CustomCommands { get; set; }

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
                    await ServerProfiles.AddRangeAsync(
                    [
                        new ServerProfile { Uri = "192.168.2.200", MinecraftPort = 25565, RconPort = 25575, Password = "AStrongPassword" },
                        new ServerProfile { Uri = "sample1.example.com", MinecraftPort = 25565, RconPort = 25575, Password = "pass1" },
                    ]);

                    await SaveChangesAsync();
                }
    #endif

                // Seed default custom commands if none exist
                if (!CustomCommands.Any())
                {
                    await SeedDefaultCommandsAsync();
                }

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
                .LogTo(msg => _logger?.LogDebug(msg)); // For debugging
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

        private async Task SeedDefaultCommandsAsync()
        {
            _logger?.LogInformation("Seeding default custom commands");

            // Get all server profiles to associate commands with them
            var serverProfiles = await ServerProfiles.ToListAsync();

            var defaultCommands = new List<CustomCommand>();

            foreach (var serverProfile in serverProfiles)
            {
                // Default Player Commands
                defaultCommands.AddRange(new[]
                {
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Kick",
                        Description = "Kick player from server",
                        Command = "kick {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ban",
                        Description = "Ban player from server",
                        Command = "ban {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Op",
                        Description = "Give player operator privileges",
                        Command = "op {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Creative",
                        Description = "Set player to creative mode",
                        Command = "gamemode creative {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Survival",
                        Description = "Set player to survival mode",
                        Command = "gamemode survival {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Spectator",
                        Description = "Set player to spectator mode",
                        Command = "gamemode spectator {player}",
                        Category = "Default",
                        Target = CommandTarget.Player,
                        RequiresPlayerName = true,
                        ServerProfileId = serverProfile.Id
                    }
                });

                // Default Server Commands
                defaultCommands.AddRange(new[]
                {
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Stop Server",
                        Description = "Stop the Minecraft server",
                        Command = "stop",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Save World",
                        Description = "Save the current world",
                        Command = "save-all",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Reload",
                        Description = "Reload server configuration",
                        Command = "reload",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Clear Weather",
                        Description = "Set weather to clear",
                        Command = "weather clear",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Rain",
                        Description = "Set weather to rain",
                        Command = "weather rain",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Thunder",
                        Description = "Set weather to thunder",
                        Command = "weather thunder",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Day",
                        Description = "Set time to day",
                        Command = "time set day",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Night",
                        Description = "Set time to night",
                        Command = "time set night",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    },
                    new CustomCommand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Noon",
                        Description = "Set time to noon",
                        Command = "time set noon",
                        Category = "Default",
                        Target = CommandTarget.Server,
                        RequiresPlayerName = false,
                        ServerProfileId = serverProfile.Id
                    }
                });
            }

            if (defaultCommands.Any())
            {
                await CustomCommands.AddRangeAsync(defaultCommands);
                await SaveChangesAsync();
                _logger?.LogInformation("Seeded {Count} default custom commands", defaultCommands.Count);
            }
        }
    }
}
