using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WordBrainServer.Data;

public class DesignTimeGameServerDbContextFactory : IDesignTimeDbContextFactory<GameServerDbContext>
{
    public GameServerDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("GameDb")
                               ?? throw new InvalidOperationException("Connection string 'GameDb' not found for design-time services.");

        var optionsBuilder = new DbContextOptionsBuilder<GameServerDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new GameServerDbContext(optionsBuilder.Options);
    }
}
