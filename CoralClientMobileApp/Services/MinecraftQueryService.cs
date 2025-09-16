using System.Net.Sockets;
using MineStatLib;

namespace CoralClientMobileApp.Services
{
    public class MinecraftQueryService
    {
        private const int DefaultTimeout = 5;
        
        public MinecraftQueryService()
        {
        }

        public async Task<Model.ServerStatus> QueryServerAsync(string address, int port)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var mineStat = new MineStat(address, (ushort)port, DefaultTimeout);
                    
                    return new Model.ServerStatus
                    {
                        IsOnline = mineStat.ServerUp,
                        OnlinePlayers = mineStat.CurrentPlayersInt,
                        MaxPlayers = mineStat.MaximumPlayersInt,
                        VersionName = mineStat.Version ?? string.Empty,
                        Ping = (int)mineStat.Latency,
                        Motd = mineStat.Motd ?? string.Empty,
                        StrippedMotd = mineStat.Stripped_Motd ?? string.Empty,
                        GameType = mineStat.Gamemode ?? string.Empty,
                        HostIp = address,
                        HostPort = port,
                        PlayerList = mineStat.PlayerList,
                        Favicon = mineStat.Favicon,
                        FaviconBytes = mineStat.FaviconBytes,
                        ErrorMessage = mineStat.ServerUp ? null : "Server is offline"
                    };
                }
                catch (Exception ex)
                {
                    return await TcpFallbackCheckAsync(address, port, ex.Message);
                }
            });
        }

        public async Task<Model.ServerStatus> QueryServerFullAsync(string address, int port)
        {
            return await QueryServerAsync(address, port);
        }

        private async Task<Model.ServerStatus> TcpFallbackCheckAsync(string address, int port, string originalError)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    return new Model.ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = "Connection timed out"
                    };
                }
                
                if (connectTask.IsFaulted)
                {
                    return new Model.ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = connectTask.Exception?.GetBaseException().Message ?? originalError
                    };
                }
                
                return new Model.ServerStatus
                {
                    IsOnline = true,
                    OnlinePlayers = 0,
                    MaxPlayers = 0,
                    VersionName = "Unknown",
                    Ping = 0,
                    Motd = "Server online (query failed)",
                    StrippedMotd = "Server online (query failed)",
                    GameType = "Unknown",
                    HostIp = address,
                    HostPort = port,
                    ErrorMessage = $"Server reachable but query failed: {originalError}"
                };
            }
            catch (Exception tcpEx)
            {
                return new Model.ServerStatus
                {
                    IsOnline = false,
                    ErrorMessage = tcpEx.Message
                };
            }
        }

        /// <summary>
        /// Measures the ping latency to a Minecraft server
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="port">Server port</param>
        /// <returns>Ping in milliseconds, or -1 if unreachable</returns>
        public async Task<int> PingAsync(string address, int port)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var mineStat = new MineStat(address, (ushort)port, DefaultTimeout);
                    return mineStat.ServerUp ? (int)mineStat.Latency : -1;
                }
                catch
                {
                    return -1;
                }
            });
        }
    }
}