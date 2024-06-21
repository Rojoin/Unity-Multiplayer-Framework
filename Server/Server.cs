using System.Net;
using RojoinNetworkSystem;

namespace Server;

public enum GameState
{
    WaitingForPlayers,
    CooldownUntilStart,
    GameHasStarted
}

class Server : IReceiveData
{
    public readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
    protected readonly Dictionary<IPEndPoint, int> ipToId = new Dictionary<IPEndPoint, int>();
    public IPAddress ipAddress { get; set; }
    public int port { get; set; } = 12345;
    public int timeOut = 30;
    public UdpConnection connection;
    public List<Player> players = new List<Player>();
    public string tagName = "";
    protected List<MessageCache> lastImportantMessages = new();
    public int clientId = 0;
    private GameState _gameState;
    public float timerUntilStart = 30;
    private float timerInGame = 120;
    private float countdownUntilGameStart;
    public int playerLimit = 4;
    public int minimunPlayerToInitiate = 2;
    private float timeUntilResend = 3.0f;
    private IReceiveData _receiveDataImplementation;
    private bool closeServer;
    public Server(ref bool isServerRunning)
    {
        CreateServer(out isServerRunning,port);
    }
    public Server(ref bool isServerRunning, int newPort)
    {
        CreateServer(out isServerRunning,newPort);
    }

    private void CreateServer(out bool isServerRunning, int serverPort)
    {
        lastImportantMessages.Clear();
        players.Clear();
        clientId = 0;
        port = serverPort;
        countdownUntilGameStart = timerUntilStart;
        connection = new UdpConnection(port, CouldntCreateUDPConnection, this);
        BaseMessage.PlayerID = -10;
        minimunPlayerToInitiate = 2;
        _gameState = GameState.WaitingForPlayers;
        timerInGame = 120;
        isServerRunning = true;
    }


    ~Server()
    {
        connection.Close();
    }

    public void OnReceiveData(byte[] data, IPEndPoint ipEndpoint)
    {
        OnReceiveDataEvent(data, ipEndpoint);
    }

    protected void CouldntCreateUDPConnection(string errorMessage)
    {
        Console.WriteLine(errorMessage);
        closeServer = true;
    }

    private void CreateMessage(string data)
    {
        NetConsole message = new NetConsole(data);
        byte[] messageDataToSend = message.Serialize();
        Broadcast(messageDataToSend);
    }

    public void OnUpdate(float deltaTime, ref bool isServerRunning)
    {
        CheckTimeOut(deltaTime, ref isServerRunning);
        if (clients.Count > 0)
        {
            foreach (var client in clients)
            {
                if (client.Value.lastImportantMessages.Count > 0)
                {
                    foreach (var importantMessage in client.Value.lastImportantMessages)
                    {
                        importantMessage.timerForResend += deltaTime;
                        if (importantMessage.timerForResend > timeUntilResend)
                        {
                            importantMessage.timerForResend = 0;
                            SendToClient(importantMessage.data.ToArray(), client.Value.ipEndPoint);
                        }
                    }
                }
            }
        }

        CheckLastImportantMessages(deltaTime);
        if (connection != null)
            connection.FlushReceiveData();

        isServerRunning = !closeServer && isServerRunning;
    }

    protected void CheckLastImportantMessages(float deltaTime)
    {
        if (lastImportantMessages.Count > 0)
        {
            foreach (MessageCache cached in lastImportantMessages.ToList())
            {
                if (cached.startTimer)
                {
                    cached.timerForDelete += deltaTime;
                    if (cached.timerForDelete >= timeUntilResend)
                    {
                        lastImportantMessages.Remove(cached);
                    }
                }
            }
        }
    }

    protected void CheckTimeOut(float delta, ref bool isServerRunning)
    {
        if (clients.Count > 0)
        {
            using (var iterator = clients.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    iterator.Current.Value.timer += delta;
                    if (iterator.Current.Value.timer >= timeOut && iterator.Current.Value.isActive)
                    {
                        DisconnectPlayer(iterator.Current.Value);
                    }
                }
            }

            ClearInactiveClients();
            if (_gameState == GameState.GameHasStarted)
            {
                if (clients.Count < 2)
                {
                    NetExit winnerMessage = new("You have won!");
                    Broadcast(winnerMessage.Serialize());
                    isServerRunning = false;
                }
                else
                {
                    timerInGame -= delta;
                    if (timerInGame > 0)
                    {
                        NetTime newTime = new NetTime(delta);
                        Broadcast(newTime.Serialize());
                    }
                    else
                    {
                        NetExit disconnectAll = new NetExit(GetPlayerVictoryString());
                        Broadcast(disconnectAll.Serialize());
                    }
                }
            }

            if (_gameState == GameState.CooldownUntilStart)
            {
                countdownUntilGameStart -= delta;
                if (countdownUntilGameStart < 0)
                {
                    _gameState = GameState.GameHasStarted;
                    //Todo:Change to have send all players initial pos
                    //   gameManager.SetAllPlayerPos();
                    CreateMessage("Server:Go");
                }
            }
        }
    }

    private string GetPlayerVictoryString()
    {
        return "The house is the winner";
    }

    public void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);


        bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
        bool isImportant = flags.HasFlag(MessageFlags.Important);
        bool isOrdenable = flags.HasFlag(MessageFlags.Ordenable);
        ulong getMessageID = 0;
        if (shouldCheckSum)
        {
            if (!BaseMessage<int>.IsMessageCorrectS(new List<byte>(data)))
            {
                Console.WriteLine("The packet was corrupted.");
                return;
            }
        }

        if (isOrdenable)
        {
            getMessageID = NetByteTranslator.GetMesaggeID(data);
        }

        switch (type)
        {
            case MessageType.HandShake:
                CheckHandShake(data, ep);
                break;
            case MessageType.Position when clients.ContainsKey(playerID):
                CheckPositionMessage(data, flags, playerID);
                break;
            case MessageType.Message when !clients.ContainsKey(playerID):
                break;
            case MessageType.Message:
                CheckChatMessage(data, ep, playerID, type, getMessageID, isImportant);
                break;
            case MessageType.Exit when clients.ContainsKey(playerID):
                DisconnectPlayer(clients[playerID]);
                break;
            case MessageType.Ping when !clients.ContainsKey(playerID):
                break;
            case MessageType.Ping:
                CheckPing(data, ep);
                break;
            case MessageType.HandShakeOk:
                break;
            case MessageType.Confirmation when clients.ContainsKey(playerID):
                CheckConfirmation(data, playerID);
                break;

            case MessageType.AskForObject when clients.ContainsKey(playerID):
                CheckAskForBullet(data, ep, playerID, type, getMessageID, isImportant);
                break;
            case MessageType.TRS:
                CheckDamage(data, playerID, ep);
                break;
        }
    }


    private void CheckDamage(byte[] data, int playerID, IPEndPoint ip)
    {
        // NetTRS netTRS = new NetTRS();
        // var damageData = netTRS.Deserialize(data);
        // if (damageData == playerID)
        // {
        //     Console.WriteLine($"Player {playerID} has been killed.");
        //     NetExit netExit = new NetExit("You have been eliminated.");
        //     SendToClient(netExit.Serialize(), ip);
        //     DisconnectPlayer(clients[playerID]);
        // }
    }

    private void CheckAskForBullet(byte[] data, IPEndPoint ep, int playerID, MessageType type, ulong getMessageID,
        bool isImportant)
    {
        // NetSpawnObject objectToSpawn = new NetSpawnObject();
        // MessageCache messageToCheck = new MessageCache(objectToSpawn.GetMessageType(), data.ToList(), getMessageID);
        // if (clients[playerID].IsTheNextMessage(type, messageToCheck, objectToSpawn))
        // {
        //     var newData = objectToSpawn.Deserialize(data);
        //     NetPositionAndRotation netPositionAndRotation =
        //         new NetPositionAndRotation((int)getMessageID, newData.Item1, newData.Item2, newData.Item3);
        //     byte[] messageDataToSend = netPositionAndRotation.Serialize(playerID);
        //
        //     if (_gameState == GameState.GameHasStarted)
        //     {
        //         Broadcast(messageDataToSend);
        //         //Console.WriteLine($"Forwards was:{newData.Item3}");
        //         AddImportantMessageToClients(data, MessageType.PositionAndRotation,
        //             NetByteTranslator.GetMesaggeID(messageDataToSend), true);
        //     }
        //
        //     if (isImportant)
        //     {
        //         //       Console.WriteLine($"Created the confirmation message for {type} with ID {getMessageID}");
        //         NetConfirmation netConfirmation = new NetConfirmation((type, getMessageID));
        //         SendToClient(netConfirmation.Serialize(), ep);
        //     }
        // }
    }

    private void CheckHandShake(byte[] data, IPEndPoint ep)
    {
        NetHandShake handShake = new NetHandShake();
        string gameTag = handShake.Deserialize(data);
        Console.WriteLine($"La ip de el cliente es: {ep.Address} y el nameTag es: {gameTag}");

        if (TryAddClient(ep, gameTag))
        {
            NetHandShakeOK handOK = new(players);
            byte[] handData = handOK.Serialize();
            Broadcast(handData);
            string welcomeMessage = $"Server:The player {gameTag} has joined the game.";
            CreateMessage(welcomeMessage);
            NetConsole netConsole = new NetConsole(welcomeMessage);
            byte[] newData = netConsole.Serialize();
            Broadcast(newData);
            NetByteTranslator.GetMesaggeID(newData);
            NetPing ping = new();
            SendToClient(ping.Serialize(), gameTag, ep);
            AddImportantMessageToClients(handData, MessageType.HandShakeOk, NetByteTranslator.GetMesaggeID(handData),
                true);
            foreach (KeyValuePair<int, Client> VARIABLE in clients)
            {
                if (VARIABLE.Value.tag != gameTag &&
                    VARIABLE.Value.lastReceiveMessage.ContainsKey(MessageType.Position))
                {
                    var msg = VARIABLE.Value.GetLastMessage(MessageType.Position);
                    SendToClient(msg.data.ToArray(), ep);
                }
            }
        }
    }

    private void CheckPositionMessage(byte[] data, MessageFlags flags, int playerID)
    {
        ulong getMessageID;
        if (flags.HasFlag(MessageFlags.Ordenable))
        {
            // NetPlayerPos netPlayerPos = new NetPlayerPos();
            // getMessageID = NetByteTranslator.GetMesaggeID(data);
            // MessageCache msgToCache = new MessageCache(netPlayerPos.GetMessageType(), data.ToList(), getMessageID);
            // if (clients[playerID].IsTheLastMesagge(MessageType.Position, msgToCache))
            // {
            //     var dataReceived = netPlayerPos.Deserialize(data);
            //     NetPlayerPos playerPosToSend = new NetPlayerPos(dataReceived.Item1, dataReceived.Item2);
            //     SendToEveryoneExceptClient(playerPosToSend.Serialize(playerID), playerID);
            // }
            // else
            // {
            //     Console.WriteLine("Wasnt the last");
            // }
        }
    }

    private void CheckConfirmation(byte[] data, int playerID)
    {
        NetConfirmation confirmation = new NetConfirmation();
        //        Console.WriteLine($"Checking Confirmation form player {playerID}.");
        clients[playerID].CheckImportantMessageConfirmation(confirmation.Deserialize(data));
    }

    private void CheckPing(byte[] data, IPEndPoint ep)
    {
        NetPing pingMessage = new();
        NetPing pongMessage = new();
        int currentClientId = pingMessage.Deserialize(data);
        SendToClient(pingMessage.Serialize(), currentClientId, ep);
        DateTime currentTime = DateTime.UtcNow;
        // if (showPing)
        // {
        //     Console.WriteLine(
        //         $"Pong with {pongMessage.Deserialize(data)} in {clients[currentClientId].GetCurrentMS(currentTime).Milliseconds} ms");
        // }

        clients[currentClientId].ResetTimer(currentTime);
    }

    private void CheckChatMessage(byte[] data, IPEndPoint ep, int playerID, MessageType type, ulong getMessageID,
        bool isImportant)
    {
        NetConsole message = new();
        MessageCache msgToCache = new MessageCache(type, data.ToList(), getMessageID);
        if (clients[playerID].IsTheNextMessage(type, msgToCache, message))
        {
            string deserializeMessage = message.Deserialize(data);
            string textToWrite =
                $"{GetPlayer(NetByteTranslator.GetPlayerID(data)).nameTag}:{deserializeMessage}";

            CreateMessage(textToWrite);
            message = new NetConsole(deserializeMessage);
            byte[] messageDataToSend = message.Serialize(playerID);
            Broadcast(messageDataToSend);
            AddImportantMessageToClients(data, type, NetByteTranslator.GetMesaggeID(messageDataToSend), true);

            if (isImportant)
            {
                //                Console.WriteLine($"Created the confirmation message for {type} with ID {getMessageID}");
                NetConfirmation netConfirmation = new NetConfirmation((type, getMessageID));
                SendToClient(netConfirmation.Serialize(), ep);
            }
        }
    }

    private void AddImportantMessageToClients(byte[] data, MessageType type, ulong getMesaggeID,
        bool shouldBeResend = false)
    {
        //        Console.WriteLine($"Adding message of {type} with ID {getMesaggeID} to the clients.");
        foreach (var client in clients)
        {
            MessageCache messageCache = new MessageCache(type, data.ToList(), getMesaggeID)
            {
                canBeResend = shouldBeResend,
                startTimer = !shouldBeResend
            };
            client.Value.lastImportantMessages.Add(messageCache);
        }
    }

    private void ClearInactiveClients()
    {
        Dictionary<int, Client> aux = new(clients);
        foreach (KeyValuePair<int, Client> i1 in aux)
        {
            if (!i1.Value.isActive)
            {
                ipToId.Remove(i1.Value.ipEndPoint);
                i1.Value.OnDestroy();
                clients.Remove(i1.Key);
            }
        }
    }

    private void DisconnectPlayer(Client client)
    {
        string textToWrite = $"The player {client.tag} has left the game.";
        CreateMessage(textToWrite);

        Player playerToRemove = new();
        foreach (Player player in players)
        {
            if (client.id == player.id)
            {
                playerToRemove = player;
                break;
            }
        }

        client.isActive = false;
        players.Remove(playerToRemove);
        NetHandShakeOK newPlayerList = new NetHandShakeOK(players, MessageFlags.None);
        Broadcast(newPlayerList.Serialize());
    }

    public void RemoveClient(IPEndPoint ip)
    {
        if (ipToId.ContainsKey(ip))
        {
            Console.WriteLine($"Removing {ipToId[ip]}");
            Console.WriteLine("Removing client: " + ip.Address);
            clients[ipToId[ip]].isActive = false;
        }
    }

    public bool TryAddClient(IPEndPoint ip, string nameTag)
    {
        if (!ipToId.ContainsKey(ip))
        {
            if (_gameState != GameState.GameHasStarted)
            {
                if (clients.Count < playerLimit)
                {
                    if (!IsNameTagAClient(nameTag))
                    {
                        Console.WriteLine("Adding client: " + ip.Address);

                        int id = clientId;
                        ipToId[ip] = clientId;

                        clients.Add(clientId, new Client(ip, id, DateTime.UtcNow, nameTag));

                        players.Add(new Player(clientId, nameTag));
                        clientId++;
                        //OnPlayerCreated.RaiseEvent(id, nameTag);
                        Console.WriteLine("Entre");
                        if (clients.Count > minimunPlayerToInitiate)
                        {
                            _gameState = GameState.CooldownUntilStart;
                            string data = $"Game will start in {countdownUntilGameStart}.";
                            //OnMessageCreatedChannel.RaiseEvent(data);
                            CreateMessage(data);
                        }


                        return true;
                    }
                    else
                    {
                        NetExit errorHandshake = new NetExit("Error: Tagname already exist.");
                        SendToClient(errorHandshake.Serialize(), ip);
                    }
                }
                else
                {
                    NetExit errorHandshake = new NetExit("Error: The Player limit has been reach.");
                    SendToClient(errorHandshake.Serialize(), ip);
                }
            }
            else
            {
                NetExit errorHandshake = new NetExit("Error: Game has Already Started.");
                SendToClient(errorHandshake.Serialize(), ip);
            }
        }


        return false;
    }

    private bool IsNameTagAClient(string nametag)
    {
        foreach (var player in players)
        {
            if (player.nameTag == nametag)
            {
                return true;
            }
        }

        return false;
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

    public void SendToClient(byte[] data, IPEndPoint ep)
    {
        connection.Send(data, ep);
    }

    public void SendToEveryoneExceptClient(byte[] data, int id)
    {
        using (var iterator = clients.GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.id != id && iterator.Current.Value.isActive)
                {
                    connection.Send(data, iterator.Current.Value.ipEndPoint);
                }
            }
        }
    }

    public void SendToEveryoneExceptClient(byte[] data, string nameTag)
    {
        using (var iterator = clients.GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.tag != nameTag && iterator.Current.Value.isActive)
                {
                    connection.Send(data, iterator.Current.Value.ipEndPoint);
                }
            }
        }
    }


    public void Broadcast(byte[] data)
    {
        using (var iterator = clients.GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.isActive)
                {
                    connection.Send(data, iterator.Current.Value.ipEndPoint);
                }
            }
        }
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

    protected Player GetPlayer(int id)
    {
        foreach (Player t in players)
        {
            if (t.id == id)
            {
                return t;
            }
        }

        return new Player(-999, "Not Found");
    }
}