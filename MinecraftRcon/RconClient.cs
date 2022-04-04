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
        private bool _isDisposed;

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<Message> MessageReceived;

        public bool IsConnected => _tcpClient?.Connected ?? false;
        
        public void Dispose()
        {
            if (_isDisposed) return;

            _networkStream?.Dispose();
            _tcpClient?.Dispose();

            _isDisposed = true;
        }

        public async Task ConnectAsync(string host, int port)
        {
            _tcpClient = new TcpClient(host, port);

            if (IsConnected)
            {
                _networkStream = _tcpClient.GetStream();
                _ = Task.Run(ReadMessagesJob);
                Connected?.Invoke(this, EventArgs.Empty);
            }
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
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task AuthenticateAsync(string password)
        {
            return SendMessageAsync(new Message(
                password.Length + Encoder.HEADER_LENGTH,
                Interlocked.Increment(ref _lastId),
                MessageType.Authenticate,
                password));
        }

        public Task SendCommandAsync(string command)
        {
            return SendMessageAsync(new Message(
                command.Length + Encoder.HEADER_LENGTH,
                Interlocked.Increment(ref _lastId),
                MessageType.Command,
                command));
        }

        private async Task SendMessageAsync(Message req)
        {
            if (!IsConnected)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                Dispose();
            }

            // Send a new message
            var encoded = Encoder.EncodeMessage(req);
            await _networkStream.WriteAsync(encoded, 0, encoded.Length);
        }

        private async Task ReadMessagesJob()
        {
            while (IsConnected)
            {
                if (!_networkStream.DataAvailable)
                    continue;

                // Receive a message
                var respBytes = new byte[MAX_MESSAGE_SIZE];

                try
                {
                    var bytesRead = await _networkStream.ReadAsync(respBytes, 0, respBytes.Length);
                    if (bytesRead > 0)
                    {
                        Array.Resize(ref respBytes, bytesRead);
                        MessageReceived?.Invoke(this, Encoder.DecodeMessage(respBytes));
                    }
#if DEBUG
                    else
                    {
                        const string body = "DEBUG: EMPTY MESSAGE FROM SERVER";
                        MessageReceived?.Invoke(this, new Message(body.Length, -1, MessageType._, body));
                    }
#endif
                }
                catch (Exception e)
                {
                    var body = $"Error reading from server connection. {e.Message}";
                    MessageReceived?.Invoke(this, new Message(body.Length, -1, MessageType._, body));
                }
            }
        }
    }
}
