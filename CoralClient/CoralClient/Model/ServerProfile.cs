namespace CoralClient.Model
{
    public class ServerProfile
    {
        public string ServerUriText => $"{Uri}:{MinecraftPort}";
        public string Uri { get; set; }
        public ushort MinecraftPort { get; set; }
        public ushort RconPort { get; set; }
        public string Password { get; set; }
    }
}