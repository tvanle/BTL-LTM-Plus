using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WordBrainServer.Data;

public sealed class GameServerDbContextFactory : IDbContextFactory<GameServerDbContext>, IDisposable
{
    private readonly DbContextOptions<GameServerDbContext> _options;

    public GameServerDbContextFactory(DbContextOptions<GameServerDbContext> options)
    {
        _options = options;
    }

    public GameServerDbContext CreateDbContext()
        => new GameServerDbContext(_options);

    public ValueTask<GameServerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new GameServerDbContext(_options));

    public void Dispose()
    {
        // No unmanaged resources to release. Method provided for symmetry with using statements.
    }
}
