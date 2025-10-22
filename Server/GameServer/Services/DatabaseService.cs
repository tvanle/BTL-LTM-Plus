using MySql.Data.MySqlClient;
using GameServer.Models;
using System.Data;

namespace GameServer.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    // ============================================
    // User Management
    // ============================================

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT * FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", userId.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT * FROM users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("@username", username);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT * FROM users WHERE email = @email", conn);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User> CreateUserAsync(string username, string email, string passwordHash)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cmd = new MySqlCommand(
            @"INSERT INTO users (id, username, email, password_hash, created_at, updated_at)
              VALUES (@id, @username, @email, @passwordHash, @createdAt, @updatedAt)", conn);
        cmd.Parameters.AddWithValue("@id", userId.ToString());
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@createdAt", now);
        cmd.Parameters.AddWithValue("@updatedAt", now);

        await cmd.ExecuteNonQueryAsync();

        // Create initial user stats
        await CreateUserStatsAsync(userId);

        return new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task UpdateUserOnlineStatusAsync(Guid userId, bool isOnline)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"UPDATE users SET is_online = @isOnline, last_login_at = @lastLogin
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@isOnline", isOnline);
        cmd.Parameters.AddWithValue("@lastLogin", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", userId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // Session Tokens
    // ============================================

    public async Task<SessionToken> CreateSessionTokenAsync(
        Guid userId, string token, string? deviceInfo, string? ipAddress)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(30); // Token valid for 30 days

        var cmd = new MySqlCommand(
            @"INSERT INTO session_tokens
              (id, user_id, token, device_info, ip_address, expires_at, created_at, last_used_at)
              VALUES (@id, @userId, @token, @deviceInfo, @ipAddress, @expiresAt, @createdAt, @lastUsedAt)", conn);
        cmd.Parameters.AddWithValue("@id", sessionId.ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@deviceInfo", deviceInfo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ipAddress", ipAddress ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
        cmd.Parameters.AddWithValue("@createdAt", now);
        cmd.Parameters.AddWithValue("@lastUsedAt", now);

        await cmd.ExecuteNonQueryAsync();

        return new SessionToken
        {
            Id = sessionId,
            UserId = userId,
            Token = token,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            LastUsedAt = now
        };
    }

    public async Task<SessionToken?> GetSessionTokenAsync(string token)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"SELECT * FROM session_tokens
              WHERE token = @token AND expires_at > @now", conn);
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapSessionToken(reader);
        }
        return null;
    }

    // ============================================
    // User Stats
    // ============================================

    public async Task<UserStats?> GetUserStatsAsync(Guid userId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "SELECT * FROM user_stats WHERE user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUserStats(reader);
        }
        return null;
    }

    private async Task CreateUserStatsAsync(Guid userId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"INSERT INTO user_stats (id, user_id) VALUES (@id, @userId)", conn);
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserStatsAsync(Guid userId, int score, int streak, int wordsFound, bool isWinner)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"UPDATE user_stats SET
              total_score = total_score + @score,
              best_score = GREATEST(best_score, @score),
              best_streak = GREATEST(best_streak, @streak),
              games_played = games_played + 1,
              games_won = games_won + @won,
              total_words_found = total_words_found + @words
              WHERE user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@score", score);
        cmd.Parameters.AddWithValue("@streak", streak);
        cmd.Parameters.AddWithValue("@won", isWinner ? 1 : 0);
        cmd.Parameters.AddWithValue("@words", wordsFound);
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // Friendships
    // ============================================

    public async Task<List<User>> GetUserFriendsAsync(Guid userId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand("CALL GetUserFriends(@userId)", conn);
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        var friends = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            friends.Add(MapUser(reader));
        }
        return friends;
    }

    public async Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"SELECT * FROM friendships
              WHERE (user_id1 = @user1 AND user_id2 = @user2)
                 OR (user_id1 = @user2 AND user_id2 = @user1)", conn);
        cmd.Parameters.AddWithValue("@user1", user1Id.ToString());
        cmd.Parameters.AddWithValue("@user2", user2Id.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFriendship(reader);
        }
        return null;
    }

    public async Task<Friendship> CreateFriendshipRequestAsync(Guid requesterId, Guid receiverId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var friendshipId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cmd = new MySqlCommand(
            @"INSERT INTO friendships (id, user_id1, user_id2, status, requester_id, created_at)
              VALUES (@id, @user1, @user2, 'pending', @requester, @createdAt)", conn);
        cmd.Parameters.AddWithValue("@id", friendshipId.ToString());
        cmd.Parameters.AddWithValue("@user1", requesterId.ToString());
        cmd.Parameters.AddWithValue("@user2", receiverId.ToString());
        cmd.Parameters.AddWithValue("@requester", requesterId.ToString());
        cmd.Parameters.AddWithValue("@createdAt", now);

        await cmd.ExecuteNonQueryAsync();

        return new Friendship
        {
            Id = friendshipId,
            UserId1 = requesterId,
            UserId2 = receiverId,
            Status = "pending",
            RequesterId = requesterId,
            CreatedAt = now
        };
    }

    public async Task AcceptFriendshipAsync(Guid friendshipId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"UPDATE friendships SET status = 'accepted', accepted_at = @acceptedAt
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@acceptedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", friendshipId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFriendshipAsync(Guid friendshipId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand("DELETE FROM friendships WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", friendshipId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // Match History
    // ============================================

    public async Task<MatchHistory> CreateMatchHistoryAsync(
        string roomCode, string category, int levelDuration,
        int totalLevels, int numQuestions, Guid hostId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var matchId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var cmd = new MySqlCommand(
            @"INSERT INTO match_history
              (id, room_code, category, level_duration, total_levels, num_questions, host_id, started_at)
              VALUES (@id, @roomCode, @category, @levelDuration, @totalLevels, @numQuestions, @hostId, @startedAt)", conn);
        cmd.Parameters.AddWithValue("@id", matchId.ToString());
        cmd.Parameters.AddWithValue("@roomCode", roomCode);
        cmd.Parameters.AddWithValue("@category", category);
        cmd.Parameters.AddWithValue("@levelDuration", levelDuration);
        cmd.Parameters.AddWithValue("@totalLevels", totalLevels);
        cmd.Parameters.AddWithValue("@numQuestions", numQuestions);
        cmd.Parameters.AddWithValue("@hostId", hostId.ToString());
        cmd.Parameters.AddWithValue("@startedAt", now);

        await cmd.ExecuteNonQueryAsync();

        return new MatchHistory
        {
            Id = matchId,
            RoomCode = roomCode,
            Category = category,
            LevelDuration = levelDuration,
            TotalLevels = totalLevels,
            NumQuestions = numQuestions,
            HostId = hostId,
            StartedAt = now
        };
    }

    public async Task CompleteMatchAsync(Guid matchId, Guid? winnerId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"UPDATE match_history SET
              winner_id = @winnerId,
              completed_at = @completedAt
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@winnerId", winnerId?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@completedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", matchId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddMatchPlayerResultAsync(
        Guid matchId, Guid userId, int finalScore, int bestStreak,
        int totalWordsFound, float avgTime, int completedLevels, int rankPosition)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            @"INSERT INTO match_player_results
              (id, match_id, user_id, final_score, best_streak, total_words_found,
               average_time_per_level, completed_levels, rank_position)
              VALUES (@id, @matchId, @userId, @score, @streak, @words, @avgTime, @completed, @rank)", conn);
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@matchId", matchId.ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());
        cmd.Parameters.AddWithValue("@score", finalScore);
        cmd.Parameters.AddWithValue("@streak", bestStreak);
        cmd.Parameters.AddWithValue("@words", totalWordsFound);
        cmd.Parameters.AddWithValue("@avgTime", avgTime);
        cmd.Parameters.AddWithValue("@completed", completedLevels);
        cmd.Parameters.AddWithValue("@rank", rankPosition);

        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // Helper Mappers
    // ============================================

    private User MapUser(IDataReader reader)
    {
        return new User
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Username = reader.GetString(reader.GetOrdinal("username")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            DisplayName = reader.IsDBNull(reader.GetOrdinal("display_name")) ? null : reader.GetString(reader.GetOrdinal("display_name")),
            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString(reader.GetOrdinal("avatar_url")),
            IsOnline = reader.GetBoolean(reader.GetOrdinal("is_online")),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private UserStats MapUserStats(IDataReader reader)
    {
        return new UserStats
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            UserId = Guid.Parse(reader.GetString(reader.GetOrdinal("user_id"))),
            TotalScore = reader.GetInt32(reader.GetOrdinal("total_score")),
            BestScore = reader.GetInt32(reader.GetOrdinal("best_score")),
            BestStreak = reader.GetInt32(reader.GetOrdinal("best_streak")),
            GamesPlayed = reader.GetInt32(reader.GetOrdinal("games_played")),
            GamesWon = reader.GetInt32(reader.GetOrdinal("games_won")),
            TotalWordsFound = reader.GetInt32(reader.GetOrdinal("total_words_found")),
            AverageCompletionTime = reader.GetFloat(reader.GetOrdinal("average_completion_time")),
            RankPosition = reader.GetInt32(reader.GetOrdinal("rank_position"))
        };
    }

    private Friendship MapFriendship(IDataReader reader)
    {
        return new Friendship
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            UserId1 = Guid.Parse(reader.GetString(reader.GetOrdinal("user_id1"))),
            UserId2 = Guid.Parse(reader.GetString(reader.GetOrdinal("user_id2"))),
            Status = reader.GetString(reader.GetOrdinal("status")),
            RequesterId = Guid.Parse(reader.GetString(reader.GetOrdinal("requester_id"))),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            AcceptedAt = reader.IsDBNull(reader.GetOrdinal("accepted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("accepted_at"))
        };
    }

    private SessionToken MapSessionToken(IDataReader reader)
    {
        return new SessionToken
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            UserId = Guid.Parse(reader.GetString(reader.GetOrdinal("user_id"))),
            Token = reader.GetString(reader.GetOrdinal("token")),
            DeviceInfo = reader.IsDBNull(reader.GetOrdinal("device_info")) ? null : reader.GetString(reader.GetOrdinal("device_info")),
            IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString(reader.GetOrdinal("ip_address")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            LastUsedAt = reader.GetDateTime(reader.GetOrdinal("last_used_at"))
        };
    }
}
