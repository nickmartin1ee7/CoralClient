namespace MinecraftQuery.Models
{
    public class ServerStatus
    {
        public bool IsOnline { get; set; }
        public int Ping { get; set; }
        public int OnlinePlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Motd { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> PlayerList { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }
}