using Microsoft.EntityFrameworkCore;
using WordBrainServer.Data.Entities;

namespace WordBrainServer.Data;

public class GameServerDbContext : DbContext
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options) : base(options)
    {
    }

    public DbSet<GameRoomEntity> Rooms => Set<GameRoomEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<PlayerScoreEntity> PlayerScores => Set<PlayerScoreEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameRoomEntity>()
            .HasKey(r => r.Code);

        modelBuilder.Entity<GameRoomEntity>()
            .HasMany(r => r.Players)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomCode)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerEntity>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<PlayerScoreEntity>()
            .HasKey(ps => new { ps.PlayerId, ps.Level });

        modelBuilder.Entity<PlayerScoreEntity>()
            .HasOne(ps => ps.Player)
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId);
    }
}
