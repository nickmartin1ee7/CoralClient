using MCQuery;
using CoralClientMobileApp.Model;

namespace CoralClientMobileApp.Services
{
    public class MinecraftQueryService
    {
        public async Task<Model.ServerStatus> QueryServerAsync(string address, int port)
        {
            try
            {
                var server = new MCServer(address, port);
                
                // Run status and ping queries in parallel
                var statusTask = Task.Run(() => server.Status());
                var pingTask = Task.Run(() => server.Ping());
                
                await Task.WhenAll(statusTask, pingTask);
                
                var status = await statusTask;
                var ping = await pingTask;
                
                return new Model.ServerStatus
                {
                    IsOnline = true,
                    OnlinePlayers = (int)status.Players.Online,
                    MaxPlayers = (int)status.Players.Max,
                    VersionName = status.Version.Name ?? string.Empty,
                    Ping = (int)Math.Round(ping)
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
    }
}