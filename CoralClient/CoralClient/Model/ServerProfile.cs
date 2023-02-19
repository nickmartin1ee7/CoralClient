using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;

using Xamarin.Forms;

namespace CoralClient.Model
{
    public class ServerProfile
    {
        [Key]
        public Guid Id { get; set; }

        public string Uri { get; set; }

        public ushort MinecraftPort { get; set; } = 25565;

        public ushort RconPort { get; set; }

        public string Password { get; set; }

        public string ImageB64 { get; set; }

        [NotMapped]
        public ImageSource Image
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(ImageB64))
                    {
                        return ImageSource.FromFile("default_server.png");
                    }

                    var imageBytes = Convert.FromBase64String(ImageB64);
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }


        [NotMapped]
        public string ServerUriText => $"{Uri}:{MinecraftPort}";
    }
}