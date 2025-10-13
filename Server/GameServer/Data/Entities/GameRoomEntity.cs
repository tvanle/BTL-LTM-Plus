using System.Collections.Generic;

namespace WordBrainServer.Data.Entities;

public class GameRoomEntity
{
    public string Code { get; set; } = null!;
    public Guid HostId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int LevelDuration { get; set; }
    public int TotalLevels { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlayerEntity> Players { get; set; } = new List<PlayerEntity>();
}
