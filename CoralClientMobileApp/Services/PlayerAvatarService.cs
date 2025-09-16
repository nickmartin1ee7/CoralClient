using Microsoft.Extensions.Logging;

namespace CoralClientMobileApp.Services
{
    public interface IPlayerAvatarService
    {
        string GetPlayerAvatarUrl(string playerName, int size = 100);
        string GetPlayerAvatarUrlByUuid(string uuid, int size = 100);
        Task LoadPlayerAvatarAsync(Model.Player player, int size = 100);
    }

    public class PlayerAvatarService : IPlayerAvatarService
    {
        private readonly ILogger<PlayerAvatarService> _logger;
        private const string MC_HEADS_BASE_URL = "https://mc-heads.net";

        public PlayerAvatarService(ILogger<PlayerAvatarService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetPlayerAvatarUrl(string playerName, int size = 100)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return string.Empty;

            return $"{MC_HEADS_BASE_URL}/avatar/{playerName}/{size}/nohelm.png";
        }

        public string GetPlayerAvatarUrlByUuid(string uuid, int size = 100)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return string.Empty;

            // Remove dashes from UUID if present
            var cleanUuid = uuid.Replace("-", "");
            return $"{MC_HEADS_BASE_URL}/avatar/{cleanUuid}/{size}/nohelm.png";
        }

        public Task LoadPlayerAvatarAsync(Model.Player player, int size = 100)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Name))
                return Task.CompletedTask;

            try
            {
                player.IsLoadingAvatar = true;
                
                // Prefer UUID-based avatar if available, fallback to name-based
                if (!string.IsNullOrWhiteSpace(player.Uuid))
                {
                    player.AvatarUrl = GetPlayerAvatarUrlByUuid(player.Uuid, size);
                }
                else
                {
                    player.AvatarUrl = GetPlayerAvatarUrl(player.Name, size);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading avatar for player {PlayerName}", player.Name);
            }
            finally
            {
                player.IsLoadingAvatar = false;
            }

            return Task.CompletedTask;
        }
    }
}