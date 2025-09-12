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

namespace WordBrainServer;

public class GameServer
{
    private TcpListener _tcpListener;
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly ConcurrentDictionary<Guid, Player> _players = new();
    private readonly ConcurrentDictionary<Guid, ClientConnection> _connections = new();
    private bool _isRunning;
    private readonly int _port;

    public GameServer(int port = 8080)
    {
        _port = port;
        _tcpListener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync()
    {
        _tcpListener.Start();
        _isRunning = true;
        Console.WriteLine($"Game Server started on port {_port}");

        _ = Task.Run(AcceptClientsAsync);
        _ = Task.Run(HeartbeatLoopAsync);
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                var connection = new ClientConnection(tcpClient);
                _connections[connection.Id] = connection;
                
                Console.WriteLine($"Client connected: {connection.Id}");
                _ = Task.Run(() => HandleClientAsync(connection));
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
                await ProcessMessageAsync(connection, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {connection.Id}: {ex.Message}");
        }
        finally
        {
            await DisconnectPlayerAsync(connection);
        }
    }

    private async Task ProcessMessageAsync(ClientConnection connection, GameMessage message)
    {
        try
        {
            switch (message.Type)
            {
                case "CREATE_ROOM":
                    await HandleCreateRoom(connection, message);
                    break;
                case "JOIN_ROOM":
                    await HandleJoinRoom(connection, message);
                    break;
                case "LEAVE_ROOM":
                    await HandleLeaveRoom(connection);
                    break;
                case "PLAYER_READY":
                    await HandlePlayerReady(connection);
                    break;
                case "START_GAME":
                    await HandleStartGame(connection);
                    break;
                case "SUBMIT_ANSWER":
                    await HandleSubmitAnswer(connection, message);
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
        var data = JsonSerializer.Deserialize<CreateRoomData>(message.Data);
        
        var player = new Player
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            Username = data.Username
        };
        
        _players[player.Id] = player;
        connection.PlayerId = player.Id;

        var roomCode = GenerateRoomCode();
        var room = new GameRoom
        {
            Code = roomCode,
            HostId = player.Id,
            Topic = data.Topic,
            MaxPlayers = data.MaxPlayers,
            LevelDuration = data.LevelDuration
        };
        
        room.Players[player.Id] = player;
        player.RoomCode = roomCode;
        
        _rooms[roomCode] = room;

        await connection.SendAsync(new GameMessage
        {
            Type = "ROOM_CREATED",
            Data = JsonSerializer.Serialize(new { roomCode, playerId = player.Id })
        });

        Console.WriteLine($"Room {roomCode} created by {player.Username}");
    }

    private async Task HandleJoinRoom(ClientConnection connection, GameMessage message)
    {
        var data = JsonSerializer.Deserialize<JoinRoomData>(message.Data);
        
        if (!_rooms.TryGetValue(data.RoomCode, out var room))
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
        
        _players[player.Id] = player;
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

        await BroadcastToRoomExcept(room, connection.Id, new GameMessage
        {
            Type = "PLAYER_JOINED",
            Data = JsonSerializer.Serialize(new { player.Id, player.Username })
        });

        Console.WriteLine($"{player.Username} joined room {room.Code}");
    }

    private async Task HandleLeaveRoom(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = _players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = _rooms.GetValueOrDefault(player.RoomCode);
        if (room == null)
            return;

        room.Players.TryRemove(player.Id, out _);
        player.RoomCode = null;

        await BroadcastToRoom(room, new GameMessage
        {
            Type = "PLAYER_LEFT",
            Data = JsonSerializer.Serialize(new { playerId = player.Id })
        });

        if (room.Players.Count == 0)
        {
            _rooms.TryRemove(room.Code, out _);
            Console.WriteLine($"Room {room.Code} closed");
        }
    }

    private async Task HandlePlayerReady(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = _players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = _rooms.GetValueOrDefault(player.RoomCode);
        if (room == null)
            return;

        player.IsReady = true;

        await BroadcastToRoom(room, new GameMessage
        {
            Type = "PLAYER_READY",
            Data = JsonSerializer.Serialize(new { playerId = player.Id, isReady = true })
        });
    }

    private async Task HandleStartGame(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = _players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = _rooms.GetValueOrDefault(player.RoomCode);
        if (room == null || room.HostId != player.Id)
            throw new Exception("Only host can start the game");

        if (!room.Players.Values.All(p => p.IsReady))
            throw new Exception("Not all players are ready");

        room.GameState = new GameState
        {
            CurrentLevel = 1,
            GridData = GenerateGrid(room.CurrentLevel),
            TargetWords = GenerateTargetWords(room.Topic, room.CurrentLevel)
        };

        await BroadcastToRoom(room, new GameMessage
        {
            Type = "GAME_STARTED",
            Data = JsonSerializer.Serialize(new 
            { 
                level = room.GameState.CurrentLevel,
                grid = room.GameState.GridData,
                words = room.GameState.TargetWords,
                duration = room.LevelDuration
            })
        });

        Console.WriteLine($"Game started in room {room.Code}");
    }

    private async Task HandleSubmitAnswer(ClientConnection connection, GameMessage message)
    {
        if (!connection.PlayerId.HasValue)
            return;

        var player = _players.GetValueOrDefault(connection.PlayerId.Value);
        if (player?.RoomCode == null)
            return;

        var room = _rooms.GetValueOrDefault(player.RoomCode);
        if (room?.GameState == null)
            return;

        var data = JsonSerializer.Deserialize<SubmitAnswerData>(message.Data);
        
        bool isCorrect = room.GameState.TargetWords.Contains(data.Answer, StringComparer.OrdinalIgnoreCase);
        
        if (isCorrect)
        {
            player.Score += CalculateScore(data.TimeTaken);
            room.GameState.FoundWords.Add(data.Answer);
        }

        await BroadcastToRoom(room, new GameMessage
        {
            Type = "ANSWER_RESULT",
            Data = JsonSerializer.Serialize(new 
            { 
                playerId = player.Id,
                answer = data.Answer,
                isCorrect,
                score = player.Score,
                foundWords = room.GameState.FoundWords
            })
        });

        if (room.GameState.FoundWords.Count == room.GameState.TargetWords.Count)
        {
            await NextLevel(room);
        }
    }

    private async Task NextLevel(GameRoom room)
    {
        room.GameState.CurrentLevel++;
        
        if (room.GameState.CurrentLevel > room.TotalLevels)
        {
            await EndGame(room);
            return;
        }

        room.GameState.GridData = GenerateGrid(room.GameState.CurrentLevel);
        room.GameState.TargetWords = GenerateTargetWords(room.Topic, room.GameState.CurrentLevel);
        room.GameState.FoundWords.Clear();

        await BroadcastToRoom(room, new GameMessage
        {
            Type = "NEXT_LEVEL",
            Data = JsonSerializer.Serialize(new 
            { 
                level = room.GameState.CurrentLevel,
                grid = room.GameState.GridData,
                words = room.GameState.TargetWords,
                duration = room.LevelDuration
            })
        });
    }

    private async Task EndGame(GameRoom room)
    {
        var results = room.Players.Values
            .OrderByDescending(p => p.Score)
            .Select(p => new { p.Id, p.Username, p.Score })
            .ToList();

        await BroadcastToRoom(room, new GameMessage
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
            .Select(p => _connections.GetValueOrDefault(p.ConnectionId))
            .Where(c => c != null)
            .Select(c => c.SendAsync(message));
        
        await Task.WhenAll(tasks);
    }

    private async Task BroadcastToRoomExcept(GameRoom room, Guid exceptConnectionId, GameMessage message)
    {
        var tasks = room.Players.Values
            .Where(p => p.ConnectionId != exceptConnectionId)
            .Select(p => _connections.GetValueOrDefault(p.ConnectionId))
            .Where(c => c != null)
            .Select(c => c.SendAsync(message));
        
        await Task.WhenAll(tasks);
    }

    private async Task DisconnectPlayerAsync(ClientConnection connection)
    {
        await HandleLeaveRoom(connection);
        
        if (connection.PlayerId.HasValue)
        {
            _players.TryRemove(connection.PlayerId.Value, out _);
        }
        
        _connections.TryRemove(connection.Id, out _);
        await connection.DisconnectAsync();
        
        Console.WriteLine($"Client disconnected: {connection.Id}");
    }

    private async Task HeartbeatLoopAsync()
    {
        while (_isRunning)
        {
            var disconnected = _connections.Values
                .Where(c => c.IsTimedOut())
                .ToList();

            foreach (var connection in disconnected)
            {
                await DisconnectPlayerAsync(connection);
            }

            await Task.Delay(5000);
        }
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;
        
        do
        {
            code = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        } while (_rooms.ContainsKey(code));
        
        return code;
    }

    private string GenerateGrid(int level)
    {
        var size = Math.Min(4 + level / 3, 8);
        var random = new Random();
        var grid = new char[size * size];
        
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = (char)('A' + random.Next(26));
        }
        
        return new string(grid);
    }

    private List<string> GenerateTargetWords(string topic, int level)
    {
        var wordCount = Math.Min(3 + level / 2, 8);
        var words = new List<string>();
        
        for (int i = 0; i < wordCount; i++)
        {
            words.Add($"{topic}_{level}_{i}");
        }
        
        return words;
    }

    private int CalculateScore(int timeTaken)
    {
        var baseScore = 100;
        var timeBonus = Math.Max(0, 60 - timeTaken) * 2;
        return baseScore + timeBonus;
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        _tcpListener.Stop();
        
        foreach (var connection in _connections.Values)
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
        Id = Guid.NewGuid();
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
        _lastHeartbeat = DateTime.UtcNow;
    }

    public async Task SendAsync(GameMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(bytes.Length);
        
        await _stream.WriteAsync(lengthBytes);
        await _stream.WriteAsync(bytes);
        await _stream.FlushAsync();
    }

    public async IAsyncEnumerable<GameMessage> ReadMessagesAsync()
    {
        var buffer = new byte[4096];
        var messageBuffer = new List<byte>();

        while (_tcpClient.Connected)
        {
            var bytesRead = await _stream.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            _lastHeartbeat = DateTime.UtcNow;
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
                    var message = JsonSerializer.Deserialize<GameMessage>(json);
                    
                    if (message != null)
                        yield return message;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public bool IsTimedOut() => DateTime.UtcNow.Subtract(_lastHeartbeat).TotalSeconds > 30;

    public async Task DisconnectAsync()
    {
        try
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
        catch { }
        
        await Task.CompletedTask;
    }
}

public class GameRoom
{
    public string Code { get; set; }
    public Guid HostId { get; set; }
    public string Topic { get; set; }
    public int MaxPlayers { get; set; } = 50;
    public int LevelDuration { get; set; } = 30;
    public int TotalLevels { get; set; } = 10;
    public int CurrentLevel { get; set; } = 0;
    public ConcurrentDictionary<Guid, Player> Players { get; } = new();
    public GameState? GameState { get; set; }
}

public class Player
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? RoomCode { get; set; }
    public bool IsReady { get; set; }
    public int Score { get; set; }
}

public class GameState
{
    public int CurrentLevel { get; set; }
    public string GridData { get; set; } = string.Empty;
    public List<string> TargetWords { get; set; } = new();
    public HashSet<string> FoundWords { get; set; } = new();
}

public class GameMessage
{
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

public class CreateRoomData
{
    public string Username { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
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