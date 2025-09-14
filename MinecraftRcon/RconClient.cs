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
        private int? _pendingAuthId;

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<Message> MessageReceived;
        public event EventHandler<bool> AuthenticationCompleted; // bool indicates success

        public bool IsConnected => _tcpClient?.Connected ?? false;
        public bool IsAuthenticated  { get; private set; }
        
        public void Dispose()
        {
            if (_isDisposed) return;

            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            IsAuthenticated = false;
            _pendingAuthId = null;

            _isDisposed = true;
        }

        public Task ConnectAsync(string host, int port)
        {
            _tcpClient = new TcpClient(host, port);

            if (IsConnected)
            {
                _networkStream = _tcpClient.GetStream();
                _ = Task.Run(ReadMessagesJob);
                Connected?.Invoke(this, EventArgs.Empty);
            }

            return Task.CompletedTask;
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
                IsAuthenticated = false;
                _pendingAuthId = null;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task AuthenticateAsync(string password)
        {
            IsAuthenticated = false;
            var authId = Interlocked.Increment(ref _lastId);
            _pendingAuthId = authId;
            
            return SendMessageAsync(new Message(
                password.Length + Encoder.HEADER_LENGTH,
                authId,
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
                        var message = Encoder.DecodeMessage(respBytes);
                        
                        // Handle authentication response according to protocol
                        if (_pendingAuthId.HasValue && HandleAuthenticationResponse(message))
                        {
                            // Authentication response was handled, don't forward to general handler
                            continue;
                        }
                        
                        MessageReceived?.Invoke(this, message);
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

        private bool HandleAuthenticationResponse(Message message)
        {
            if (!_pendingAuthId.HasValue)
                return false;

            // According to protocol: server responds with empty RESPONSE_VALUE followed by AUTH_RESPONSE
            // The AUTH_RESPONSE (type 2) packet ID indicates success/failure
            if (message.Type == MessageType.AuthResponse && string.IsNullOrEmpty(message.Body))
            {
                // Check if this is an auth response by looking at the ID
                if (message.Id == _pendingAuthId.Value)
                {
                    // Successful authentication
                    IsAuthenticated = true;
                    _pendingAuthId = null;
                    AuthenticationCompleted?.Invoke(this, true);
                    return true;
                }
                else if (message.Id == -1)
                {
                    // Failed authentication (ID = -1)
                    IsAuthenticated = false;
                    _pendingAuthId = null;
                    AuthenticationCompleted?.Invoke(this, false);
                    return true;
                }
            }
            
            return false;
        }
    }
}
