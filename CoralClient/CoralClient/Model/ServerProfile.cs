using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoralClient.Model
{
    public class ServerProfile
    {
        [Key]
        public Guid Id { get; set; }

        public string Uri { get; set; }

        public ushort MinecraftPort { get; set; }

        public ushort RconPort { get; set; }

        public string Password { get; set; }

        [NotMapped]
        public string ServerUriText => $"{Uri}:{MinecraftPort}";
    }
}