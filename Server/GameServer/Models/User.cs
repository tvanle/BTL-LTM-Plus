using System;

namespace GameServer.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserStats
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TotalScore { get; set; }
    public int BestScore { get; set; }
    public int BestStreak { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int TotalWordsFound { get; set; }
    public float AverageCompletionTime { get; set; }
    public int RankPosition { get; set; }
    public int TotalXP { get; set; }
}

public class GameInvitation
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string? RoomCode { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "pending"; // pending, accepted, declined, expired
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class MatchHistory
{
    public Guid Id { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int LevelDuration { get; set; }
    public int TotalLevels { get; set; }
    public int NumQuestions { get; set; }
    public Guid HostId { get; set; }
    public Guid? WinnerId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TotalDurationSeconds { get; set; }
}

public class MatchPlayerResult
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
    public int FinalScore { get; set; }
    public int BestStreak { get; set; }
    public int TotalWordsFound { get; set; }
    public float AverageTimePerLevel { get; set; }
    public int CompletedLevels { get; set; }
    public int RankPosition { get; set; }
    public int XPGained { get; set; }
}

public class SessionToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
}

public class UserMatchHistoryDto
{
    public Guid MatchId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CompletedAt { get; set; } = string.Empty;
    public int FinalScore { get; set; }
    public int Rank { get; set; }
    public int XPGained { get; set; }
    public bool IsWinner { get; set; }
}

public class LeaderboardEntryDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int TotalXP { get; set; }
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int Rank { get; set; }
}
