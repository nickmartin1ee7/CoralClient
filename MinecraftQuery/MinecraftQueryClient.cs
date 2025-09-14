using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using MinecraftQuery.Models;

namespace MinecraftQuery
{
    public class MinecraftQueryClient
    {
        private readonly HttpClient _httpClient;

        public MinecraftQueryClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<ServerStatus> QueryServerAsync(string hostname, int port = 25565)
        {
            var serverStatus = new ServerStatus();

            try
            {
                // First, try to ping the server to check basic connectivity
                var ping = await PingServerAsync(hostname);
                serverStatus.Ping = ping;

                if (ping < 0)
                {
                    serverStatus.IsOnline = false;
                    serverStatus.ErrorMessage = "Server is not reachable";
                    return serverStatus;
                }

                // Try to get server status using Minecraft's server list ping protocol
                serverStatus = await GetServerStatusAsync(hostname, port);
                serverStatus.Ping = ping;
            }
            catch (Exception ex)
            {
                serverStatus.IsOnline = false;
                serverStatus.ErrorMessage = ex.Message;
            }

            return serverStatus;
        }

        private async Task<int> PingServerAsync(string hostname)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(hostname, 5000);
                
                return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;
            }
            catch
            {
                return -1;
            }
        }

        private async Task<ServerStatus> GetServerStatusAsync(string hostname, int port)
        {
            var serverStatus = new ServerStatus();

            try
            {
                // This is a simplified implementation
                // In a real implementation, you would use the Minecraft Server List Ping protocol
                // For now, we'll simulate the data or use a third-party API

                // Try to connect to see if the server is responding
                using var tcpClient = new System.Net.Sockets.TcpClient();
                var connectTask = tcpClient.ConnectAsync(hostname, port);
                var timeoutTask = Task.Delay(5000);

                if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && tcpClient.Connected)
                {
                    serverStatus.IsOnline = true;
                    
                    // Simulate server data (in a real implementation, you'd parse the actual server response)
                    serverStatus.OnlinePlayers = Random.Shared.Next(0, 20);
                    serverStatus.MaxPlayers = 20;
                    serverStatus.Motd = "A Minecraft Server";
                    serverStatus.Version = "1.20.1";
                    
                    // Generate some fake player names for demo
                    var playerNames = new[] { "Player1", "Player2", "BuilderBob", "CraftMaster", "RedstoneGuru" };
                    serverStatus.PlayerList = playerNames.Take(serverStatus.OnlinePlayers).ToList();
                }
                else
                {
                    serverStatus.IsOnline = false;
                    serverStatus.ErrorMessage = "Could not connect to server";
                }
            }
            catch (Exception ex)
            {
                serverStatus.IsOnline = false;
                serverStatus.ErrorMessage = ex.Message;
            }

            return serverStatus;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}