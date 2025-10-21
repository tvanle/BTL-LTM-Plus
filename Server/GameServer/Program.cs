using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WordBrainServer;
using WordBrainServer.Data;

Console.WriteLine("WordBrain Game Server");
Console.WriteLine("=====================");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var port = args.Length > 0 && int.TryParse(args[0], out var p)
    ? p
    : configuration.GetValue("Server:Port", 8080);

var connectionString = configuration.GetConnectionString("GameDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'GameDb' is missing. Please update appsettings.json or provide it via environment variables.");
}

var optionsBuilder = new DbContextOptionsBuilder<GameServerDbContext>();
optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

using var dbContextFactory = new GameServerDbContextFactory(optionsBuilder.Options);
var server = new WordBrainServer.GameServer(dbContextFactory, port);

var cts = new CancellationTokenSource();

Console.WriteLine($"Server starting on port {port}...");
Console.WriteLine("Press Ctrl+C to quit the server...");

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down server...");
    cts.Cancel();
};

await server.StartAsync();

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
}

await server.StopAsync();
Console.WriteLine("Server shutdown complete.");