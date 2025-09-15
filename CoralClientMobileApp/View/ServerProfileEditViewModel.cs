using CoralClientMobileApp.Model;
using CoralClientMobileApp.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoralClientMobileApp.View;

public partial class ServerProfileEditViewModel : BaseObservableViewModel
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string serverUri = string.Empty;

    [ObservableProperty]
    private string minecraftPort = "25565";

    [ObservableProperty]
    private string rconPort = "25575";

    [ObservableProperty]
    private string rconPassword = string.Empty;

    public void Initialize(ServerProfile? existingProfile = null)
    {
        if (existingProfile != null)
        {
            Name = existingProfile.Name;
            ServerUri = existingProfile.Uri;
            MinecraftPort = existingProfile.MinecraftPort.ToString();
            RconPort = existingProfile.RconPort.ToString();
            RconPassword = existingProfile.Password ?? string.Empty;
        }
        else
        {
            Name = string.Empty;
            ServerUri = string.Empty;
            MinecraftPort = "25565";
            RconPort = "25575";
            RconPassword = string.Empty;
        }
    }

    public ServerProfile? CreateServerProfile()
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ServerUri))
            return null;

        // Parse ports with validation
        if (!ushort.TryParse(MinecraftPort, out var minecraftPort))
            minecraftPort = 25565;

        if (!ushort.TryParse(RconPort, out var rconPort))
            rconPort = 25575;

        return new ServerProfile
        {
            Name = Name?.Trim() ?? string.Empty,
            Uri = ServerUri.Trim().ToLower(),
            MinecraftPort = minecraftPort,
            RconPort = rconPort,
            Password = string.IsNullOrWhiteSpace(RconPassword) ? null : RconPassword.Trim()
        };
    }
}