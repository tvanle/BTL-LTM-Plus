using System;
using System.Threading;
using System.Threading.Tasks;
using WordBrainServer;

Console.WriteLine("WordBrain Game Server");
Console.WriteLine("=====================");

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

// Connection string for SQLite (database file will be created automatically)
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "word_game.db");
var connectionString = $"Data Source={dbPath}";

Console.WriteLine($"Database: {dbPath}");

var server = new WordBrainServer.GameServer(port, connectionString);

// Start server in background
var serverTask = Task.Run(async () => await server.StartAsync());

Console.WriteLine($"Server starting on port {port}...");
Console.WriteLine("Press Ctrl+C to quit the server...");

// Use cancellation token for graceful shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down server...");
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // Expected when cancellation is requested
}

await server.StopAsync();
Console.WriteLine("Server shutdown complete.");