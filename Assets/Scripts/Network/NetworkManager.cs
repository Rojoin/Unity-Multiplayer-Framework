using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public struct Client
{
    public float timeStamp;
    public int id;
    public IPEndPoint ipEndPoint;
    public string tag;

    public Client(IPEndPoint ipEndPoint, int id, float timeStamp, string tag)
    {
        this.timeStamp = timeStamp;
        this.id = id;
        this.ipEndPoint = ipEndPoint;
        this.tag = tag;
    }
}

public struct Player
{
    public int id;
    public string nameTag;

   public Player(int id, string nameTag)
    {
        this.id = id;
        this.nameTag = nameTag;
    }
}

public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IReceiveData
{
    public IPAddress ipAddress { get; private set; }

    public int port { get; private set; }

    public bool isServer { get; private set; }

    public int TimeOut = 30;

    public Action<byte[], IPEndPoint, int> OnReceiveEvent;

    public UdpConnection connection;

    private readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
    private readonly Dictionary<IPEndPoint, int> ipToId = new Dictionary<IPEndPoint, int>();
    public List<Player> players = new();
    public string tagName = "";

    public int clientId = 0; // This id should be generated during first handshake

    //Todo dividir logica segun cliente y servidor
    private void OnDestroy()
    {
        if (!isServer)
        {
            NetExit netExit = new NetExit();
            SendToServer(netExit.Serialize());
        }
    }

    public void StartServer(int port)
    {
        isServer = true;
        this.port = port;
        connection = new UdpConnection(port, this);
        NetConsole.PlayerID = -10;
        NetExit.PlayerID = -10;
        NetVector3.PlayerID = -10;
    }

    public void StartClient(IPAddress ip, int port)
    {
        isServer = false;

        this.port = port;
        this.ipAddress = ip;

        connection = new UdpConnection(ip, port, tagName, this);

        // AddClient(new IPEndPoint(ip, port), out var id);
    }

    public void AddClient(IPEndPoint ip, out int id, string nameTag)
    {
        if (!ipToId.ContainsKey(ip))
        {
            Debug.Log("Adding client: " + ip.Address);

            id = clientId;
            ipToId[ip] = clientId;
            clients.Add(clientId, new Client(ip, id, Time.realtimeSinceStartup, tag));
            players.Add(new Player(clientId,nameTag));
            clientId++;
        }
        else
        {
            id = ipToId[ip];
        }
    }

    public void RemoveClient(IPEndPoint ip)
    {
        if (ipToId.ContainsKey(ip))
        {
            Debug.Log("Removing client: " + ip.Address);
            clients.Remove(ipToId[ip]);
        }
    }

    public void SetPlayer(List<Player> newPlayersList)
    {
        foreach (Player player in newPlayersList)
        {
            if (tagName != player.nameTag) continue;
            clientId = player.id;
            break;
        }

        players = newPlayersList;
        NetConsole.PlayerID = clientId;
        NetExit.PlayerID = clientId;
        NetVector3.PlayerID = clientId;
        
    }
    public void OnReceiveData(byte[] data, IPEndPoint ip, int id, string tag)
    {

        if (OnReceiveEvent != null)
            OnReceiveEvent.Invoke(data, ip, id);
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    public void SendToClient(byte[] data, IPEndPoint ip)
    {
        connection.Send(data, ip);
    }

    public void Broadcast(byte[] data)
    {
        using (var iterator = clients.GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                connection.Send(data, iterator.Current.Value.ipEndPoint);
            }
        }
    }

    void Update()
    {
        // Flush the data in main thread
        if (connection != null)
            connection.FlushReceiveData();
    }

    public Player GetPlayer(int id)
    {
        foreach (Player player in players)
        {
            if (player.id == id)
            {
                return player;
            }
        }

        return new Player(-999, "Not Found");
    }
}