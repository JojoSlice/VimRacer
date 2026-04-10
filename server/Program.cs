using VimRacerServer;

const int Port = 7777;
var db     = new UserDatabase("vimracer.db");
var server = new RelayServer(Port, db);
Console.WriteLine($"VimRacer relay server listening on :{Port}");
Console.WriteLine("Press Ctrl+C to stop.");

while (true)
{
    server.Poll();
    Thread.Sleep(15);
}
