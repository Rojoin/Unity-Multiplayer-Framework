namespace Server;

class Program
{
    static void Main(string[] args)
    {
        int newPort = int.Parse(args[0]);
        Server currentServer;
        Console.WriteLine("Open Server, World!");
        bool isServerOn = true;
        currentServer = new Server(ref isServerOn,newPort);
        int millisecondsTimeout = 100;
        while (isServerOn)
        {
            Thread.Sleep(millisecondsTimeout);
            currentServer.OnUpdate(millisecondsTimeout, ref isServerOn);
        }
        currentServer = null;
    }
}