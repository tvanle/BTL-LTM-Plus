using WordBrainServer;

Console.WriteLine("WordBrain Game Server");
Console.WriteLine("=====================");

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var server = new GameServer(port);

await server.StartAsync();

Console.WriteLine("Press 'Q' to quit the server...");

while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Q)
    {
        break;
    }
}

await server.StopAsync();
Console.WriteLine("Server shutdown complete.");