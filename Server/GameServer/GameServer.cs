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
using GameServer.Services;
using GameServer.Models;
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
    private readonly DatabaseService                             _database;
    private readonly AuthenticationService                       _auth;

    public GameServer(int port = 8080, string? connectionString = null)
    {
        this._port     = port;
        this._tcpListener = new TcpListener(IPAddress.Any, port);

        // Initialize database (use default connection string if none provided)
        connectionString ??= "Server=localhost;Database=word_game;User=root;Password=;";
        this._database = new DatabaseService(connectionString);
        this._auth = new AuthenticationService(this._database);
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
                case "REGISTER":
                    await this.HandleRegister(connection, message);
                    break;
                case "LOGIN":
                    await this.HandleLogin(connection, message);
                    break;
                case "LOGOUT":
                    await this.HandleLogout(connection);
                    break;
                case "UPDATE_PROFILE":
                    await this.HandleUpdateProfile(connection, message);
                    break;
                case "CREATE_ROOM":
                    await this.HandleCreateRoom(connection, message);
                    break;
                case "JOIN_ROOM":
                    await this.HandleJoinRoom(connection, message);
                    break;
                case "LEAVE_ROOM":
                    await this.HandleLeaveRoom(connection);
                    break;
                case "START_GAME":
                    await this.HandleStartGame(connection);
                    break;
                case "LEVEL_COMPLETED":
                    await this.HandleLevelCompleted(connection, message);
                    break;
                case "GET_FRIENDS":
                    await this.HandleGetFriends(connection);
                    break;
                case "ADD_FRIEND":
                    await this.HandleAddFriend(connection, message);
                    break;
                case "ACCEPT_FRIEND":
                    await this.HandleAcceptFriend(connection, message);
                    break;
                case "REMOVE_FRIEND":
                    await this.HandleRemoveFriend(connection, message);
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

        // Get user avatar from database
        var user = await this._database.GetUserByUsernameAsync(data.Username);
        string? avatarUrl = user?.AvatarUrl;

        var player = new Player
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            Username = data.Username,
            AvatarUrl = avatarUrl
        };

        this._players[player.Id] = player;
        connection.PlayerId = player.Id;

        var roomCode = this.GenerateRoomCode();
        var room = new GameRoom
        {
            Code = roomCode,
            HostId = player.Id,
            Category = data.Category,
            LevelDuration = data.LevelDuration,
            NumQuestions = data.NumQuestions
        };

        room.Players[player.Id] = player;
        player.RoomCode = roomCode;

        this._rooms[roomCode] = room;

        await connection.SendAsync(new GameMessage
        {
            Type = "ROOM_CREATED",
            Data = JsonSerializer.Serialize(new { roomCode, category = room.Category, numQuestions = room.NumQuestions, player = new { player.Id, player.Username, player.AvatarUrl } })
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

        // Get user avatar from database
        var user = await this._database.GetUserByUsernameAsync(data.Username);
        string? avatarUrl = user?.AvatarUrl;

        var player = new Player
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            Username = data.Username,
            AvatarUrl = avatarUrl,
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
                category = room.Category,
                numQuestions = room.NumQuestions,
                playerId = player.Id,
                players = room.Players.Values.Select(p => new { p.Id, p.Username, p.AvatarUrl })
            })
        });

        await this.BroadcastToRoomExcept(room, connection.Id, new GameMessage
        {
            Type = "PLAYER_JOINED",
            Data = JsonSerializer.Serialize(new { player.Id, player.Username, player.AvatarUrl })
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

    private async Task HandleStartGame(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
        {
            Console.WriteLine($"[DEBUG] Connection has no PlayerId");
            return;
    private async Task HandleGetOnlinePlayers(ClientConnection connection)
    {
        if (!connection.PlayerId.HasValue)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Not logged in" })
            });
            return;
        }

        var currentPlayer = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (currentPlayer == null)
        {
            return;
        }

        // Get all online players
        var onlinePlayers = this._players.Values
            .Where(p => p.Id != currentPlayer.Id) // Exclude self
            .Select(p => new
            {
                PlayerId = p.Id.ToString(),
                Username = p.Username,
                InRoom = !string.IsNullOrEmpty(p.RoomCode)
            })
            .ToList();

        await connection.SendAsync(new GameMessage
        {
            Type = "ONLINE_PLAYERS",
            Data = JsonSerializer.Serialize(new { players = onlinePlayers })
        });

        Console.WriteLine($"[GET_ONLINE_PLAYERS] Sent {onlinePlayers.Count} online players to {currentPlayer.Username}");
    }

    private async Task HandleSendInvite(ClientConnection connection, GameMessage message)
    {
        if (!connection.PlayerId.HasValue)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Not logged in" })
            });
            return;
        }

        var sender = this._players.GetValueOrDefault(connection.PlayerId.Value);
        if (sender == null || string.IsNullOrEmpty(sender.RoomCode))
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Not in a room" })
            });
            return;
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SendInviteData>(message.Data, options);

        if (!Guid.TryParse(data.TargetPlayerId, out var targetPlayerId))
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Invalid player ID" })
            });
            return;
        }

        var targetPlayer = this._players.GetValueOrDefault(targetPlayerId);
        if (targetPlayer == null)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Target player not found" })
            });
            return;
        }

        // Check if target player is already in a room
        if (!string.IsNullOrEmpty(targetPlayer.RoomCode))
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Player is already in a room" })
            });
            return;
        }

        // Get target player's connection
        var targetConnection = this._connections.Values.FirstOrDefault(c => c.PlayerId == targetPlayerId);
        if (targetConnection == null)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "ERROR",
                Data = JsonSerializer.Serialize(new { error = "Target player is offline" })
            });
            return;
        }

        // Send invite to target player
        await targetConnection.SendAsync(new GameMessage
        {
            Type = "ROOM_INVITE",
            Data = JsonSerializer.Serialize(new
            {
                inviterName = sender.Username,
                roomCode = sender.RoomCode
            })
        });

        // Confirm to sender
        await connection.SendAsync(new GameMessage
        {
            Type = "INVITE_SENT",
            Data = JsonSerializer.Serialize(new { success = true })
        });

        Console.WriteLine($"[SEND_INVITE] {sender.Username} invited {targetPlayer.Username} to room {sender.RoomCode}");
    }

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

        // Calculate score server-side (authoritative)
        var timeTaken = data?.TimeTaken ?? 60;
        player.Streak++; // Increment streak on level completion
        player.WordsFoundThisLevel++;
        player.TotalWordsFound++;
        var scoreGained = this.CalculateCorrectAnswerScore(player, timeTaken, room.LevelDuration);
        player.Score += scoreGained;

        Console.WriteLine($"Player {player.Username} completed level {room.GameState.CurrentLevel} in {timeTaken}s. Total Score: {player.Score}");

        // Send score update back to this player
        var playerConnection = this._connections.GetValueOrDefault(player.ConnectionId);
        if (playerConnection != null)
        {
            await playerConnection.SendAsync(new GameMessage
            {
                Type = "SCORE_UPDATE",
                Data = JsonSerializer.Serialize(new
                {
                    scoreGained = scoreGained,
                    totalScore = player.Score,
                    streak = player.Streak
                })
            });
        }

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

        // Mark all non-completed players as timed out (score = 0 for this level, streak reset)
        foreach (var player in room.Players.Values)
        {
            if (room.GameState.CompletedPlayers.Add(player.Id))
            {
                // Reset streak for players who didn't complete in time
                player.Streak = 0;
                Console.WriteLine($"Player {player.Username} timed out on level {room.GameState.CurrentLevel}. Streak reset!");
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
        var results = room.Players.Values
            .OrderByDescending(p => p.Score)
            .Select(p => new
            {
                Id = p.Id.ToString(),
                Username = p.Username,
                AvatarUrl = p.AvatarUrl,
                Score = p.Score
            })
            .ToList();

        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "LEVEL_ENDED",
            Data = JsonSerializer.Serialize(new
            {
                level = room.GameState.CurrentLevel,
                results = results
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

        // Reset words found counter for all players
        foreach (var player in room.Players.Values)
        {
            player.WordsFoundThisLevel = 0;
        }

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
        // Calculate total words in game
        int totalWords = room.TotalLevels;

        // Build results with XP and stats
        var resultsWithXP = new List<object>();

        // Save match history to database first
        if (room.Players.Count > 0)
        {
            try
            {
                // Find winner
                var winner = room.Players.Values.OrderByDescending(p => p.Score).First();

                // Create match history
                var match = await this._database.CreateMatchHistoryAsync(
                    room.Code, room.Category, room.LevelDuration,
                    room.TotalLevels, room.NumQuestions, room.HostId);

                // Complete match with winner
                await this._database.CompleteMatchAsync(match.Id, winner.Id);

                // Save player results and update stats
                int rank = 1;
                foreach (var player in room.Players.Values.OrderByDescending(p => p.Score))
                {
                    // Calculate XP gained
                    int xpGained = this.CalculateXPGained(player.Score, rank, player.Id == winner.Id);

                    // Get current user stats
                    var stats = await this._database.GetUserStatsAsync(player.Id);
                    int currentTotalXP = stats?.TotalXP ?? 0;
                    int newTotalXP = currentTotalXP + xpGained;
                    int newLevel = this.CalculateLevelFromXP(newTotalXP);

                    // Save match result with XP
                    await this._database.AddMatchPlayerResultAsync(
                        match.Id, player.Id, player.Score, player.Streak,
                        player.TotalWordsFound, 0, room.GameState?.CurrentLevel ?? 0, rank, xpGained);

                    // Update user stats with XP
                    await this._database.UpdateUserStatsAsync(
                        player.Id, player.Score, player.Streak,
                        player.TotalWordsFound, player.Id == winner.Id, xpGained, newLevel);

                    // Add to results
                    resultsWithXP.Add(new
                    {
                        Id = player.Id.ToString(),
                        Username = player.Username,
                        AvatarUrl = player.AvatarUrl,
                        Score = player.Score,
                        WordsFound = player.TotalWordsFound,
                        TotalWords = totalWords,
                        XPGained = xpGained,
                        TotalXP = newTotalXP,
                        Level = newLevel
                    });

                    rank++;
                }

                Console.WriteLine($"Match history saved for room {room.Code}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving match history: {ex.Message}");
                // Fallback to simple results
                resultsWithXP = room.Players.Values
                    .OrderByDescending(p => p.Score)
                    .Select(p => new
                    {
                        Id = p.Id.ToString(),
                        Username = p.Username,
                        AvatarUrl = p.AvatarUrl,
                        Score = p.Score,
                        WordsFound = p.TotalWordsFound,
                        TotalWords = totalWords,
                        XPGained = 0,
                        TotalXP = 0,
                        Level = 1
                    } as object)
                    .ToList();
            }
        }

        // Broadcast game end with XP data
        await this.BroadcastToRoom(room, new GameMessage
        {
            Type = "GAME_ENDED",
            Data = JsonSerializer.Serialize(new { results = resultsWithXP })
        });

        room.GameState = null;
        foreach (var player in room.Players.Values)
        {
            player.Score = 0;
            player.Streak = 0;
            player.WordsFoundThisLevel = 0;
            player.TotalWordsFound = 0;
        }
    }

    // ============================================
    // Authentication Handlers
    // ============================================

    private async Task HandleRegister(ClientConnection connection, GameMessage message)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<RegisterData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid registration data");
        }

        var result = await this._auth.RegisterAsync(data.Username, data.Email, data.Password);

        if (result.Success && result.User != null && result.Token != null)
        {
            // Get user stats
            var stats = await this._database.GetUserStatsAsync(result.User.Id);

            await connection.SendAsync(new GameMessage
            {
                Type = "REGISTER_SUCCESS",
                Data = JsonSerializer.Serialize(new
                {
                    token = result.Token,
                    user = new
                    {
                        id = result.User.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        displayName = result.User.DisplayName,
                        avatarUrl = result.User.AvatarUrl
                    },
                    stats = stats != null ? new
                    {
                        totalScore = stats.TotalScore,
                        bestScore = stats.BestScore,
                        bestStreak = stats.BestStreak,
                        gamesPlayed = stats.GamesPlayed,
                        gamesWon = stats.GamesWon
                    } : null
                })
            });

            Console.WriteLine($"User registered: {result.User.Username}");
        }
        else
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "REGISTER_FAILED",
                Data = JsonSerializer.Serialize(new { error = result.Error })
            });
        }
    }

    private async Task HandleLogin(ClientConnection connection, GameMessage message)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<LoginData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid login data");
        }

        var result = await this._auth.LoginAsync(data.UsernameOrEmail, data.Password);

        if (result.Success && result.User != null && result.Token != null)
        {
            connection.UserId = result.User.Id;

            // Get user stats
            var stats = await this._database.GetUserStatsAsync(result.User.Id);

            await connection.SendAsync(new GameMessage
            {
                Type = "LOGIN_SUCCESS",
                Data = JsonSerializer.Serialize(new
                {
                    token = result.Token,
                    user = new
                    {
                        id = result.User.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        displayName = result.User.DisplayName,
                        avatarUrl = result.User.AvatarUrl
                    },
                    stats = stats != null ? new
                    {
                        totalScore = stats.TotalScore,
                        bestScore = stats.BestScore,
                        bestStreak = stats.BestStreak,
                        gamesPlayed = stats.GamesPlayed,
                        gamesWon = stats.GamesWon
                    } : null
                })
            });

            Console.WriteLine($"User logged in: {result.User.Username}");
        }
        else
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "LOGIN_FAILED",
                Data = JsonSerializer.Serialize(new { error = result.Error })
            });
        }
    }

    private async Task HandleLogout(ClientConnection connection)
    {
        if (connection.UserId.HasValue)
        {
            await this._auth.LogoutAsync(connection.UserId.Value);
            Console.WriteLine($"User logged out: {connection.UserId}");
        }

        await connection.SendAsync(new GameMessage { Type = "LOGOUT_SUCCESS" });
    }

    private async Task HandleUpdateProfile(ClientConnection connection, GameMessage message)
    {
        if (!connection.UserId.HasValue)
        {
            throw new Exception("Not authenticated");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<UpdateProfileData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid update profile data");
        }

        var result = await this._auth.UpdateProfileAsync(connection.UserId.Value, data.DisplayName, data.AvatarUrl);

        if (result.Success && result.User != null)
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "UPDATE_PROFILE_SUCCESS",
                Data = JsonSerializer.Serialize(new
                {
                    user = new
                    {
                        id = result.User.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        displayName = result.User.DisplayName,
                        avatarUrl = result.User.AvatarUrl
                    }
                })
            });

            Console.WriteLine($"User profile updated: {result.User.Username}");
        }
        else
        {
            await connection.SendAsync(new GameMessage
            {
                Type = "UPDATE_PROFILE_FAILED",
                Data = JsonSerializer.Serialize(new { error = result.Error })
            });
        }
    }

    // ============================================
    // Friends Handlers
    // ============================================

    private async Task HandleGetFriends(ClientConnection connection)
    {
        if (!connection.UserId.HasValue)
        {
            throw new Exception("Not authenticated");
        }

        var friends = await this._database.GetUserFriendsAsync(connection.UserId.Value);

        await connection.SendAsync(new GameMessage
        {
            Type = "FRIENDS_LIST",
            Data = JsonSerializer.Serialize(new
            {
                friends = friends.Select(f => new
                {
                    id = f.Id,
                    username = f.Username,
                    displayName = f.DisplayName,
                    avatarUrl = f.AvatarUrl,
                    isOnline = f.IsOnline
                })
            })
        });
    }

    private async Task HandleAddFriend(ClientConnection connection, GameMessage message)
    {
        if (!connection.UserId.HasValue)
        {
            throw new Exception("Not authenticated");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<AddFriendData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid data");
        }

        // Find friend by username
        var friend = await this._database.GetUserByUsernameAsync(data.Username);
        if (friend == null)
        {
            throw new Exception("User not found");
        }

        if (friend.Id == connection.UserId.Value)
        {
            throw new Exception("Cannot add yourself as friend");
        }

        // Check if friendship already exists
        var existing = await this._database.GetFriendshipAsync(connection.UserId.Value, friend.Id);
        if (existing != null)
        {
            throw new Exception("Friendship already exists");
        }

        // Create friendship request
        var friendship = await this._database.CreateFriendshipRequestAsync(
            connection.UserId.Value, friend.Id);

        await connection.SendAsync(new GameMessage
        {
            Type = "FRIEND_REQUEST_SENT",
            Data = JsonSerializer.Serialize(new { friendshipId = friendship.Id, username = friend.Username })
        });

        Console.WriteLine($"Friend request sent from {connection.UserId} to {friend.Username}");
    }

    private async Task HandleAcceptFriend(ClientConnection connection, GameMessage message)
    {
        if (!connection.UserId.HasValue)
        {
            throw new Exception("Not authenticated");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<AcceptFriendData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid data");
        }

        await this._database.AcceptFriendshipAsync(data.FriendshipId);

        await connection.SendAsync(new GameMessage { Type = "FRIEND_REQUEST_ACCEPTED" });

        Console.WriteLine($"Friend request accepted: {data.FriendshipId}");
    }

    private async Task HandleRemoveFriend(ClientConnection connection, GameMessage message)
    {
        if (!connection.UserId.HasValue)
        {
            throw new Exception("Not authenticated");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<RemoveFriendData>(message.Data, options);

        if (data == null)
        {
            throw new Exception("Invalid data");
        }

        await this._database.DeleteFriendshipAsync(data.FriendshipId);

        await connection.SendAsync(new GameMessage { Type = "FRIEND_REMOVED" });

        Console.WriteLine($"Friendship removed: {data.FriendshipId}");
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

    /// <summary>
    /// Calculates score for a correct answer based on speed and streak
    /// Formula: Base * (Speed factor + Streak multiplier)
    /// This matches the client's CalculateCorrectAnswerScore logic
    /// </summary>
    private int CalculateCorrectAnswerScore(Player player, float timeTaken, float levelDuration = 60f)
    {
        const int BASE_SCORE = 1000;
        const float MIN_SPEED_FACTOR = 0.5f;
        const float MAX_SPEED_FACTOR = 1.0f;
        const float STREAK_BONUS_PER_STREAK = 0.1f;
        const float MAX_STREAK_MULTIPLIER = 1.5f;

        // Calculate speed factor based on time remaining (0.5 to 1.0)
        var timeRemaining = Math.Max(0, levelDuration - timeTaken);
        var percentTimeRemaining = timeRemaining / levelDuration;
        var speedFactor = MIN_SPEED_FACTOR + (MAX_SPEED_FACTOR - MIN_SPEED_FACTOR) * percentTimeRemaining;

        // Calculate streak multiplier (0.1 per streak, max 1.5)
        var streakMultiplier = Math.Min(player.Streak * STREAK_BONUS_PER_STREAK, MAX_STREAK_MULTIPLIER);

        // Calculate total score
        var totalMultiplier = speedFactor + streakMultiplier;
        var scoreGained = (int)Math.Round(BASE_SCORE * totalMultiplier);

        Console.WriteLine($"[Scoring] Player {player.Username}: +{scoreGained} points | Speed: {speedFactor:F2}x | Streak: {player.Streak} ({streakMultiplier:F2}x) | Time: {timeTaken:F1}s");

        return scoreGained;
    }

    /// <summary>
    /// Calculate XP gained from a match based on performance
    /// XP Formula: Base XP (100) + Score bonus (Score / 50) + Win bonus (500)
    /// </summary>
    private int CalculateXPGained(int score, int rank, bool isWinner)
    {
        const int BASE_XP = 100;
        const int SCORE_TO_XP_RATIO = 50; // 1 XP per 50 score
        const int WIN_BONUS = 500;
        const int SECOND_PLACE_BONUS = 300;
        const int THIRD_PLACE_BONUS = 150;

        int xpGained = BASE_XP;

        // Score bonus
        xpGained += score / SCORE_TO_XP_RATIO;

        // Rank bonuses
        if (isWinner || rank == 1)
        {
            xpGained += WIN_BONUS;
        }
        else if (rank == 2)
        {
            xpGained += SECOND_PLACE_BONUS;
        }
        else if (rank == 3)
        {
            xpGained += THIRD_PLACE_BONUS;
        }

        return xpGained;
    }

    /// <summary>
    /// Calculate player level from total XP
    /// Level Formula: Level = floor(sqrt(TotalXP / 100))
    /// </summary>
    private int CalculateLevelFromXP(int totalXP)
    {
        return (int)Math.Floor(Math.Sqrt(totalXP / 100.0)) + 1;
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
    public Guid? UserId { get; set; } // Added for authentication
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
    public int LevelDuration { get; set; } = 30;
    public int TotalLevels { get; set; } = 10;
    public int NumQuestions { get; set; } = 10;
    public ConcurrentDictionary<Guid, Player> Players { get; } = new();
    public GameState? GameState { get; set; }
}

public class Player
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? RoomCode { get; set; }
    public int Score { get; set; }
    public int Streak { get; set; }
    public int WordsFoundThisLevel { get; set; }
    public int TotalWordsFound { get; set; }
}

public class GameState
{
    public int CurrentLevel { get; set; }
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
    public int LevelDuration { get; set; } = 30;
    public int NumQuestions { get; set; } = 10;
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

public class RegisterData
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginData
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileData
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class AddFriendData
{
    public string Username { get; set; } = string.Empty;
}

public class AcceptFriendData
{
    public Guid FriendshipId { get; set; }
}

public class RemoveFriendData
{
    public Guid FriendshipId { get; set; }
}
