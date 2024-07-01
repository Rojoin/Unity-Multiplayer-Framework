using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using RojoinNetworkSystem;

namespace MatchMaker;

class MatchMaker : IReceiveData
{
    public IPAddress ipAddress { get; set; }
    public IPAddress myIP { get; set; }
    public UdpConnection connection;

    public int port { get; set; } = 12345;
    public int portToConnect { get; set; } = 12346;
    private List<ServerInfo> activeServers = new List<ServerInfo>();

    //Cuanta gente tiene
    //Quienes tiene
    //Estado del juego
    //port
  
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


        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                myIP = ip;
                Console.WriteLine(ip);
            }
        }
    }

    private void OnError(string obj)
    {
        Console.WriteLine(obj);
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
        int id = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);


        bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
        bool isImportant = flags.HasFlag(MessageFlags.Important);
        bool isOrdenable = flags.HasFlag(MessageFlags.Ordenable);
        bool isServer = flags.HasFlag(MessageFlags.ServerMessage);
        ulong getMessageID = 0;
        Console.WriteLine($"New Data of type:{type} is from server:{isServer}");
        if (isServer)
        {
            switch (type)
            {
                case MessageType.HandShakeOk:
                    NetHandShakeOK serverHandshake = new NetHandShakeOK();
                    activeServers[id].playersInServer = serverHandshake.DeseliarizeObj(data);
                    activeServers[id]._currentPlayers = activeServers[id].playersInServer.Count;
                    Console.WriteLine($" {activeServers[id].playersInServer.Count}");
                    if (activeServers[id]._currentPlayers == 0)
                    {
                        NetExit exit = new NetExit("CloseServer");
                        activeServers[id].SendToServer(data);
                        Thread.Sleep(500);
                        activeServers[id].CloseProcess();
                        activeServers.RemoveAt(id);
                    }

                    break;
                case MessageType.HandShake:
                    activeServers[id].ep = ipEndpoint;
                    break;
            }
        }
        else if (type == MessageType.HandShake)
        {
            if (activeServers.Count == 0)
            {
                CreateNewServer(ipEndpoint);
            }
            else
            {
                NetHandShake playerData = new NetHandShake();
                string newName = playerData.DeseliarizeObj(data);
                foreach (ServerInfo serverInfo in activeServers)
                {
                    if (serverInfo.CanSendPlayer(newName))
                    {
                        NetServerDirection netServerDirection = new((myIP.ToString(), serverInfo.port));
                        connection.Send(netServerDirection.Serialize(), ipEndpoint);
                        return;
                    }
                }

                CreateNewServer(ipEndpoint);
            }
        }
   
    }

    private void CreateNewServer(IPEndPoint ipEndpoint)
    {
        activeServers.Add(
            new ServerInfo(DateTime.UtcNow, portToConnect + ServerInfo.serverCount, myIP.ToString(), this));
        Thread.Sleep(1500);
        NetServerDirection netServerDirection = new((myIP.ToString(), activeServers[^1].port));
        connection.Send(netServerDirection.Serialize(), ipEndpoint);
    }

    public void OnUpdate()
    {
        if (connection != null)
        {
            connection.FlushReceiveData();
        }

        foreach (ServerInfo activeServer in activeServers)
        {
            activeServer.connection?.FlushReceiveData();
        }
    }
}

class ServerInfo
{
    public static int serverCount = 0;
    private int serverId = 0;
    public int _currentPlayers;
    private int maxPlayers = 2;
    private DateTime _startTime;
    public int port;
    private Process _serverProcess;
    private GameState _gameState;
    private IPAddress _ipAddress;
    public IPEndPoint ep;
    public List<Player> playersInServer = new List<Player>();
    public UdpConnection connection;

    public ServerInfo(DateTime startTime, int port, string serverIp, IReceiveData receiver)
    {
        _gameState = GameState.WaitingForPlayers;
        _startTime = startTime;
        this.port = port;
        serverId = serverCount++;
        _serverProcess = new Process();
        _ipAddress = IPAddress.Parse(serverIp);
        _serverProcess.StartInfo.FileName = "Server.exe";
        _serverProcess.StartInfo.Arguments = $"{port}";
        _serverProcess.StartInfo.UseShellExecute = true;
        _serverProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        _serverProcess.Start();
        Thread.Sleep(500);
        connection = new UdpConnection(_ipAddress, port, OnErrorMessage, serverId, receiver);
    }

    public void CloseProcess()
    {
        _serverProcess.Close();
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    private void OnErrorMessage(string errorMessage)
    {
        // Console.WriteLine($"Server {serverId}:{errorMessage}");
    }

    public bool CanSendPlayer(string tag)
    {
        if (_currentPlayers >= maxPlayers)
        {
            return false;
        }

        foreach (Player player in playersInServer)
        {
            if (IsNameAlreadyInServer(player, tag))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsNameAlreadyInServer(Player player, string name)
    {
        string playerTag = player.nameTag;
        return player.nameTag.ToLower() == name.ToLower() || player.nameTag.ToUpper() == name.ToUpper();
    }
}