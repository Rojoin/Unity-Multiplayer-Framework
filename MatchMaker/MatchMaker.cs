using System.Diagnostics;
using System.Net;
using RojoinNetworkSystem;

namespace MatchMaker;

class MatchMaker : IReceiveData
{
    public IPAddress ipAddress { get; set; }
    public UdpConnection connection;

    public int port { get; set; } = 12345;
    private List<ServerInfo> activeServers = new List<ServerInfo>();

    //Cuanta gente tiene
    //Quienes tiene
    //Estado del juego
    //port
    //Todo: Fijarme si hay server
    //Sino hay server y hay minimo 2 personas
    //Creo server
    //Si ya hay server
    //Fijarme si esta lleno
    //Fijarme si nombre
    //etc
    //Si el juego no esta andando
    //crear un nuevo server
    public MatchMaker()
    {
        connection = new UdpConnection(port, OnError, this);
    }

    private void OnError(string obj)
    {
       
    }
    ~MatchMaker()
    {
        connection.Close();
        foreach (ServerInfo server in activeServers)
        {
            server.CloseProcess();
        }
    }

    public void OnReceiveData(byte[] data, IPEndPoint ipEndpoint)
    {
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);


        bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
        bool isImportant = flags.HasFlag(MessageFlags.Important);
        bool isOrdenable = flags.HasFlag(MessageFlags.Ordenable);
        ulong getMessageID = 0;

        if (type == MessageType.HandShake)
        {
            if (activeServers.Count == 0)
            {
                activeServers.Add(new ServerInfo(DateTime.UtcNow, 12346));
                NetServerDirection netServerDirection = new(("127.0.0.1", activeServers[0].port));
                connection.Send(netServerDirection.Serialize(), ipEndpoint);
            }
            else
            {
                NetServerDirection netServerDirection = new(("127.0.0.1", activeServers[0].port));
                connection.Send(netServerDirection.Serialize(), ipEndpoint);
            }
        }
    }

    public void OnUpdate()
    {
        if (connection != null)
        {
            connection.FlushReceiveData();
        }
    }
}

class ServerInfo
{
    private static uint _serverId = 0;
    private int _currentPlayers;
    private DateTime _startTime;
    public int port;
    private Process _serverProcess;
    private GameState _gameState;

    public ServerInfo(DateTime startTime, int port)
    {
        _gameState = GameState.WaitingForPlayers;
        _startTime = startTime;
        this.port = port;
        _serverId++;
        _serverProcess = new Process();

        _serverProcess.StartInfo.FileName = "Server.exe";
        _serverProcess.StartInfo.Arguments = $"{port}";
        _serverProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        _serverProcess.Start();
    }
    
    public void CloseProcess()
    {
        _serverProcess.Close();
    }
}