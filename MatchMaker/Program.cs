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
            Thread.Sleep(200);
            mm.OnUpdate();
        }
    }
}