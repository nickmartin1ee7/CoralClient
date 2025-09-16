namespace CoralClientMobileApp.Model
{
    public class ServerStatus
    {
        public bool IsOnline { get; set; }
        
        public int OnlinePlayers { get; set; }
        
        public int MaxPlayers { get; set; }
        
        public string VersionName { get; set; } = string.Empty;
        
        public int Ping { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public string Motd { get; set; } = string.Empty;
        
        public string StrippedMotd { get; set; } = string.Empty;
        
        public string GameType { get; set; } = string.Empty;
        
        public string HostIp { get; set; } = string.Empty;
        
        public int HostPort { get; set; }
        
        public string[]? PlayerList { get; set; }
        
        public string? Favicon { get; set; }
        
        public byte[]? FaviconBytes { get; set; }
        
        // Legacy properties kept for backward compatibility (always null with MineStat)
        public string Map { get; set; } = string.Empty;
        
        public string? GameId { get; set; }
        
        public string? Plugins { get; set; }
    }
}