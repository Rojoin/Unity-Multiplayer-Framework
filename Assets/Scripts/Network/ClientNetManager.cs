using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class ClientNetManager : NetworkManager
{
    private DateTime currentTimePing;
    private DateTime lastTimeConnection;
    private float TimeOutTimer;
    [SerializeField] private bool isConnected;

    private UnityEvent OnServerCloseEvent;
    private UnityEvent OnCouldntConnectToServer;
    public UnityEvent<double> OnMsUpdated;


    protected Dictionary<MessageType, ulong> lastReceiveMessage = new();

    protected override void OnConnect()
    {
        base.OnConnect();
        isConnected = false;
        connection = new UdpConnection(ipAddress, port, tagName, CouldntCreateUDPConnection, this);
        OnServerDisconnect.AddListener(CloseConnection);
        TimeOutTimer = 0;
    }

    protected override void ReSendMessage(MessageCache arg0)
    {
        SendToServer(arg0.data.ToArray());
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

    protected override void CouldntCreateUDPConnection(string errorMessage)
    {
        OnErrorMessage.RaiseEvent(errorMessage);
        ChatScreen.Instance.SwitchToNetworkScreen();
    }

    protected override void OnUpdate(float deltaTime)
    {
        foreach (MessageCache cached in lastImportantMessages)
        {
            cached.timerForResend += deltaTime;
            if (cached.timerForResend >= timeUntilResend)
            {
                OnResendMessage.Invoke(cached);
                cached.timerForResend = 0.0f;
            }
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
        byte[] serialize = message.Serialize();
        SendToServer(serialize);
        lastImportantMessages.Add(new MessageCache(MessageType.String, serialize.ToList(),
            NetByteTranslator.GetMesaggeID(serialize)));
    }

    public override void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);
        if (type != MessageType.Ping)
        {
            Debug.Log(type);
        }

        bool shouldCheckSum = flags.HasFlag(MessageFlags.CheckSum);
        bool isImportant = flags.HasFlag(MessageFlags.Important);
        bool isOrdenable = flags.HasFlag(MessageFlags.Important);
        ulong getMessageID = 0;
        if (shouldCheckSum)
        {
            if (!BaseMessage<int>.IsMessageCorrectS(data.ToList()))
            {
                Debug.Log("The packet was corrupted.");
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
                NetHandShake errorMessage = new NetHandShake();

                ChatScreen.Instance.SwitchToNetworkScreen();
                OnErrorMessage.RaiseEvent(errorMessage.Deserialize(data));
                OnServerDisconnect.Invoke();
                break;
            case MessageType.Position:
                break;
            case MessageType.String:
                NetConsole message = new();

                if (IsTheLastMesagge(MessageType.String, getMessageID))
                {
                    string idName = playerID != -10 ? GetPlayer(playerID).nameTag + ":" : "Server:";
                    OnChatMessage.Invoke(idName + message.Deserialize(data));
                    lastImportantMessages.Add(new MessageCache(MessageType.String, data.ToList(), getMessageID));
                    NetConfirmation confirmation = new NetConfirmation((type, getMessageID));
                    SendToServer(confirmation.Serialize());
                }
                else
                {
                    Debug.Log("Message wasnt the last");
                }

                break;
            case MessageType.HandShakeOk:

                NetHandShakeOK handOk = new();
                List<Player> newPlayersList = handOk.Deserialize(data);
                SetPlayer(newPlayersList);
                if (!isConnected)
                {
                    NetworkScreen.Instance.SwitchToChatScreen();
                }

                isConnected = true;
                foreach (Player pl in newPlayersList)
                {
                    Debug.Log("This is " + pl.nameTag + "with id:" + pl.id);
                }

                Debug.Log("My id is" + clientId);

                break;
            case MessageType.Exit:

                ChatScreen.Instance.SwitchToNetworkScreen();
                OnErrorMessage.RaiseEvent("The server has been closed.");
                OnServerDisconnect.Invoke();
                break;
            case MessageType.Ping:

                NetPing netPong = new NetPing();
                SendToServer(netPong.Serialize());
                currentTimePing = DateTime.UtcNow;
                var a = currentTimePing - lastTimeConnection;
                lastTimeConnection = currentTimePing;
                OnMsUpdated.Invoke(a.TotalMilliseconds);
                TimeOutTimer = 0;
                break;

            case MessageType.Confirmation:
                //TODO:Check Why confirmation doesnt work, probably the message cache is bad
                Debug.Log("Confirmation Message Appears");
                NetConfirmation netConfirmation = new NetConfirmation();
                CheckImportantMessageConfirmation(netConfirmation.Deserialize(data));
                break;
            default:
                Debug.Log("MessageType not found");
                break;
        }

        if (flags.HasFlag(MessageFlags.Important))
        {
            //TODO:Send Confirmation
        }
    }

    private void CheckImportantMessageConfirmation((MessageType, ulong) data)
    {
        foreach (var VARIABLE in lastImportantMessages)
        {
       
            if (VARIABLE.messageId == data.Item2 && VARIABLE.type == data.Item1)
            {
                VARIABLE.startTimer = true;
                Debug.Log("LLego");
                break;
            }
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
        NetPosition.PlayerID = clientId;
        NetPing.PlayerID = clientId;
        NetConfirmation.PlayerID = clientId;
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    public bool IsTheLastMesagge(MessageType messageType, ulong value)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }
Debug.Log($"The message id is {value}");
Debug.Log($"The last id is {lastReceiveMessage[messageType]}");
        if (lastReceiveMessage[messageType] > value)
        {
            return false;
        }

        lastReceiveMessage[messageType] = value;
        return true;
    }
}