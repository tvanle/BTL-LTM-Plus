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
        public static NetworkManager Instance => instance;

        [Header("Connection Settings")] [SerializeField]
        private string serverHost = "localhost";

        [SerializeField] private int serverPort = 8080;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;

        public event Action<GameMessage> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        // Player and Room Info
        public string PlayerId { get; private set; }
        public string RoomCode { get; private set; }
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
                this._stream = this._tcpClient.GetStream();
                this._isConnected = true;
                this._cancellationTokenSource = new CancellationTokenSource();

                this.OnConnected?.Invoke();

                _ = this.ReceiveMessagesTask();
                _ = this.HeartbeatTask();

                Debug.Log($"[CLIENT] Connected successfully at {DateTime.Now:HH:mm:ss.fff}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CLIENT] Connection failed: {ex.Message}");
                this.OnError?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public async void Disconnect()
        {
            if (!this._isConnected) return;

            Debug.Log($"[CLIENT] Disconnecting at {DateTime.Now:HH:mm:ss.fff}");

            this._isConnected = false;
            this._cancellationTokenSource?.Cancel();

            try
            {
                this._stream?.Close();
                this._tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CLIENT] Error during disconnect: {ex.Message}");
            }

            this.OnDisconnected?.Invoke();
            Debug.Log($"[CLIENT] Disconnected from server at {DateTime.Now:HH:mm:ss.fff}");

            await Task.CompletedTask;
        }

        public async Task<bool> CreateRoom(string username, string category, int maxPlayers = 50,
            int levelDuration = 30)
        {
            var createData = new CreateRoomData
            {
                Username = username,
                Category = category,
                MaxPlayers = maxPlayers,
                LevelDuration = levelDuration
            };

            var message = new GameMessage
            {
                Type = "CREATE_ROOM",
                Data = JsonUtility.ToJson(createData)
            };

            await this.SendMessageAsync(message);
            return true;
        }

        public async Task<bool> JoinRoom(string roomCode, string username)
        {
            var joinData = new JoinRoomData
            {
                RoomCode = roomCode,
                Username = username
            };

            var message = new GameMessage
            {
                Type = "JOIN_ROOM",
                Data = JsonUtility.ToJson(joinData)
            };

            await this.SendMessageAsync(message);
            return true;
        }

        public async Task LeaveRoom()
        {
            var message = new GameMessage { Type = "LEAVE_ROOM" };
            await this.SendMessageAsync(message);
            this.RoomCode = null;
            this.RoomPlayers.Clear();
        }

        public async Task SetReady()
        {
            var message = new GameMessage { Type = "PLAYER_READY" };
            await this.SendMessageAsync(message);
        }

        public async Task StartGame()
        {
            var message = new GameMessage { Type = "START_GAME" };
            await this.SendMessageAsync(message);
        }

        public async Task LevelCompleted(int timeTaken)
        {
            var levelData = new LevelCompletedData
            {
                TimeTaken = timeTaken
            };

            var message = new GameMessage
            {
                Type = "LEVEL_COMPLETED",
                Data = JsonUtility.ToJson(levelData)
            };

            await this.SendMessageAsync(message);
        }

        public async Task LevelTimeout()
        {
            var message = new GameMessage { Type = "LEVEL_TIMEOUT" };
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
                // Debug log outgoing messages (except frequent heartbeats)
                if (message.Type != "HEARTBEAT")
                {
                    Debug.Log($"[CLIENT SEND] {message.Type} data: {message.Data} at {DateTime.Now:HH:mm:ss.fff}");
                }

                var json = JsonUtility.ToJson(message);

                // Debug full JSON message
                if (message.Type != "HEARTBEAT")
                {
                    Debug.Log($"[CLIENT JSON] Full message: {json}");
                }

                var bytes = Encoding.UTF8.GetBytes(json);
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
            var buffer = new byte[4096];
            var messageBuffer = new List<byte>();

            while (this._isConnected && !this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var bytesRead =
                        await this._stream.ReadAsync(buffer, 0, buffer.Length, this._cancellationTokenSource.Token);

                    if (bytesRead == 0)
                    {
                        Debug.Log("Server disconnected");
                        break;
                    }

                    messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));

                    while (messageBuffer.Count >= 4)
                    {
                        var lengthBytes = messageBuffer.GetRange(0, 4).ToArray();
                        var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                        if (messageBuffer.Count >= 4 + messageLength)
                        {
                            var messageBytes = messageBuffer.GetRange(4, messageLength).ToArray();
                            messageBuffer.RemoveRange(0, 4 + messageLength);

                            var json = Encoding.UTF8.GetString(messageBytes);
                            var message = JsonUtility.FromJson<GameMessage>(json);

                            if (message != null)
                            {
                                // Debug log incoming messages
                                if (message.Type != "HEARTBEAT")
                                {
                                    Debug.Log($"[CLIENT RECV] {message.Type} at {DateTime.Now:HH:mm:ss.fff}");
                                }

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
                if (message.Type != "HEARTBEAT")
                {
                    Debug.Log($"Received: {message.Type}");
                }

                switch (message.Type)
                {
                    case "ROOM_CREATED":
                        var createData = JsonUtility.FromJson<RoomCreatedData>(message.Data);
                        this.RoomCode = createData.roomCode;
                        this.PlayerId = createData.playerId;
                        break;

                    case "ROOM_JOINED":
                        var joinData = JsonUtility.FromJson<RoomJoinedData>(message.Data);
                        this.RoomCode = joinData.roomCode;
                        this.PlayerId = joinData.playerId;
                        this.RoomPlayers = joinData.players;
                        break;

                    case "PLAYER_JOINED":
                        var playerJoined = JsonUtility.FromJson<PlayerJoinedData>(message.Data);
                        this.RoomPlayers.Add(new PlayerInfo
                            { Id = playerJoined.Id, Username = playerJoined.Username, IsReady = false });
                        break;

                    case "PLAYER_LEFT":
                        var playerLeft = JsonUtility.FromJson<PlayerLeftData>(message.Data);
                        this.RoomPlayers.RemoveAll(p => p.Id == playerLeft.playerId);
                        break;

                    case "ERROR":
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
                    await this.SendMessageAsync(new GameMessage { Type = "HEARTBEAT" });
                }
            }
        }

        private void OnDestroy()
        {
            this.Disconnect();
        }

        [Serializable]
        public class GameMessage
        {
            public string Type;
            public string Data;
        }

        [Serializable]
        public class PlayerInfo
        {
            public string Id;
            public string Username;
            public bool IsReady;
        }

        [Serializable]
        private class RoomCreatedData
        {
            public string roomCode;
            public string playerId;
        }

        [Serializable]
        private class RoomJoinedData
        {
            public string roomCode;
            public string playerId;
            public List<PlayerInfo> players;
        }

        [Serializable]
        public class CreateRoomData
        {
            public string Username;
            public string Category;
            public int MaxPlayers;
            public int LevelDuration;
        }

        [Serializable]
        public class JoinRoomData
        {
            public string RoomCode;
            public string Username;
        }

        [Serializable]
        public class LevelCompletedData
        {
            public int TimeTaken;
        }

        [Serializable]
        public class PlayerJoinedData
        {
            public string Id;
            public string Username;
        }

        [Serializable]
        public class PlayerLeftData
        {
            public string playerId;
        }

        [Serializable]
        public class ErrorData
        {
            public string error;
        }
    }
}