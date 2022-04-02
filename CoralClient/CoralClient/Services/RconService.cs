using System;
using System.Net;
using System.Threading.Tasks;
using CoreRCON;

namespace CoralClient.Services
{
    public class RconService : IDisposable
    {
        private RCON _rcon;

        public event EventHandler OnConnected;

        public event EventHandler OnDisconnected; 
        
        public async Task ConnectAsync(IPAddress host, ushort port, string password)
        {
            _rcon = new RCON(host, port, password);
            _rcon.OnDisconnected += () => OnDisconnected?.Invoke(this, EventArgs.Empty);

            await _rcon.ConnectAsync();

            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public async Task<string> SendCommandAsync(string command)
        {
            return await _rcon.SendCommandAsync(command);
        }

        public void Dispose()
        {
            _rcon?.Dispose();
            _rcon = null;
        }
    }
}
