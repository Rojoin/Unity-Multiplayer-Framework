using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerNetManager : NetworkManager
{
  //Todo: Make logic for already existing message
    public readonly Dictionary<int, Client> clients = new Dictionary<int, Client>();
    protected readonly Dictionary<IPEndPoint, int> ipToId = new Dictionary<IPEndPoint, int>();

    protected override void OnConnect()
    {
        base.OnConnect();
        connection = new UdpConnection(port, this);
        NetConsole.PlayerID = -10;
        NetExit.PlayerID = -10;
        NetVector3.PlayerID = -10;
        NetPing.PlayerID = -10;
    }

    public override void CloseConnection()
    {
        connection?.Close();
    }

    protected override void OnDisconect()
    {
        base.OnDisconect();
        connection?.Close();
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
        }
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
            //clients.Remove(ipToId[ip]);
        }
    }

    public void AddClient(IPEndPoint ip, out int id, string nameTag)
    {
        if (!ipToId.ContainsKey(ip))
        {
            Debug.Log("Adding client: " + ip.Address);

            id = clientId;
            ipToId[ip] = clientId;
            clients.Add(clientId, new Client(ip, id, DateTime.UtcNow, tag));
            players.Add(new Player(clientId, nameTag));
            clientId++;
        }
        else
        {
            id = ipToId[ip];
        }
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
        var type = NetByteTranslator.getNetworkType(data);
        var playerID = NetByteTranslator.GetPlayerID(data);

        switch (type)
        {
            case MessageType.HandShake:
                NetHandShake handShake = new NetHandShake();
                // data[0] = 243;
                if (handShake.IsMessageCorrect(new List<byte>(data)))
                {
                    string gameTag = handShake.Deserialize(data);
                    Debug.Log($"La ip de el cliente es: {ep.Address} y el nameTag es: {gameTag}");

                    AddClient(ep, out var id, gameTag);
                    NetHandShakeOK handOK = new(players);
                    Broadcast(handOK.Serialize());

                    NetPing ping = new();
                    SendToClient(ping.Serialize(), gameTag, ep);
                }
                else
                {
                    Debug.Log("The message is corrupted");
                }

                break;
            case MessageType.Console:
                break;
            case MessageType.Position:
                break;
            case MessageType.String when !clients.ContainsKey(playerID):
                break;
            case MessageType.String:
                NetConsole message = new();
                string textToWrite =
                    $"{GetPlayer(NetByteTranslator.GetPlayerID(data)).nameTag}:{message.Deserialize(data)}";
                OnChatMessage.Invoke(textToWrite);
                Broadcast(data);

                break;
            case MessageType.Exit:
                RemoveClient(ep);
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
                    $"Pong with {pongMessage.Deserialize(data)} in {clients[currentClientId].GetCurrentMS(currentTime).TotalMilliseconds} ms");
                clients[currentClientId].ResetTimer(currentTime);
                break;
        }
    }
}