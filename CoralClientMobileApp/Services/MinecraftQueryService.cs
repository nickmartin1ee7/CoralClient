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
                // Fallback to TCP check if query fails
                return await TcpFallbackCheckAsync(address, port, ex.Message);
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
            catch (SocketException se) when (se.SocketErrorCode == SocketError.HostNotFound)

            {
                // For DNS resolution failures - do not fallback to basic status
                return new Model.ServerStatus
                {
                    IsOnline = false,
                    ErrorMessage = se.Message
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
                    // If both full and basic query fail, fallback to TCP check
                    return await TcpFallbackCheckAsync(address, port, ex.Message);
                }
            }
        }

        private async Task<Model.ServerStatus> TcpFallbackCheckAsync(string address, int port, string originalError)
        {
            try
            {
                // Try to connect via TCP to determine if server is at least reachable
                var resolvedAddress = await ResolveHostnameAsync(address);
                var endpoint = new IPEndPoint(IPAddress.Parse(resolvedAddress), port);
                
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Connection timed out - server is offline
                    return new Model.ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = $"Query failed: {originalError}. TCP connection timed out."
                    };
                }
                
                if (connectTask.IsFaulted)
                {
                    // Connection failed - server is offline
                    return new Model.ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = $"Query failed: {originalError}. TCP connection failed: {connectTask.Exception?.GetBaseException().Message}"
                    };
                }
                
                // TCP connection succeeded - server is online but query protocol failed
                var ping = await PingServerAsync(address, port);
                return new Model.ServerStatus
                {
                    IsOnline = true,
                    OnlinePlayers = 0,
                    MaxPlayers = 0,
                    VersionName = "Unknown (Query Failed)",
                    Ping = ping > 0 ? ping : 0,
                    Motd = "Query protocol unavailable",
                    GameType = "Unknown",
                    Map = "Unknown",
                    HostIp = resolvedAddress,
                    HostPort = port,
                    ErrorMessage = $"Server online but query failed: {originalError}"
                };
            }
            catch (Exception tcpEx)
            {
                // Even TCP check failed - server is definitely offline
                return new Model.ServerStatus
                {
                    IsOnline = false,
                    ErrorMessage = $"Query failed: {originalError}. TCP check also failed: {tcpEx.Message}"
                };
            }
        }

        private async Task<string> ResolveHostnameAsync(string hostname)
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
            catch (Exception)
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