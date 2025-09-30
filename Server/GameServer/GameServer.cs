using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace WordBrainServer;

public class GameServer
{
    private readonly TcpListener                                  _tcpListener;
    private readonly ConcurrentDictionary<string, GameRoom>       _rooms       = new();
    private readonly ConcurrentDictionary<Guid, Player>           _players     = new();
    private readonly ConcurrentDictionary<Guid, ClientConnection> _connections = new();
    private          bool                                         _isRunning;
    private readonly int                                          _port;

    public GameServer(int port = 8080)
    {
        this._port     = port;
        this._tcpListener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        this._tcpListener.Start();
        this._isRunning = true;
        Console.WriteLine($"Game Server started on port {this._port}");

        _ = Task.Run(this.AcceptClientsAsync);
        _ = Task.Run(this.HeartbeatLoopAsync);
    }

    private async Task AcceptClientsAsync()
    {
        while (this._isRunning)
        {
            try
            {
                var tcpClient  = await this._tcpListener.AcceptTcpClientAsync();
                var connection = new ClientConnection(tcpClient);
                this._connections[connection.Id] = connection;

                Console.WriteLine($"Client connected: {connection.Id}");
                _ = Task.Run(() => this.HandleClientAsync(connection));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(ClientConnection connection)
    {
        try
        {
            await foreach (var message in connection.ReadMessagesAsync())
            {
                await this.ProcessMessageAsync(connection, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {connection.Id}: {ex.Message}");
        }
        finally
        {
            await this.DisconnectPlayerAsync(connection);
        }
    }

    private async Task ProcessMessageAsync(ClientConnection connection, GameMessage message)
    {
        Console.WriteLine($"Received message from {connection.Id}: {message.Type}");
        try
        {
            switch (message.Type)
            {
                case "CREATE_ROOM":
                    await this.HandleCreateRoom(connection, message);
                    break;
                case "JOIN_ROOM":
                    await this.HandleJoinRoom(connection, message);
                    break;
                case "LEAVE_ROOM":
                    await this.HandleLeaveRoom(connection);
                    break;
                case "PLAYER_READY":
                    await this.HandlePlayerReady(connection);
                    break;
                case "START_GAME":
                    await this.HandleStartGame(connection);
                    break;
                case "LEVEL_COMPLETED":
                    await this.HandleLevelCompleted(connection, message);
                    break;
                case "HEARTBEAT":
                    await connection.SendAsync(new GameMessage { Type = "HEARTBEAT" });
                    break;
            }
        }
        catch (Exception ex)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = ex.Message })
            });
        }
    }

    private async Task HandleCreateRoom(ClientConnection connection, GameMessage message)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<CreateRoomData>(message.Data, options);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            Username = data.Username,
            IsReady = true  // Host is automatically ready
        };

        this._players[player.Id] = player;
        connection.PlayerId = player.Id;

        var roomCode = this.GenerateRoomCode();
        var room = new GameRoom
        {
            Code = roomCode,
            HostId = player.Id,
            Category = data.Category,
            MaxPlayers = data.MaxPlayers,
            LevelDuration = data.LevelDuration
        };

        room.Players[player.Id] = player;
        player.RoomCode = roomCode;

        this._rooms[roomCode] = room;

        await connection.SendAsync(new GameMessage
        {
            Type = "ROOM_CREATED",
            Data = JsonSerializer.Serialize(new { roomCode, player = new { player.Id, player.Username, player.IsReady } })
        });

        Console.WriteLine($"Room {roomCode} created by {player.Username}");
        Console.WriteLine($"[DEBUG] Room has {room.Players.Count} player(");
    }

    private async Task HandleJoinRoom(ClientConnection connection, GameMessage message)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<JoinRoomData>(message.Data, options);

        if (!this._rooms.TryGetValue(data.RoomCode, out var room))
        {
            throw new Exception("Room not found");
        }

        if (room.Players.Count >= room.MaxPlayers)
        {
            throw new Exception("Room is full");
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            Username = data.Username,
            RoomCode = data.RoomCode
        };

        this._players[player.Id] = player;
        connection.PlayerId = player.Id;
        room.Players[player.Id] = player;

        await connection.SendAsync(new GameMessage
        {
            Type = "ROOM_JOINED",
            Data = JsonSerializer.Serialize(new
            {
                roomCode = room.Code,
                playerId = player.Id,
                players = room.Players.Values.Select(p => new { p.Id, p.Username, p.IsReady })
            })
        });

        await this.BroadcastToRoomExcept(room, connection.Id, new GameMessage
        {
            Type = "PLAYER_JOINED",
            Data = JsonSerializer.Serialize(new { player.Id, player.Username })
        });

        Console.WriteLine($"{player.Username} joined room {room.Code}");
        Console.WriteLine($"[DEBUG] Room now has {room.Players.Count} players");
    }

    private async Task HandleLeaveRoom(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = this._rooms.GetValueOrDefault(player.RoomCode);
        if (room == null)
            return;

        room.Players.TryRemove(player.Id, out _);
        player.RoomCode = null;

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "PLAYER_LEFT",
            Data = JsonSerializer.Serialize(new { playerId = player.Id })
        });

        if (room.Players.Count == 0)
        {
            this._rooms.TryRemove(room.Code, out _);
            Console.WriteLine($"Room {room.Code} closed");
        }
    }

    private async Task HandlePlayerReady(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = this._rooms.GetValueOrDefault(player.RoomCode);
        if (room == null)
            return;

        player.IsReady = true;

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "PLAYER_READY",
            Data = JsonSerializer.Serialize(new { playerId = player.Id, isReady = true })
        });
    }

    private async Task HandleStartGame(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
        {
            Console.WriteLine($"[DEBUG] Connection has no PlayerId");
            return;
        }

        var player = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
        {
            Console.WriteLine($"[DEBUG] Player {connection.PlayerId.Value} has no room");
            return;
        }

        var room = this._rooms.GetValueOrDefault(player.RoomCode);
        if (room == null)
        {
            throw new Exception($"Room {player.RoomCode} not found");
        }

        if (room.HostId != player.Id)
        {
            throw new Exception($"Only host can start the game. Host: {room.HostId}, Player: {player.Id}");
        }

        // Debug: Log player ready states
        foreach (var p in room.Players.Values)
        {
            Console.WriteLine($"[DEBUG] Player {p.Username} (ID: {p.Id}) - Ready: {p.IsReady}");
        }

        if (!room.Players.Values.All(p => p.IsReady))
        {
            var notReadyPlayers = room.Players.Values.Where(p => !p.IsReady).Select(p => p.Username);
            throw new Exception($"Not all players are ready. Not ready: {string.Join(", ", notReadyPlayers)}");
        }

        room.GameState = new GameState
        {
            CurrentLevel = 1,
            LevelStartTime = DateTime.UtcNow
        };

        // Handle "Random" category by selecting a random category and level
        string actualCategory = room.Category;
        int actualLevel = room.GameState.CurrentLevel;

        if (room.Category.Equals("Random", StringComparison.OrdinalIgnoreCase))
        {
            // Pick a random category from 1-10 (adjust based on your actual categories)
            var random = new Random();
            int categoryNum = random.Next(1, 16); // Categories 1-15
            actualCategory = $"Category {categoryNum}";

            // Pick a random level (0-19 is common range, adjust as needed)
            actualLevel = random.Next(0, 20);
        }

        // Start level timer (60 seconds - server manages completely)
        room.GameState.LevelTimer = new Timer(
            async _ => await this.HandleLevelTimerExpired(room),
            null,
            TimeSpan.FromSeconds(60),
            Timeout.InfiniteTimeSpan
        );

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "GAME_STARTED",
            Data = JsonSerializer.Serialize(new
            {
                category = actualCategory,
                level = actualLevel
            })
        });

        Console.WriteLine($"Game started in room {room.Code}");
    }
    
    private async Task HandleLevelCompleted(ClientConnection connection, GameMessage message)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = this._rooms.GetValueOrDefault(player.RoomCode);
        if (room?.GameState == null)
            return;

        // Parse time taken
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<LevelCompletedData>(message.Data, options);

        // Mark player as completed
        room.GameState.CompletedPlayers.Add(player.Id);

        // Calculate score
        var timeTaken = data?.TimeTaken ?? 60;
        player.Score += this.CalculateScore(timeTaken);

        Console.WriteLine($"Player {player.Username} completed level {room.GameState.CurrentLevel} in {timeTaken}s");

        // Check if all players completed
        if (room.GameState.CompletedPlayers.Count == room.Players.Count)
        {
            // Cancel timer and move to next level
            room.GameState.LevelTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            room.GameState.LevelTimer?.Dispose();

            // Wait 3 seconds before showing leaderboard
            await Task.Delay(3000);

            // Send level ended with scores
            await this.SendLevelEnded(room);

            // Wait 6 seconds for leaderboard display
            await Task.Delay(6000);

            // Then move to next level
            await this.NextLevel(room);
        }
    }

    // Removed HandleLevelTimeout - server timer handles everything automatically

    private async Task HandleLevelTimerExpired(GameRoom room)
    {
        if (room.GameState == null)
            return;

        Console.WriteLine($"Level {room.GameState.CurrentLevel} timer expired for room {room.Code}");

        // Mark all non-completed players as timed out (score = 0 for this level)
        foreach (var player in room.Players.Values)
        {
            if (room.GameState.CompletedPlayers.Add(player.Id))
            {
                Console.WriteLine($"Player {player.Username} timed out on level {room.GameState.CurrentLevel}");
            }
        }

        // Send level ended with current scores
        await this.SendLevelEnded(room);

        // Wait 6 seconds for leaderboard display
        await Task.Delay(6000);

        // Then move to next level
        await this.NextLevel(room);
    }

    private async Task SendLevelEnded(GameRoom room)
    {
        if (room.GameState == null)
            return;

        // Get sorted player scores for leaderboard
        var leaderboard = room.Players.Values
            .OrderByDescending(p => p.Score)
            .Select(p => new
            {
                playerId = p.Id,
                username = p.Username,
                score = p.Score
            })
            .ToList();

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "LEVEL_ENDED",
            Data = JsonSerializer.Serialize(new
            {
                level = room.GameState.CurrentLevel,
                leaderboard = leaderboard
            })
        });

        Console.WriteLine($"Level {room.GameState.CurrentLevel} ended in room {room.Code}");
    }

    private async Task NextLevel(GameRoom room)
    {
        if (room.GameState == null)
            return;

        room.GameState.CurrentLevel++;
        room.GameState.CompletedPlayers.Clear();
        room.GameState.LevelStartTime = DateTime.UtcNow;

        if (room.GameState.CurrentLevel > room.TotalLevels)
        {
            await this.EndGame(room);
            return;
        }

        // Restart timer for next level
        room.GameState.LevelTimer?.Dispose();
        room.GameState.LevelTimer = new Timer(
            async _ => await this.HandleLevelTimerExpired(room),
            null,
            TimeSpan.FromSeconds(60),
            Timeout.InfiniteTimeSpan
        );

        // Handle Random category for next level too
        string actualCategory = room.Category;
        int actualLevel = room.GameState.CurrentLevel;

        if (room.Category.Equals("Random", StringComparison.OrdinalIgnoreCase))
        {
            var random = new Random();
            int categoryNum = random.Next(1, 16);
            actualCategory = $"Category {categoryNum}";
            actualLevel = random.Next(0, 20);
        }

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "NEXT_LEVEL",
            Data = JsonSerializer.Serialize(new
            {
                category = actualCategory,
                level = actualLevel
            })
        });

        Console.WriteLine($"Starting level {room.GameState.CurrentLevel} in room {room.Code}");
    }

    private async Task EndGame(GameRoom room)
    {
        var results = room.Players.Values
            .OrderByDescending(p => p.Score)
            .Select(p => new { p.Id, p.Username, p.Score })
            .ToList();

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "GAME_ENDED",
            Data = JsonSerializer.Serialize(new { results })
        });

        room.GameState = null;
        foreach (var player in room.Players.Values)
        {
            player.IsReady = false;
            player.Score = 0;
        }
    }

    private async Task BroadcastToRoom(GameRoom room, GameMessage message)
    {
        var tasks = room.Players.Values
            .Select(p => this._connections.GetValueOrDefault(p.ConnectionId))
            .Where(c => c != null)
            .Select(c => c.SendAsync(message));

        await Task.WhenAll(tasks);
    }

    private async Task BroadcastToRoomExcept(GameRoom room, Guid exceptConnectionId, GameMessage message)
    {
        var tasks = room.Players.Values
            .Where(p => p.ConnectionId != exceptConnectionId)
            .Select(p => this._connections.GetValueOrDefault(p.ConnectionId))
            .Where(c => c != null)
            .Select(c => c.SendAsync(message));

        await Task.WhenAll(tasks);
    }

    private async Task DisconnectPlayerAsync(ClientConnection connection)
    {
        await this.HandleLeaveRoom(connection);

        if (connection.PlayerId.HasValue)
        {
            this._players.TryRemove(connection.PlayerId.Value, out _);
        }

        this._connections.TryRemove(connection.Id, out _);
        await connection.DisconnectAsync();

        Console.WriteLine($"Client disconnected: {connection.Id}");
    }

    private async Task HeartbeatLoopAsync()
    {
        while (this._isRunning)
        {
            var disconnected = this._connections.Values
                .Where(c => c.IsTimedOut())
                .ToList();

            foreach (var connection in disconnected)
            {
                await this.DisconnectPlayerAsync(connection);
            }

            await Task.Delay(5000);
        }
    }

    private string GenerateRoomCode()
    {
        const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;

        do
        {
            code = new string(Enumerable.Range(0, 6)
                .Select(_ => CHARS[random.Next(CHARS.Length)])
                .ToArray());
        } while (this._rooms.ContainsKey(code));

        return code;
    }

    // Removed GenerateGrid and GenerateTargetWords - client will use local board data

    private int CalculateScore(int timeTaken)
    {
        var baseScore = 100;
        var timeBonus = Math.Max(0, 60 - timeTaken) * 2;
        return baseScore + timeBonus;
    }

    public async Task StopAsync()
    {
        this._isRunning = false;
        this._tcpListener.Stop();

        foreach (var connection in this._connections.Values)
        {
            await connection.DisconnectAsync();
        }

        Console.WriteLine("Game Server stopped");
    }
}

public class ClientConnection
{
    public Guid Id { get; }
    public Guid? PlayerId { get; set; }
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private DateTime _lastHeartbeat;

    public ClientConnection(TcpClient tcpClient)
    {
        this.Id          = Guid.NewGuid();
        this._tcpClient  = tcpClient;
        this._stream     = tcpClient.GetStream();
        this._lastHeartbeat = DateTime.UtcNow;
    }

    public async Task SendAsync(GameMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(bytes.Length);

        await this._stream.WriteAsync(lengthBytes);
        await this._stream.WriteAsync(bytes);
        await this._stream.FlushAsync();
    }

    public async IAsyncEnumerable<GameMessage> ReadMessagesAsync()
    {
        var buffer = new byte[4096];
        var messageBuffer = new List<byte>();

        while (this._tcpClient.Connected)
        {
            var bytesRead = await this._stream.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            this._lastHeartbeat = DateTime.UtcNow;
            messageBuffer.AddRange(buffer.Take(bytesRead));

            while (messageBuffer.Count >= 4)
            {
                var lengthBytes = messageBuffer.Take(4).ToArray();
                var messageLength = BitConverter.ToInt32(lengthBytes);

                if (messageBuffer.Count >= 4 + messageLength)
                {
                    var messageBytes = messageBuffer.Skip(4).Take(messageLength).ToArray();
                    messageBuffer.RemoveRange(0, 4 + messageLength);

                    var json = Encoding.UTF8.GetString(messageBytes);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var message = JsonSerializer.Deserialize<GameMessage>(json, options);

                    if (message != null)
                    {
                        Console.WriteLine($"[DEBUG] Parsed Type: '{message.Type}', Data: '{message.Data}'");
                        yield return message;
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to parse message: {json}");
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    public bool IsTimedOut() => DateTime.UtcNow.Subtract(this._lastHeartbeat).TotalSeconds > 30;

    public async Task DisconnectAsync()
    {
        try
        {
            this._stream?.Close();
            this._tcpClient?.Close();
        }
        catch { }

        await Task.CompletedTask;
    }
}

public class GameRoom
{
    public string Code { get; set; }
    public Guid HostId { get; set; }
    public string Category { get; set; }
    public int MaxPlayers { get; set; } = 50;
    public int LevelDuration { get; set; } = 30;
    public int TotalLevels { get; set; } = 10;
    public ConcurrentDictionary<Guid, Player> Players { get; } = new();
    public GameState? GameState { get; set; }
}

public class Player
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? RoomCode { get; set; }
    public bool IsReady { get; set; } = true;
    public int Score { get; set; }
}

public class GameState
{
    public int CurrentLevel { get; set; }
    public Dictionary<string, int> PlayerScores { get; set; } = new();
    public HashSet<Guid> CompletedPlayers { get; set; } = new();
    public DateTime LevelStartTime { get; set; }
    public Timer? LevelTimer { get; set; }
}

public class GameMessage
{
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

public class CreateRoomData
{
    public string Username { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 50;
    public int LevelDuration { get; set; } = 30;
}

public class JoinRoomData
{
    public string RoomCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class SubmitAnswerData
{
    public string Answer { get; set; } = string.Empty;
    public int TimeTaken { get; set; }
}

public class LevelCompletedData
{
    public int TimeTaken { get; set; }
}