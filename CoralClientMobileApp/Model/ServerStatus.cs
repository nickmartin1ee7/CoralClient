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
    }
}