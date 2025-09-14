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
        
        public string GameType { get; set; } = string.Empty;
        
        public string Map { get; set; } = string.Empty;
        
        public string HostIp { get; set; } = string.Empty;
        
        public int HostPort { get; set; }
        
        // Full status properties (optional, might be null for basic status)
        public string? GameId { get; set; }
        
        public string? Plugins { get; set; }
        
        public string[]? PlayerList { get; set; }
    }
}