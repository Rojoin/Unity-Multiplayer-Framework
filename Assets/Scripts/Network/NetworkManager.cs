using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;


public class Client
{
    public float timeStamp;
    public int id;
    public IPEndPoint ipEndPoint;
    public string tag;
    public Coroutine timeOutCorroutine;
    public float timer;

    public Client(IPEndPoint ipEndPoint, int id, float timeStamp, string tag)
    {
        this.timeStamp = timeStamp;
        this.id = id;
        this.ipEndPoint = ipEndPoint;
        this.tag = tag;
        timeOutCorroutine = null;
        timer = 0.0f;
    }

    public float GetCurrentMS(float currentTimeStamp)
    {
        return currentTimeStamp - this.timeStamp;
    }

    public void ResetTimer(float currentTimeStamp)
    {
        Debug.Log($" Timer for {id} has been resetted from {timer}.");
        this.timer = 0.0f;
        timeStamp = currentTimeStamp;
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

    public int timeOut = 30;
    private WaitForSeconds timeUntilDisconnect;


    public Action<byte[], IPEndPoint, int> OnReceiveEvent;

    public UdpConnection connection;

    public readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
    private readonly Dictionary<IPEndPoint, int> ipToId = new Dictionary<IPEndPoint, int>();
    public List<Player> players = new();
    public string tagName = "";

    private WaitForSeconds timeUntilTimeOut;
    public int clientId = 0; // This id should be generated during first handshake

    public UnityEvent<Client> OnPlayerDisconnect;
    public UnityEvent OnServerDisconnect;
    public UnityEvent<string, int> OnChatMessage;
    private float clientCurrentTime;
    private float clientLastTime;
    private float clientDisconnectTimer;

    protected override void Initialize()
    {
        base.Initialize();
        if (isServer)
        {
            timeUntilDisconnect = new WaitForSeconds(timeOut);
        }
        OnServerDisconnect.AddListener(CloseConnection);
    }

    public void CloseConnection()
    {
        connection.Close();
        connection = null;
        ChatScreen.Instance.SwitchToNetworkScreen();
    }

    private void OnDestroy()
    {
        if (!isServer && connection != null)
        {
            NetExit netExit = new NetExit();
            SendToServer(netExit.Serialize());
        }
        else
        {
            connection?.Close();
        }

        OnPlayerDisconnect.RemoveAllListeners();
        OnServerDisconnect.RemoveAllListeners();
    }

    public void StartServer(int port)
    {
        isServer = true;
        this.port = port;
        connection = new UdpConnection(port, this);
        NetConsole.PlayerID = -10;
        NetExit.PlayerID = -10;
        NetVector3.PlayerID = -10;
        NetPing.PlayerID = -10;
    }

    public IEnumerator StartTimeOutServer(Client client)
    {
        while (client.timer < timeOut)
        {
            client.timer += Time.deltaTime;
            Debug.Log(client.timer);
            yield return null;
        }

        DisconnectPlayer(client);
    }

    public IEnumerator StartTimeOutClient()
    {
        while (clientDisconnectTimer < timeOut)
        {
            clientDisconnectTimer += Time.deltaTime;
            Debug.Log(clientDisconnectTimer);
            yield return null;
        }

        connection.Close();
    }

    private void DisconnectPlayer(Client client)
    {
        RemoveClient(client.ipEndPoint);
        Player playerToRemove = new();
        foreach (Player player in players)
        {
            if (client.id == player.id)
            {
                playerToRemove = player;
                break;
            }
        }

        players.Remove(playerToRemove);

        NetHandShakeOK newPlayerList = new NetHandShakeOK(players);
        Broadcast(newPlayerList.Serialize());
        Debug.Log("New Player list:");
        foreach (Player player in players)
        {
            Debug.Log(player.nameTag);
        }
    }

    public void StartClient(IPAddress ip, int port)
    {
        isServer = false;

        this.port = port;
        this.ipAddress = ip;

        connection = new UdpConnection(ip, port, tagName, this);
    }

    public void AddClient(IPEndPoint ip, out int id, string nameTag)
    {
        if (!ipToId.ContainsKey(ip))
        {
            Debug.Log("Adding client: " + ip.Address);

            id = clientId;
            ipToId[ip] = clientId;
            clients.Add(clientId, new Client(ip, id, Time.realtimeSinceStartup, tag));
            players.Add(new Player(clientId, nameTag));
            clients[clientId].timeOutCorroutine = StartCoroutine(StartTimeOutServer(clients[clientId]));
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
        NetPing.PlayerID = clientId;
        NetPong.PlayerID = clientId;
    }

    public void OnReceiveData(byte[] data, IPEndPoint ip, int id, string tag)
    {
        if (OnReceiveEvent != null)
        {
            OnReceiveEvent.Invoke(data, ip, id);
        }

        OnReceiveDataEvent(data, ip, id);
    }

    void OnReceiveDataEvent(byte[] data, IPEndPoint ep, int id)
    {
        var type = NetByteTranslator.getNetworkType(data);
        var playerID = NetByteTranslator.GetPlayerID(data);
        Debug.Log("Checking");

        if (isServer)
        {
            switch (type)
            {
                case MessageType.HandShake:
                    NetHandShake handShake = new NetHandShake();
                    string gameTag = handShake.Deserialize(data);
                    Debug.Log($"La ip de el cliente es: {ep.Address} y el nameTag es: {gameTag}");

                    AddClient(ep, out id, gameTag);
                    NetHandShakeOK handOK = new(players);
                    Broadcast(handOK.Serialize());

                    NetPing ping = new();
                    SendToClient(ping.Serialize(), gameTag, ep);

                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String when !clients.ContainsKey(playerID):
                    break;
                case MessageType.String:

                    NetConsole message = new();
                    OnChatMessage.Invoke(message.Deserialize(data), NetByteTranslator.GetPlayerID(data));
                    Broadcast(data);

                    break;
                case MessageType.Exit:
                    RemoveClient(ep);
                    break;
                case MessageType.Pong when !clients.ContainsKey(playerID):
                    break;
                case MessageType.Pong:
                    NetPing pingMessage = new();
                    NetPong pongMessage = new();
                    int currentClientId = pingMessage.Deserialize(data);
                    SendToClient(pingMessage.Serialize(), currentClientId, ep);
                    float currentTime = Time.time;
                    Debug.Log(
                        $"Pong with {pongMessage.Deserialize(data)} in {clients[currentClientId].GetCurrentMS(currentTime)} ms");
                    clients[currentClientId].ResetTimer(currentTime);
                    break;
            }
        }

        else
        {
            switch (type)
            {
                case MessageType.HandShake:


                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String:
                    NetConsole message = new();
                    Debug.Log("MessageType is String");
                    Debug.Log(NetByteTranslator.GetPlayerID(data));
                    OnChatMessage.Invoke(message.Deserialize(data), NetByteTranslator.GetPlayerID(data));
                    break;
                case MessageType.HandShakeOk:
                    NetHandShakeOK handOk = new();
                    List<Player> players = handOk.Deserialize(data);
                    SetPlayer(players);

                    StartCoroutine(StartTimeOutClient());
                    foreach (Player pl in NetworkManager.Instance.players)
                    {
                        Debug.Log("This is " + pl.nameTag + "with id:" + pl.id);
                    }

                    Debug.Log("My id is" + NetworkManager.Instance.clientId);

                    break;
                case MessageType.Exit:
                    break;
                case MessageType.Ping:

                    //Empezar la corrutina del timeout del servidor
                    NetPong netPong = new NetPong();
                    SendToServer(netPong.Serialize());
                    clientCurrentTime = Time.time;
                    var a = clientCurrentTime - clientLastTime;
                    Debug.Log("Ping in " + a + "ms");
                    clientLastTime = clientCurrentTime;
                    clientDisconnectTimer = 0;
                    break;

                default:
                    Debug.Log("MessageType not found");
                    break;
            }
        }
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    public void SendToClient(byte[] data, string nameTag, IPEndPoint ep)
    {
        IPEndPoint clientIp = ep;

        foreach (var client in clients)
        {
            if (client.Value.tag == nameTag)
            {
                clientIp = client.Value.ipEndPoint;
                break;
            }
        }

        connection.Send(data, clientIp);
    }

    public void SendToClient(byte[] data, int id, IPEndPoint ep)
    {
        IPEndPoint clientIp = ep;

        foreach (var client in clients)
        {
            if (client.Value.id == id)
            {
                clientIp = client.Value.ipEndPoint;
                break;
            }
        }

        connection.Send(data, clientIp);
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