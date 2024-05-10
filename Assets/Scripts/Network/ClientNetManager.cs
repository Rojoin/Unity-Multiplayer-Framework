using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

public class ClientNetManager : NetworkManager
{
    private DateTime currentTimePing;
    private DateTime lastTimeConnection;
    private float TimeOutTimer;
    [SerializeField] private bool isConnected;

    private UnityEvent OnServerCloseEvent;

    protected override void OnConnect()
    {
        base.OnConnect();
        isConnected = false;
        connection = new UdpConnection(ipAddress, port, tagName, this);
        OnServerDisconnect.AddListener(CloseConnection);
        TimeOutTimer = 0;
    }

    protected override void OnDisconect()
    {
        base.OnDisconect();
        CloseConnection();
    }
    public override void CloseConnection()
    {
        if (isConnected)
        {
            NetExit netExit = new NetExit();
            SendToServer(netExit.Serialize());
            connection.Close();
            isConnected = false;
            TimeOutTimer = 0;
        }
    }


    protected override void CheckTimeOut(float delta)
    {
        if (isConnected)
        {
            TimeOutTimer += Time.deltaTime;
            if (TimeOutTimer >= timeOut)
            {
                OnServerDisconnect.Invoke();
            }
        }
    }

    protected override void OnTextAdded(string text)
    {
        NetConsole message = new(text);
        SendToServer(message.Serialize());
    }

    public override void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        var type = NetByteTranslator.getNetworkType(data);
        var playerID = NetByteTranslator.GetPlayerID(data);
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
                string idName = playerID != -10 ? GetPlayer(playerID).nameTag + ":" : "Server:";
                OnChatMessage.Invoke(idName + message.Deserialize(data));
                break;
            case MessageType.HandShakeOk:
                NetHandShakeOK handOk = new();
                List<Player> newPlayersList = handOk.Deserialize(data);
                SetPlayer(newPlayersList);
                isConnected = true;
                foreach (Player pl in newPlayersList)
                {
                    Debug.Log("This is " + pl.nameTag + "with id:" + pl.id);
                }

                Debug.Log("My id is" + clientId);

                break;
            case MessageType.Exit:
                break;
            case MessageType.Ping:
                //Empezar la corrutina del timeout del servidor
                NetPing netPong = new NetPing();
                SendToServer(netPong.Serialize());
                currentTimePing = DateTime.UtcNow;
                var a = currentTimePing - lastTimeConnection;
                lastTimeConnection = currentTimePing;
                TimeOutTimer = 0;
                break;

            default:
                Debug.Log("MessageType not found");
                break;
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
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }
}