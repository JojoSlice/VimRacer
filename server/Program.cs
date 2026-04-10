using VimRacerServer;

const int Port = 7777;
var server = new RelayServer(Port);
Console.WriteLine($"VimRacer relay server listening on :{Port}");
Console.WriteLine("Press Ctrl+C to stop.");

while (true)
{
    server.Poll();
    Thread.Sleep(15);
}
