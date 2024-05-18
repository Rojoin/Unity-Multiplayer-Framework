using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class ClientNetManager : NetworkManager, IMessageChecker
{
    private DateTime currentTimePing;
    private DateTime lastTimeConnection;
    private float TimeOutTimer;
    [SerializeField] private bool isConnected;

    private UnityEvent OnServerCloseEvent;
    private UnityEvent OnCouldntConnectToServer;
    public UnityEvent<double> OnMsUpdated;

    public Dictionary<MessageType, List<MessageCache>> pendingMessages = new();

    protected Dictionary<MessageType, ulong> lastReceiveMessage = new();
    UnityEvent<byte[], IPEndPoint> IMessageChecker.OnPreviousData { get; set; } = new();

    protected override void OnConnect()
    {
        base.OnConnect();
        isConnected = false;
        connection = new UdpConnection(ipAddress, port, tagName, CouldntCreateUDPConnection, this);
        OnServerDisconnect.AddListener(CloseConnection);
        TimeOutTimer = 0;
        lastReceiveMessage.Clear();
        pendingMessages.Clear();
        ((IMessageChecker)this).OnPreviousData.AddListener(OnReceiveDataEvent);
    }


    protected override void OnDisconect()
    {
        base.OnDisconect();
        CloseConnection();
        ((IMessageChecker)this).OnPreviousData.RemoveListener(OnReceiveDataEvent);
    }

    protected override void ReSendMessage(MessageCache arg0)
    {
        SendToServer(arg0.data.ToArray());
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
            //Todo: Look a way more clean to do it
            ChatScreen.Instance.SwitchToNetworkScreen();
        }
    }

    protected override void CouldntCreateUDPConnection(string errorMessage)
    {
        OnErrorMessage.RaiseEvent(errorMessage);
        Debug.Log(errorMessage);
        ChatScreen.Instance.SwitchToNetworkScreen();
    }

    protected override void OnUpdate(float deltaTime)
    {
        foreach (MessageCache cached in lastImportantMessages)
        {
            if (cached.canBeResend)
            {
                cached.timerForResend += deltaTime;
                if (cached.timerForResend >= timeUntilResend)
                {
                    Debug.Log($"The Message {cached.type} with ID {cached.messageId} has been resend.");
                    OnResendMessage.Invoke(cached);
                    cached.timerForResend = 0.0f;
                }
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
        AddMessageToCacheList(MessageType.String, serialize.ToList(), NetByteTranslator.GetMesaggeID(serialize), true);
    }

    public override void OnReceiveDataEvent(byte[] data, IPEndPoint ep = null)
    {
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);
        if (type != MessageType.Ping)
        {
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
                Debug.Log(getMessageID);
                if (IsTheNextMessage(type, getMessageID, message))
                {
                    string idName = playerID != -10 ? GetPlayer(playerID).nameTag + ":" : "Server:";
                    OnChatMessage.Invoke(idName + message.Deserialize(data));
                    AddMessageToCacheList(MessageType.String, data.ToList(), getMessageID, false);
                    if (isImportant)
                    {
                        Debug.Log("Confirmation Message" + getMessageID);
                        NetConfirmation confirmation = new NetConfirmation((type, getMessageID));
                        SendToServer(confirmation.Serialize());
                    }
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
        Debug.Log($"The message that appeared was {data.Item1} with ID {data.Item2}.");
        foreach (var VARIABLE in lastImportantMessages)
        {
            if (VARIABLE.messageId == data.Item2 && VARIABLE.type == data.Item1 && !VARIABLE.startTimer)
            {
                VARIABLE.startTimer = true;
                VARIABLE.canBeResend = false;
                Debug.Log($"Message from Server was confirmed {VARIABLE.type} with ID {VARIABLE.messageId}.");
                break;
            }
        }
    }

    public bool IsTheNextMessage(MessageType messageType, ulong value, BaseMessage baseMessage)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }

        if (lastReceiveMessage[messageType] + 1 == value)
        {
            lastReceiveMessage[messageType] = value;
            CheckPendingMessages(messageType, value);

            return true;
        }
        else
        {
            pendingMessages.TryAdd(messageType, new List<MessageCache>());
            pendingMessages[messageType].Add(new MessageCache(messageType, value));
            pendingMessages[messageType].Sort(Utilities.Sorter);
            return false;
        }
    }

    public void CheckPendingMessages(MessageType messageType, ulong value)
    {
        if (pendingMessages.ContainsKey(messageType) && pendingMessages[messageType].Count > 0)
        {
            pendingMessages[messageType].Sort(Utilities.Sorter);
            if (value - pendingMessages[messageType][0].messageId + 1 == 0)
            {
                Debug.Log($"Sending message that was pending of type {messageType}.");
                ((IMessageChecker)this).OnPreviousData.Invoke(pendingMessages[messageType][0].data.ToArray(), null);
                pendingMessages[messageType].RemoveAt(0);
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

    void IMessageChecker.CheckImportantMessageConfirmation((MessageType, ulong) data)
    {
        CheckImportantMessageConfirmation(data);
    }

    private void AddMessageToCacheList(MessageType type, List<byte> data, ulong messageId, bool shouldBeResend = false)
    {
        MessageCache messageToCache = new(type, data, messageId)
        {
            canBeResend = shouldBeResend,
            startTimer = !shouldBeResend
        };
        lastImportantMessages.Add(messageToCache);
    }
}