using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WordGame.Network
{
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager instance;
        public static  NetworkManager Instance => instance;

        [Header("Connection Settings")] [SerializeField] private string serverHost = "localhost";
        [SerializeField]                                 private int    serverPort = 8080;

        private TcpClient               _tcpClient;
        private NetworkStream           _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private bool                    _isConnected;

        public event Action<GameMessage> OnMessageReceived;
        public event Action              OnConnected;
        public event Action              OnDisconnected;
        public event Action<string>      OnError;

        // Player and Room Info
        public string           PlayerId    { get; private set; }
        public string           RoomCode    { get; private set; }
        public List<PlayerInfo> RoomPlayers { get; private set; } = new List<PlayerInfo>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Initialize UnityMainThreadDispatcher on main thread
            UnityMainThreadDispatcher.Initialize();
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                this._tcpClient = new TcpClient();
                await this._tcpClient.ConnectAsync(this.serverHost, this.serverPort);
                this._stream                  = this._tcpClient.GetStream();
                this._isConnected             = true;
                this._cancellationTokenSource = new CancellationTokenSource();

                this.OnConnected?.Invoke();

                _ = this.ReceiveMessagesTask();
                _ = this.HeartbeatTask();

                Debug.Log($"Connected to server at {this.serverHost}:{this.serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                this.OnError?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public async void Disconnect()
        {
            if (!this._isConnected) return;

            this._isConnected = false;
            this._cancellationTokenSource?.Cancel();

            try
            {
                this._stream?.Close();
                this._tcpClient?.Close();
            }
            catch
            {
                // ignored
            }

            this.OnDisconnected?.Invoke();
            Debug.Log("Disconnected from server");

            await Task.CompletedTask;
        }

        public async Task<bool> CreateRoom(string username, string category, int maxPlayers = 50, int levelDuration = 30)
        {
            var message = new GameMessage
            {
                Type = NetworkMessageType.CREATE_ROOM,
                Data = JsonUtility.ToJson(new CreateRoomData
                {
                    Username      = username,
                    Category      = category,
                    MaxPlayers    = maxPlayers,
                    LevelDuration = levelDuration
                })
            };

            await this.SendMessageAsync(message);
            return true;
        }

        public async Task<bool> JoinRoom(string roomCode, string username)
        {
            var message = new GameMessage
            {
                Type = NetworkMessageType.JOIN_ROOM,
                Data = JsonUtility.ToJson(new JoinRoomData
                {
                    RoomCode = roomCode,
                    Username = username
                })
            };

            await this.SendMessageAsync(message);
            return true;
        }

        public async Task LeaveRoom()
        {
            var message = new GameMessage { Type = NetworkMessageType.LEAVE_ROOM };
            await this.SendMessageAsync(message);
            this.RoomCode = null;
            this.RoomPlayers.Clear();
        }

        public async Task SetReady()
        {
            var message = new GameMessage { Type = NetworkMessageType.PLAYER_READY };
            await this.SendMessageAsync(message);
        }

        public async Task StartGame()
        {
            var message = new GameMessage { Type = NetworkMessageType.START_GAME };
            await this.SendMessageAsync(message);
        }

        public async Task LevelCompleted(int timeTaken)
        {
            var message = new GameMessage
            {
                Type = NetworkMessageType.LEVEL_COMPLETED,
                Data = JsonUtility.ToJson(new LevelCompletedData
                {
                    TimeTaken = timeTaken
                })
            };

            await this.SendMessageAsync(message);
        }

        public async Task LevelTimeout()
        {
            var message = new GameMessage { Type = NetworkMessageType.LEVEL_TIMEOUT };
            await this.SendMessageAsync(message);
        }

        private async Task SendMessageAsync(GameMessage message)
        {
            if (!this._isConnected || this._stream == null)
            {
                Debug.LogError("Not connected to server");
                return;
            }

            try
            {
                var json        = JsonUtility.ToJson(message);
                var bytes       = Encoding.UTF8.GetBytes(json);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);

                await this._stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, this._cancellationTokenSource.Token);
                await this._stream.WriteAsync(bytes, 0, bytes.Length, this._cancellationTokenSource.Token);
                await this._stream.FlushAsync(this._cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send failed: {ex.Message}");
                this.OnError?.Invoke($"Send failed: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesTask()
        {
            await Task.Run(this.ReceiveMessagesAsync);
        }

        private async Task HeartbeatTask()
        {
            await Task.Run(this.HeartbeatAsync);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer        = new byte[4096];
            var messageBuffer = new List<byte>();

            while (this._isConnected && !this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var bytesRead = await this._stream.ReadAsync(buffer, 0, buffer.Length, this._cancellationTokenSource.Token);

                    if (bytesRead == 0)
                    {
                        Debug.Log("Server disconnected");
                        break;
                    }

                    messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));

                    while (messageBuffer.Count >= 4)
                    {
                        var lengthBytes   = messageBuffer.GetRange(0, 4).ToArray();
                        var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                        if (messageBuffer.Count >= 4 + messageLength)
                        {
                            var messageBytes = messageBuffer.GetRange(4, messageLength).ToArray();
                            messageBuffer.RemoveRange(0, 4 + messageLength);

                            var json    = Encoding.UTF8.GetString(messageBytes);
                            var message = JsonUtility.FromJson<GameMessage>(json);

                            if (message != null)
                            {
                                this.HandleMessage(message);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this._isConnected)
                    {
                        Debug.LogError($"Receive error: {ex.Message}");
                    }
                    break;
                }
            }

            if (this._isConnected)
            {
                this.Disconnect();
            }
        }

        private void HandleMessage(GameMessage message)
        {
            var dispatcher = UnityMainThreadDispatcher.Instance;
            if (dispatcher == null)
            {
                Debug.LogError("UnityMainThreadDispatcher not initialized. Message dropped: " + message.Type);
                return;
            }

            dispatcher.Enqueue(() =>
            {
                Debug.Log($"Received: {message.Type.ToString()}");

                switch (message.Type)
                {
                    case NetworkMessageType.ROOM_CREATED:
                        var createData = JsonUtility.FromJson<RoomCreatedData>(message.Data);
                        this.RoomCode = createData.roomCode;
                        this.PlayerId = createData.playerId;
                        break;

                    case NetworkMessageType.ROOM_JOINED:
                        var joinData = JsonUtility.FromJson<RoomJoinedData>(message.Data);
                        this.RoomCode    = joinData.roomCode;
                        this.PlayerId    = joinData.playerId;
                        this.RoomPlayers = joinData.players;
                        break;

                    case NetworkMessageType.PLAYER_JOINED:
                        var playerJoined = JsonUtility.FromJson<PlayerJoinedData>(message.Data);
                        this.RoomPlayers.Add(new PlayerInfo { Id = playerJoined.Id, Username = playerJoined.Username, IsReady = false });
                        break;

                    case NetworkMessageType.PLAYER_LEFT:
                        var playerLeft = JsonUtility.FromJson<PlayerLeftData>(message.Data);
                        this.RoomPlayers.RemoveAll(p => p.Id == playerLeft.playerId);
                        break;

                    case NetworkMessageType.ERROR:
                        var errorData = JsonUtility.FromJson<ErrorData>(message.Data);
                        this.OnError?.Invoke(errorData.error);
                        break;
                }

                this.OnMessageReceived?.Invoke(message);
            });
        }

        private async Task HeartbeatAsync()
        {
            while (this._isConnected && !this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(10000, this._cancellationTokenSource.Token);

                if (this._isConnected)
                {
                    await this.SendMessageAsync(new GameMessage { Type = NetworkMessageType.HEARTBEAT });
                }
            }
        }

        private void OnDestroy()
        {
            this.Disconnect();
        }

        [Serializable] public class GameMessage
        {
            public NetworkMessageType Type { get; set; }
            public string Data { get; set; }
        }

        [Serializable] public class PlayerInfo
        {
            public string Id       { get; set; }
            public string Username { get; set; }
            public bool   IsReady  { get; set; }
        }

        [Serializable] private class RoomCreatedData
        {
            public string roomCode;
            public string playerId;
        }

        [Serializable] private class RoomJoinedData
        {
            public string           roomCode;
            public string           playerId;
            public List<PlayerInfo> players;
        }

        [Serializable] public class CreateRoomData
        {
            public string Username      { get; set; }
            public string Category      { get; set; }
            public int    MaxPlayers    { get; set; }
            public int    LevelDuration { get; set; }
        }

        [Serializable] public class JoinRoomData
        {
            public string RoomCode { get; set; }
            public string Username { get; set; }
        }

        [Serializable] public class LevelCompletedData
        {
            public int TimeTaken { get; set; }
        }

        [Serializable] public class PlayerJoinedData
        {
            public string Id       { get; set; }
            public string Username { get; set; }
        }

        [Serializable] public class PlayerLeftData
        {
            public string playerId;
        }

        [Serializable] public class ErrorData
        {
            public string error;
        }
    }
}