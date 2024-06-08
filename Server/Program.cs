namespace Server;

class Program
{
    static void Main(string[] args)
    {
        Server currentServer;
        Console.WriteLine("Open Server, World!");
        bool isServerOn = true;
        currentServer = new Server(ref isServerOn);
        int millisecondsTimeout = 100;
        while (isServerOn)
        {
            Thread.Sleep(millisecondsTimeout);
            currentServer.OnUpdate(millisecondsTimeout, ref isServerOn);
        }
        currentServer = null;
    }
}