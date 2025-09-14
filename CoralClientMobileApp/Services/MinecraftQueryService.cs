using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using CoralClientMobileApp.Model;
using McQuery.Net;

namespace CoralClientMobileApp.Services
{
    public class MinecraftQueryService : IDisposable
    {
        private readonly IMcQueryClient _mcQueryClient;
        
        public MinecraftQueryService(IMcQueryClient mcQueryClient)
        {
            _mcQueryClient = mcQueryClient ?? throw new ArgumentNullException(nameof(mcQueryClient));
        }

        public async Task<Model.ServerStatus> QueryServerAsync(string address, int port)
        {
            try
            {
                // Create endpoint
                var resolvedAddress = await ResolveHostnameAsync(address);
                var endpoint = new IPEndPoint(IPAddress.Parse(resolvedAddress), port);
                
                // Measure ping and get basic status in parallel
                var pingTask = PingServerAsync(address, port);
                var statusTask = _mcQueryClient.GetBasicStatusAsync(endpoint);
                
                await Task.WhenAll(pingTask, statusTask);
                
                var ping = await pingTask;
                var basicStatusResponse = await statusTask;
                
                return new Model.ServerStatus
                {
                    IsOnline = true,
                    OnlinePlayers = basicStatusResponse.NumPlayers,
                    MaxPlayers = basicStatusResponse.MaxPlayers,
                    VersionName = basicStatusResponse.GameType, // McQuery.Net doesn't provide version in basic status
                    Ping = ping > 0 ? ping : 0,
                    Motd = basicStatusResponse.Motd,
                    GameType = basicStatusResponse.GameType,
                    Map = basicStatusResponse.Map,
                    HostIp = basicStatusResponse.HostIp,
                    HostPort = basicStatusResponse.HostPort
                };
            }
            catch (Exception ex)
            {
                return new Model.ServerStatus
                {
                    IsOnline = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<Model.ServerStatus> QueryServerFullAsync(string address, int port)
        {
            try
            {
                // Create endpoint
                var resolvedAddress = await ResolveHostnameAsync(address);
                var endpoint = new IPEndPoint(IPAddress.Parse(resolvedAddress), port);
                
                // Measure ping and get full status in parallel
                var pingTask = PingServerAsync(address, port);
                var statusTask = _mcQueryClient.GetFullStatusAsync(endpoint);
                
                await Task.WhenAll(pingTask, statusTask);
                
                var ping = await pingTask;
                var fullStatusResponse = await statusTask;
                
                return new Model.ServerStatus
                {
                    IsOnline = true,
                    OnlinePlayers = fullStatusResponse.NumPlayers,
                    MaxPlayers = fullStatusResponse.MaxPlayers,
                    VersionName = fullStatusResponse.Version,
                    Ping = ping > 0 ? ping : 0,
                    Motd = fullStatusResponse.Motd,
                    GameType = fullStatusResponse.GameType,
                    Map = fullStatusResponse.Map,
                    HostIp = fullStatusResponse.HostIp,
                    HostPort = fullStatusResponse.HostPort,
                    GameId = fullStatusResponse.GameId,
                    Plugins = fullStatusResponse.Plugins,
                    PlayerList = fullStatusResponse.PlayerList
                };
            }
            catch (Exception ex)
            {
                // Fallback to basic status if full status fails
                try
                {
                    return await QueryServerAsync(address, port);
                }
                catch
                {
                    return new Model.ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        private async Task<string> ResolveHostnameAsync(string hostname)
        {
            try
            {
                // If it's already an IP address, return as is
                if (IPAddress.TryParse(hostname, out _))
                {
                    return hostname;
                }
                
                // Resolve hostname to IP address
                var hostEntry = await Dns.GetHostEntryAsync(hostname);
                return hostEntry.AddressList.First(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            }
            catch
            {
                // If resolution fails, try to parse as IP anyway
                return hostname;
            }
        }

        private async Task<int> PingServerAsync(string address, int port)
        {
            try
            {
                var resolvedAddress = await ResolveHostnameAsync(address);
                var endpoint = new IPEndPoint(IPAddress.Parse(resolvedAddress), port);
                
                var stopwatch = Stopwatch.StartNew();
                
                using var tcpClient = new TcpClient();
                
                // Set a reasonable timeout for the connection
                var connectTask = tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Connection timed out
                    return -1;
                }
                
                if (connectTask.IsFaulted)
                {
                    // Connection failed
                    return -1;
                }
                
                stopwatch.Stop();
                return (int)stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return -1;
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
            return await PingServerAsync(address, port);
        }

        public void Dispose()
        {
            try
            {
                _mcQueryClient?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}