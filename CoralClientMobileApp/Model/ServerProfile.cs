using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoralClientMobileApp.Model
{
    public class ServerProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Uri { get; set; } = string.Empty;

        public ushort MinecraftPort { get; set; } = 25565;

        public ushort RconPort { get; set; } = 25575;

        public string Password { get; set; } = string.Empty;

        [NotMapped]
        public string ServerUriText => $"{Uri}:{MinecraftPort}";
    }
}