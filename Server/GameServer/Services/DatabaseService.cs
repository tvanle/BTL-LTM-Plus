using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using GameServer.Models;
using System.Data;

namespace GameServer.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    private SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    private void InitializeDatabase()
    {
        using var conn = GetConnection();
        conn.Open();

        // Try to find schema file in multiple locations
        string? schemaPath = FindSchemaFile();

        if (schemaPath == null)
        {
            Console.WriteLine("schema_sqlite.sql not found, creating new file...");
            schemaPath = CreateSchemaFile();
        }

        if (schemaPath != null && File.Exists(schemaPath))
        {
            Console.WriteLine($"Loading schema from: {schemaPath}");
            var schema = File.ReadAllText(schemaPath);
            using var cmd = new SqliteCommand(schema, conn);
            cmd.ExecuteNonQuery();
            Console.WriteLine("Database initialized successfully");
        }
        else
        {
            Console.WriteLine("ERROR: Could not find or create schema file!");
        }
    }

    private string? FindSchemaFile()
    {
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            // Relative to executable
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema_sqlite.sql"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Database", "schema_sqlite.sql"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Database", "schema_sqlite.sql"),

            // Absolute path from project root
            Path.Combine(Directory.GetCurrentDirectory(), "Database", "schema_sqlite.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Database", "schema_sqlite.sql"),

            // Common server locations
            @"C:\Users\PC\Documents\Free\BTL-LTM-Plus\Server\Database\schema_sqlite.sql"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch { }
        }

        return null;
    }

    private string CreateSchemaFile()
    {
        var schemaContent = @"-- ============================================
-- Word Game Database Schema - SQLite
-- ============================================

-- Users Table
CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    display_name TEXT,
    avatar_url TEXT,
    is_online INTEGER DEFAULT 0,
    last_login_at TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    CHECK (LENGTH(username) >= 3)
);

CREATE INDEX IF NOT EXISTS idx_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_is_online ON users(is_online);

-- User Statistics Table
CREATE TABLE IF NOT EXISTS user_stats (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    total_score INTEGER DEFAULT 0,
    best_score INTEGER DEFAULT 0,
    best_streak INTEGER DEFAULT 0,
    games_played INTEGER DEFAULT 0,
    games_won INTEGER DEFAULT 0,
    total_words_found INTEGER DEFAULT 0,
    average_completion_time REAL DEFAULT 0,
    rank_position INTEGER DEFAULT 0,
    total_xp INTEGER DEFAULT 0,
    level INTEGER DEFAULT 1,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_id ON user_stats(user_id);
CREATE INDEX IF NOT EXISTS idx_rank ON user_stats(rank_position);
CREATE INDEX IF NOT EXISTS idx_total_score ON user_stats(total_score DESC);

-- Friendships Table (bidirectional)
CREATE TABLE IF NOT EXISTS friendships (
    id TEXT PRIMARY KEY,
    user_id1 TEXT NOT NULL,
    user_id2 TEXT NOT NULL,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'accepted', 'blocked')),
    requester_id TEXT NOT NULL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    accepted_at TEXT,
    FOREIGN KEY (user_id1) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id2) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (requester_id) REFERENCES users(id) ON DELETE CASCADE,
    CHECK (user_id1 != user_id2),
    UNIQUE(user_id1, user_id2)
);

CREATE INDEX IF NOT EXISTS idx_user_id1 ON friendships(user_id1);
CREATE INDEX IF NOT EXISTS idx_user_id2 ON friendships(user_id2);
CREATE INDEX IF NOT EXISTS idx_status ON friendships(status);
CREATE INDEX IF NOT EXISTS idx_requester ON friendships(requester_id);

-- Game Invitations Table
CREATE TABLE IF NOT EXISTS game_invitations (
    id TEXT PRIMARY KEY,
    sender_id TEXT NOT NULL,
    receiver_id TEXT NOT NULL,
    room_code TEXT,
    category TEXT,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'accepted', 'declined', 'expired')),
    expires_at TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    responded_at TEXT,
    FOREIGN KEY (sender_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (receiver_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_receiver_id ON game_invitations(receiver_id);
CREATE INDEX IF NOT EXISTS idx_sender_id ON game_invitations(sender_id);
CREATE INDEX IF NOT EXISTS idx_inv_status ON game_invitations(status);
CREATE INDEX IF NOT EXISTS idx_expires_at ON game_invitations(expires_at);

-- Match History Table
CREATE TABLE IF NOT EXISTS match_history (
    id TEXT PRIMARY KEY,
    room_code TEXT NOT NULL,
    category TEXT NOT NULL,
    level_duration INTEGER NOT NULL,
    total_levels INTEGER NOT NULL,
    num_questions INTEGER NOT NULL,
    host_id TEXT NOT NULL,
    winner_id TEXT,
    started_at TEXT DEFAULT CURRENT_TIMESTAMP,
    completed_at TEXT,
    total_duration_seconds INTEGER,
    FOREIGN KEY (host_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (winner_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_host_id ON match_history(host_id);
CREATE INDEX IF NOT EXISTS idx_winner_id ON match_history(winner_id);
CREATE INDEX IF NOT EXISTS idx_started_at ON match_history(started_at DESC);
CREATE INDEX IF NOT EXISTS idx_room_code ON match_history(room_code);

-- Match Player Results Table
CREATE TABLE IF NOT EXISTS match_player_results (
    id TEXT PRIMARY KEY,
    match_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    final_score INTEGER DEFAULT 0,
    best_streak INTEGER DEFAULT 0,
    total_words_found INTEGER DEFAULT 0,
    average_time_per_level REAL DEFAULT 0,
    completed_levels INTEGER DEFAULT 0,
    rank_position INTEGER DEFAULT 0,
    xp_gained INTEGER DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES match_history(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_match_id ON match_player_results(match_id);
CREATE INDEX IF NOT EXISTS idx_mpr_user_id ON match_player_results(user_id);
CREATE INDEX IF NOT EXISTS idx_final_score ON match_player_results(final_score DESC);

-- Session Tokens Table (for authentication)
CREATE TABLE IF NOT EXISTS session_tokens (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    token TEXT NOT NULL UNIQUE,
    device_info TEXT,
    ip_address TEXT,
    expires_at TEXT NOT NULL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    last_used_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_st_user_id ON session_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_token ON session_tokens(token);
CREATE INDEX IF NOT EXISTS idx_st_expires_at ON session_tokens(expires_at);
";

        // Try to create in Database folder
        var targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema_sqlite.sql");

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(targetPath, schemaContent);
            Console.WriteLine($"Created schema file at: {targetPath}");
            return targetPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create schema file: {ex.Message}");
            return null!;
        }
    }

    // ============================================
    // User Management
    // ============================================

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand(
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

        var cmd = new SqliteCommand(
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

        var cmd = new SqliteCommand(
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

        var cmd = new SqliteCommand(
            @"INSERT INTO users (id, username, email, password_hash, created_at, updated_at)
              VALUES (@id, @username, @email, @passwordHash, @createdAt, @updatedAt)", conn);
        cmd.Parameters.AddWithValue("@id", userId.ToString());
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@createdAt", now.ToString("o"));
        cmd.Parameters.AddWithValue("@updatedAt", now.ToString("o"));

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

        var cmd = new SqliteCommand(
            @"UPDATE users SET is_online = @isOnline, last_login_at = @lastLogin
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@isOnline", isOnline ? 1 : 0);
        cmd.Parameters.AddWithValue("@lastLogin", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", userId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserProfileAsync(Guid userId, string? displayName, string? avatarUrl)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand(
            @"UPDATE users SET display_name = @displayName, avatar_url = @avatarUrl, updated_at = @updatedAt
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@displayName", displayName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@avatarUrl", avatarUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
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

        var cmd = new SqliteCommand(
            @"INSERT INTO session_tokens
              (id, user_id, token, device_info, ip_address, expires_at, created_at, last_used_at)
              VALUES (@id, @userId, @token, @deviceInfo, @ipAddress, @expiresAt, @createdAt, @lastUsedAt)", conn);
        cmd.Parameters.AddWithValue("@id", sessionId.ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@deviceInfo", deviceInfo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ipAddress", ipAddress ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("o"));
        cmd.Parameters.AddWithValue("@createdAt", now.ToString("o"));
        cmd.Parameters.AddWithValue("@lastUsedAt", now.ToString("o"));

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

        var cmd = new SqliteCommand(
            @"SELECT * FROM session_tokens
              WHERE token = @token AND expires_at > @now", conn);
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));

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

        var cmd = new SqliteCommand(
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

        var cmd = new SqliteCommand(
            @"INSERT INTO user_stats (id, user_id) VALUES (@id, @userId)", conn);
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserStatsAsync(Guid userId, int score, int streak, int wordsFound, bool isWinner, int xpGained = 0, int newLevel = 1)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand(
            @"UPDATE user_stats SET
              total_score = total_score + @score,
              best_score = GREATEST(best_score, @score),
              best_streak = GREATEST(best_streak, @streak),
              games_played = games_played + 1,
              games_won = games_won + @won,
              total_words_found = total_words_found + @words,
              total_xp = total_xp + @xp,
              level = @level
              WHERE user_id = @userId", conn);
        cmd.Parameters.AddWithValue("@score", score);
        cmd.Parameters.AddWithValue("@streak", streak);
        cmd.Parameters.AddWithValue("@won", isWinner ? 1 : 0);
        cmd.Parameters.AddWithValue("@words", wordsFound);
        cmd.Parameters.AddWithValue("@xp", xpGained);
        cmd.Parameters.AddWithValue("@level", newLevel);
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

        var cmd = new SqliteCommand(
            @"SELECT u.* FROM users u
              INNER JOIN friendships f ON (
                  CASE
                      WHEN f.user_id1 = @userId THEN f.user_id2
                      ELSE f.user_id1
                  END = u.id
              )
              WHERE (f.user_id1 = @userId OR f.user_id2 = @userId)
              AND f.status = 'accepted'
              ORDER BY u.is_online DESC, u.username ASC", conn);
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

        var cmd = new SqliteCommand(
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

        var cmd = new SqliteCommand(
            @"INSERT INTO friendships (id, user_id1, user_id2, status, requester_id, created_at)
              VALUES (@id, @user1, @user2, 'pending', @requester, @createdAt)", conn);
        cmd.Parameters.AddWithValue("@id", friendshipId.ToString());
        cmd.Parameters.AddWithValue("@user1", requesterId.ToString());
        cmd.Parameters.AddWithValue("@user2", receiverId.ToString());
        cmd.Parameters.AddWithValue("@requester", requesterId.ToString());
        cmd.Parameters.AddWithValue("@createdAt", now.ToString("o"));

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

        var cmd = new SqliteCommand(
            @"UPDATE friendships SET status = 'accepted', accepted_at = @acceptedAt
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@acceptedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", friendshipId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFriendshipAsync(Guid friendshipId)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand("DELETE FROM friendships WHERE id = @id", conn);
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

        var cmd = new SqliteCommand(
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
        cmd.Parameters.AddWithValue("@startedAt", now.ToString("o"));

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

        var cmd = new SqliteCommand(
            @"UPDATE match_history SET
              winner_id = @winnerId,
              completed_at = @completedAt
              WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@winnerId", winnerId?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@completedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", matchId.ToString());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddMatchPlayerResultAsync(
        Guid matchId, Guid userId, int finalScore, int bestStreak,
        int totalWordsFound, float avgTime, int completedLevels, int rankPosition, int xpGained = 0)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand(
            @"INSERT INTO match_player_results
              (id, match_id, user_id, final_score, best_streak, total_words_found,
               average_time_per_level, completed_levels, rank_position, xp_gained)
              VALUES (@id, @matchId, @userId, @score, @streak, @words, @avgTime, @completed, @rank, @xp)", conn);
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@matchId", matchId.ToString());
        cmd.Parameters.AddWithValue("@userId", userId.ToString());
        cmd.Parameters.AddWithValue("@score", finalScore);
        cmd.Parameters.AddWithValue("@streak", bestStreak);
        cmd.Parameters.AddWithValue("@words", totalWordsFound);
        cmd.Parameters.AddWithValue("@avgTime", avgTime);
        cmd.Parameters.AddWithValue("@completed", completedLevels);
        cmd.Parameters.AddWithValue("@rank", rankPosition);
        cmd.Parameters.AddWithValue("@xp", xpGained);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<UserMatchHistoryDto>> GetUserMatchHistoryAsync(Guid userId, int limit = 20)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var cmd = new SqliteCommand(
            @"SELECT
                mh.id as match_id,
                mh.category,
                mh.completed_at,
                mpr.final_score,
                mpr.rank_position,
                mpr.xp_gained,
                mh.winner_id
              FROM match_player_results mpr
              INNER JOIN match_history mh ON mpr.match_id = mh.id
              WHERE mpr.user_id = @userId
              AND mh.completed_at IS NOT NULL
              ORDER BY mh.completed_at DESC
              LIMIT @limit", conn);
        cmd.Parameters.AddWithValue("@userId", userId.ToString());
        cmd.Parameters.AddWithValue("@limit", limit);

        var history = new List<UserMatchHistoryDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var winnerId = reader.IsDBNull(reader.GetOrdinal("winner_id"))
                ? (Guid?)null
                : Guid.Parse(reader.GetString(reader.GetOrdinal("winner_id")));

            history.Add(new UserMatchHistoryDto
            {
                MatchId = Guid.Parse(reader.GetString(reader.GetOrdinal("match_id"))),
                Category = reader.GetString(reader.GetOrdinal("category")),
                CompletedAt = reader.GetString(reader.GetOrdinal("completed_at")),
                FinalScore = reader.GetInt32(reader.GetOrdinal("final_score")),
                Rank = reader.GetInt32(reader.GetOrdinal("rank_position")),
                XPGained = reader.GetInt32(reader.GetOrdinal("xp_gained")),
                IsWinner = winnerId.HasValue && winnerId.Value == userId
            });
        }

        return history;
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
            IsOnline = reader.GetInt32(reader.GetOrdinal("is_online")) == 1,
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_login_at"))),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
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
            RankPosition = reader.GetInt32(reader.GetOrdinal("rank_position")),
            TotalXP = reader.IsDBNull(reader.GetOrdinal("total_xp")) ? 0 : reader.GetInt32(reader.GetOrdinal("total_xp")),
            Level = reader.IsDBNull(reader.GetOrdinal("level")) ? 1 : reader.GetInt32(reader.GetOrdinal("level"))
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
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            AcceptedAt = reader.IsDBNull(reader.GetOrdinal("accepted_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("accepted_at")))
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
            ExpiresAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("expires_at"))),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            LastUsedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("last_used_at")))
        };
    }
}
