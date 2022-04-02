using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftRcon
{
    public class RconClient : IDisposable
    {
        private const int MAX_MESSAGE_SIZE = 4110; // 4096 + 14 bytes of header data.

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private int _lastId;

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        
        public void Dispose()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }

        public async Task ConnectAsync(string host, int port)
        {
            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(host, port);
            _networkStream = _tcpClient.GetStream();

            Connected?.Invoke(this, EventArgs.Empty);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _networkStream.FlushAsync();
                await _networkStream.DisposeAsync();
                _tcpClient.Close();
            }
            catch
            {
                // Cleanup process should not throw
            }
            finally
            {
                Dispose();
            }
        }

        public Task<(bool IsValid, Message Response)> AuthenticateAsync(string password)
        {
            return SendMessageAsync(new Message(
                password.Length + Encoder.HEADER_LENGTH,
                Interlocked.Increment(ref _lastId),
                MessageType.Authenticate,
                password));
        }

        public Task<(bool IsValid, Message Response)> SendCommandAsync(string command)
        {
            return SendMessageAsync(new Message(
                command.Length + Encoder.HEADER_LENGTH,
                Interlocked.Increment(ref _lastId),
                MessageType.Command,
                command));
        }

        private async Task<(bool IsValid, Message Response)> SendMessageAsync(Message req)
        {
            if (!_networkStream.CanWrite || !_tcpClient.Connected)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                Dispose();
                return (false, default);
            }

            var encoded = Encoder.EncodeMessage(req);
            await _networkStream.WriteAsync(encoded, 0, encoded.Length);

            var respBytes = new byte[MAX_MESSAGE_SIZE];
            var bytesRead = await _networkStream.ReadAsync(respBytes, 0, respBytes.Length);

            Array.Resize(ref respBytes, bytesRead);

            var resp = Encoder.DecodeMessage(respBytes);
            
            return (req.Id == resp.Id, resp);
        }
    }
}
