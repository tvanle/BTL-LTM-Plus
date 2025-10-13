namespace WordBrainServer.Data.Entities;

public class PlayerEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? RoomCode { get; set; }
    public bool IsReady { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GameRoomEntity? Room { get; set; }
}
