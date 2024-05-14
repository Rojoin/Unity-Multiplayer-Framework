using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ServerNetManager : NetworkManager
{
    public readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
    protected readonly Dictionary<IPEndPoint, int> ipToId = new Dictionary<IPEndPoint, int>();


    protected override void OnConnect()
    {
        base.OnConnect();

        connection = new UdpConnection(port, CouldntCreateUDPConnection, this);
        NetConsole.PlayerID = -10;
        NetExit.PlayerID = -10;
        NetPosition.PlayerID = -10;
        NetPing.PlayerID = -10;
        NetConfirmation.PlayerID = -10;
    }

    protected override void ReSendMessage(MessageCache arg0)
    {
        //Todo:Check for every message to have a way back for every client
    }

    public override void CloseConnection()
    {
        connection?.Close();
    }

    protected override void OnDisconect()
    {
        base.OnDisconect();
        NetExit closeServer = new NetExit();
        Broadcast(closeServer.Serialize());
        connection?.Close();
    }

    protected override void CouldntCreateUDPConnection(string errorMessage)
    {
        Debug.Log("Called");
        ChatScreen.Instance.SwitchToNetworkScreen();
        OnErrorMessage.RaiseEvent(errorMessage);
    }

    public IEnumerator StartTimeOutServer(Client client)
    {
        while (client.timer < timeOut)
        {
            client.timer += Time.deltaTime;
            yield return null;
        }

        DisconnectPlayer(client);
    }

    protected override void OnUpdate(float deltaTime)
    {
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
    }

    protected override void CheckTimeOut(float delta)
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
        }
    }

    private void ClearInactiveClients()
    {
        for (int i = 0; i < clients.Count; i++)
        {
            if (clients.ContainsKey(i) && !clients[i].isActive)
            {
                ipToId.Remove(clients[i].ipEndPoint);
                clients.Remove(i);
            }
        }
    }

    private void DisconnectPlayer(Client client)
    {
        string leftMessage = $"The player {client.tag} has left the game.";
        OnTextAdded(leftMessage);

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

        NetHandShakeOK newPlayerList = new NetHandShakeOK(players, MessageFlags.None);
        Broadcast(newPlayerList.Serialize());
        Debug.Log("New Player list:");
        foreach (Player player in players)
        {
            Debug.Log(player.nameTag);
        }
    }

    public void RemoveClient(IPEndPoint ip)
    {
        if (ipToId.ContainsKey(ip))
        {
            Debug.Log("Removing client: " + ip.Address);
            clients[ipToId[ip]].isActive = false;
        }
    }

    public bool TryAddClient(IPEndPoint ip, out int id, string nameTag)
    {
        if (!ipToId.ContainsKey(ip) && !IsNameTagAClient(nameTag))
        {
            Debug.Log("Adding client: " + ip.Address);

            id = clientId;
            ipToId[ip] = clientId;
            clients.Add(clientId, new Client(ip, id, DateTime.UtcNow, nameTag));
            players.Add(new Player(clientId, nameTag));
            clientId++;
            return true;
        }
        else
        {
            id = -7;
            return false;
        }
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

    protected override void OnTextAdded(string text)
    {
        ChatScreen.Instance.AddText("Server:" + text);
        NetConsole message = new(text);
        Broadcast(message.Serialize());
    }

    public override void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);
        bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
        bool isImportant = flags.HasFlag(MessageFlags.Important);
        bool isOrdenable = flags.HasFlag(MessageFlags.Ordenable);
        ulong getMesaggeID = 0;
        if (shouldCheckSum)
        {
            if (!BaseMessage<int>.IsMessageCorrectS(new List<byte>(data)))
            {
                Debug.Log("The packet was corrupted.");
                return;
            }
        }

        if (isOrdenable)
        {
            getMesaggeID = NetByteTranslator.GetMesaggeID(data);
        }

        switch (type)
        {
            case MessageType.HandShake:
                NetHandShake handShake = new NetHandShake();
                // data[0] = 243;

                string gameTag = handShake.Deserialize(data);
                Debug.Log($"La ip de el cliente es: {ep.Address} y el nameTag es: {gameTag}");

                if (TryAddClient(ep, out var id, gameTag))
                {
                    NetHandShakeOK handOK = new(players);
                    Broadcast(handOK.Serialize());
                    string welcomeMessage = $"The player {gameTag} has joined the game.";
                    OnChatMessage.Invoke(welcomeMessage);
                    NetConsole netConsole = new NetConsole(welcomeMessage);
                    Broadcast(netConsole.Serialize());
                    NetPing ping = new();
                    SendToClient(ping.Serialize(), gameTag, ep);
                }
                else
                {
                    NetHandShake errorHandshake = new NetHandShake("Error: Tagname already exist.");
                    SendToClient(errorHandshake.Serialize(), ep);
                }

                break;
            case MessageType.Position:

                if (flags.HasFlag(MessageFlags.Ordenable))
                {
                    getMesaggeID = NetByteTranslator.GetMesaggeID(data);
                    if (clients[playerID].IsTheLastMesagge(MessageType.Position, getMesaggeID))
                    {
                        //TODO:Add logic for distintive gameobject
                        NetPosition netPosition = new NetPosition(Vector3.one, 1);
                        SendToEveryoneExceptClient(netPosition.Serialize(), playerID);
                    }
                }

                break;
            case MessageType.String when !clients.ContainsKey(playerID):
                break;
            case MessageType.String:
                NetConsole message = new();


                if (clients[playerID].IsTheNextMessage(type, getMesaggeID, message) == 0)
                {
                    string deserializeMessage = message.Deserialize(data);
                    string textToWrite =
                        $"{GetPlayer(NetByteTranslator.GetPlayerID(data)).nameTag}:{deserializeMessage}";

                    OnChatMessage.Invoke(textToWrite);
                    message = new NetConsole(deserializeMessage);
                    Broadcast(message.Serialize(playerID));
                    AddImportantMessageToClients(data, type, getMesaggeID);
                }

                if (isImportant)
                {
                    Debug.Log("Llegue");
                    NetConfirmation netConfirmation = new NetConfirmation((type, getMesaggeID));
                    SendToClient(netConfirmation.Serialize(), ep);
                }

                break;
            case MessageType.Exit:
                DisconnectPlayer(clients[playerID]);
                break;
            case MessageType.Ping when !clients.ContainsKey(playerID):
                break;
            case MessageType.Ping:
                NetPing pingMessage = new();
                NetPing pongMessage = new();
                int currentClientId = pingMessage.Deserialize(data);
                SendToClient(pingMessage.Serialize(), currentClientId, ep);
                DateTime currentTime = DateTime.UtcNow;
                Debug.Log(
                    $"Pong with {pongMessage.Deserialize(data)} in {clients[currentClientId].GetCurrentMS(currentTime).Milliseconds} ms");
                clients[currentClientId].ResetTimer(currentTime);
                break;
            case MessageType.HandShakeOk:
                break;
            case MessageType.Confirmation:
                NetConfirmation confirmation = new NetConfirmation();
                clients[playerID].CheckImportantMessageConfirmation(confirmation.Deserialize(data));
                //TODO: add to check if should send the messages again
                break;
        }
    }

    private void AddImportantMessageToClients(byte[] data, MessageType type, ulong getMesaggeID)
    {
        foreach (var client in clients)
        {
            client.Value.lastImportantMessages.Add(new MessageCache(type, data.ToList(), getMesaggeID));
        }
    }
}