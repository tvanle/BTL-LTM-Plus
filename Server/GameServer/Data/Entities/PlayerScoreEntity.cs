namespace WordBrainServer.Data.Entities;

public class PlayerScoreEntity
{
    public Guid PlayerId { get; set; }
    public int Level { get; set; }
    public int Score { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public PlayerEntity? Player { get; set; }
}
