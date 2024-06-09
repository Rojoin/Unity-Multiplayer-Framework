using RojoinNetworkSystem;

namespace MatchMaker;

class Program
{
    static void Main(string[] args)
    {
        MatchMaker mm = new MatchMaker();
        bool isMatchMakerOn = true;
        while (isMatchMakerOn)
        {
            mm.OnUpdate();
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                isMatchMakerOn = false;
            }
        }
    }
}