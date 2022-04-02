using System;
using System.Net;
using System.Threading.Tasks;
using CoreRCON;
using CoreRCON.PacketFormats;
using CoreRCON.Parsers;

namespace CoralClient.Services
{
    public class RconService : IDisposable
    {
        private RCON _rcon;
        private IPAddress _host;

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected; 

        public async Task ConnectAsync(IPAddress host, ushort port, string password)
        {
            _host = host;
            _rcon = new RCON(_host, port, password);
            _rcon.OnDisconnected += () => OnDisconnected?.Invoke(this, EventArgs.Empty);
            
            await _rcon.ConnectAsync();

            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public async Task<string> SendCommandAsync(string command) => 
            await _rcon.SendCommandAsync(command);

        // TODO: Doesn't work
        public async Task<MinecraftQueryInfo> GetStatusAsync(ushort port) =>
            await ServerQuery.Info(_host, port, ServerQuery.ServerType.Minecraft) as MinecraftQueryInfo;

        public void Dispose()
        {
            _rcon?.Dispose();
            _rcon = null;
        }
    }
}
