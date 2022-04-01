namespace CoralClient.Model
{
    public class ServerProfile
    {
        public string ServerUriText => $"{Uri}:{MinecraftPort}";
        public string ConnectionStatusText { get; set; } = "Disconnected";
        public string Uri { get; set; }
        public int MinecraftPort { get; set; }
        public int RconPort { get; set; }
    }
}