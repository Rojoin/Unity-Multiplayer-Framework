using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime;
using RojoinNetworkSystem;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ClientNetManager : NetworkManager, IMessageChecker
{
    private DateTime currentTimePing;
    private DateTime lastTimeConnection;
    private float TimeOutTimer;
    [SerializeField] private bool isConnected;

    private UnityEvent OnServerCloseEvent;
    private UnityEvent OnCouldntConnectToServer;
    public UnityEvent<double> OnMsUpdated;
    public AskForPlayerChannelSo OnMyPlayerCreated;
    public IntChannelSO OnHittedPlayer;
    private bool hasPositionBeenSet;

    //   public UnityEvent<object, List<int>, int> OnValueDataReceived;
    public UnityEvent<byte[]> OnValueDataReceived;

    public Vector3ChannelSO OnMyPlayerMoved;

    public Dictionary<MessageType, List<MessageCache>> pendingMessages = new();

    protected Dictionary<MessageType, MessageCache> lastReceiveMessage = new();
    UnityEvent<byte[], IPEndPoint> IMessageChecker.OnPreviousData { get; set; } = new();

    protected override void OnConnect()
    {
        base.OnConnect();
        isConnected = false;
        connection = new UdpConnection(ipAddress, port, tagName, CouldntCreateUDPConnection, this);
        OnServerDisconnect.AddListener(CloseConnection);
        OnMyPlayerMoved.Subscribe(SendPosition);

        TimeOutTimer = 0;
        lastReceiveMessage.Clear();
        pendingMessages.Clear();
        hasPositionBeenSet = false;
        ((IMessageChecker)this).OnPreviousData.AddListener(OnReceiveDataEvent);
    }

    [ContextMenu("Test net Message")]
    private void Test()
    {
        NetFloat a = new NetFloat(999, -1, new List<int>());
        SendToServer(a.Serialize());
    }


    protected override void OnDisconect()
    {
        base.OnDisconect();
        CloseConnection();

        OnServerDisconnect.RemoveListener(CloseConnection);
        OnMyPlayerMoved.Unsubscribe(SendPosition);
        hasPositionBeenSet = false;
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
            NetExit netExit = new NetExit($"The player {tagName}");
            SendToServer(netExit.Serialize());
            connection.Close();
            isConnected = false;
            TimeOutTimer = 0;
            ChatScreen.Instance.SwitchToNetworkScreen();
        }
    }

    protected override void CouldntCreateUDPConnection(string errorMessage)
    {
        //  OnErrorMessage.RaiseEvent(errorMessage);
        Debug.Log(errorMessage);
        //    ChatScreen.Instance.SwitchToNetworkScreen();
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
        AddMessageToCacheList(MessageType.Message, serialize.ToList(), NetByteTranslator.GetMesaggeID(serialize), true);
    }

    public override void OnReceiveDataEvent(byte[] data, IPEndPoint ep = null)
    {
        if (data == null || data.Length == 0)
            return;
        MessageType type = NetByteTranslator.GetNetworkType(data);
        int playerID = NetByteTranslator.GetPlayerID(data);
        MessageFlags flags = NetByteTranslator.GetFlags(data);

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
            case MessageType.Message:
                CheckChatMessage(data, getMessageID, type, playerID, isImportant);
                break;
            case MessageType.HandShakeOk:
                CheckHandShakeOKMessage(data, isImportant, type, getMessageID);
                break;
            case MessageType.Exit:
                CheckExitMessage(data);
                break;
            case MessageType.Ping:
                CheckPing();
                break;
            case MessageType.Confirmation:
                CheckConfirmation(data);
                break;
            case MessageType.Error:
                break;
            case MessageType.ServerDir:
                connection.Close();
                NetServerDirection messageReceived = new NetServerDirection();
                (string, int) connectionData = messageReceived.CastFromObj(messageReceived.Deserialize(data));
                IPAddress newAdressToConnect = IPAddress.Parse(connectionData.Item1);
                int newPortToConnect = System.Convert.ToInt32(connectionData.Item2);
                connection = new UdpConnection(newAdressToConnect, newPortToConnect, tagName,
                    CouldntCreateUDPConnection, this);
                break;
            default:
                try
                {
                    OnValueDataReceived?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }

                break;
        }
    }

    private void OnHandShakeMessage(byte[] data)
    {
        NetHandShake errorMessage = new NetHandShake();
        ChatScreen.Instance.SwitchToNetworkScreen();
        //  OnErrorMessage.RaiseEvent(errorMessage. cas errorMessage.Deserialize(data));
        OnServerDisconnect.Invoke();
    }

    private void CheckExitMessage(byte[] data)
    {
        NetExit exitMessage = new NetExit();
        ChatScreen.Instance.SwitchToNetworkScreen();
        //  OnErrorMessage.RaiseEvent(exitMessage.Deserialize(data));
        OnServerDisconnect.Invoke();
    }

    private void CheckPing()
    {
        NetPing netPong = new NetPing();
        SendToServer(netPong.Serialize());
        currentTimePing = DateTime.UtcNow;
        var a = currentTimePing - lastTimeConnection;
        lastTimeConnection = currentTimePing;
        OnMsUpdated.Invoke(a.TotalMilliseconds);
        TimeOutTimer = 0;
    }

    private static void CheckPlayerDamage(byte[] data, int playerID)
    {
        //  NetDamage damage = new NetDamage();
        //  int damageData = damage.Deserialize(data);
        //  Debug.Log($"Player id {playerID}");
    }

    private void CheckConfirmation(byte[] data)
    {
        // Debug.Log("Confirmation Message Appears");
        NetConfirmation netConfirmation = new NetConfirmation();
        //ToDo:
        // CheckImportantMessageConfirmation(netConfirmation.Deserialize(data));
    }

    private void CheckPlayerPos(byte[] data, MessageFlags flags)
    {
        ulong getMessageID;
        if (flags.HasFlag(MessageFlags.Ordenable))
        {
            getMessageID = NetByteTranslator.GetMesaggeID(data);
            // MessageCache msg = new MessageCache(netPlayerPos.GetMessageType(), data.ToList(), getMessageID);
            // if (IsTheLastMesagge(MessageType.Position, msg))
            // {
            //     (System.Numerics.Vector3, int) dataReceived;
            //     dataReceived = netPlayerPos.Deserialize(data);
            //     OnPlayerMoved.RaiseEvent(dataReceived.Item2, dataReceived.Item1.ToUnityVector3());
            // }
        }
    }

    private void CheckChatMessage(byte[] data, ulong getMessageID, MessageType type, int playerID, bool isImportant)
    {
        NetConsole message = new();

        MessageCache msg = new MessageCache(type, data.ToList(), getMessageID);
        if (IsTheNextMessage(type, msg, message))
        {
            string idName = playerID != -10 ? GetPlayer(playerID).nameTag + ":" : "Server:";
            OnChatMessage.Invoke(idName + message.Deserialize(data));
            AddMessageToCacheList(MessageType.Message, data.ToList(), getMessageID, false);
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
    }

    private void CheckHandShakeOKMessage(byte[] data, bool isImportant, MessageType type, ulong getMessageID)
    {
        NetHandShakeOK handOk = new();
        List<Player> newPlayersList = handOk.CastFromObj(handOk.Deserialize(data));
        MessageCache msgCache = new(type, data.ToList(), getMessageID);
        if (IsTheNextMessage(type, msgCache, handOk))
        {
            SetPlayer(newPlayersList);
            if (!isConnected)
            {
                NetworkScreen.Instance.SwitchToChatScreen();
            }

            if (isImportant && isConnected)
            {
                //    Debug.Log("Confirmation Message" + getMessageID);
                NetConfirmation confirmation = new NetConfirmation((type, getMessageID));
                SendToServer(confirmation.Serialize());
            }

            isConnected = true;
        }
    }


    private void CheckImportantMessageConfirmation((MessageType, ulong) data)
    {
        // Debug.Log($"The message that appeared was {data.Item1} with ID {data.Item2}.");
        foreach (var VARIABLE in lastImportantMessages)
        {
            if (VARIABLE.messageId == data.Item2 && VARIABLE.type == data.Item1 && !VARIABLE.startTimer)
            {
                VARIABLE.startTimer = true;
                VARIABLE.canBeResend = false;
                //Debug.Log($"Message from Server was confirmed {VARIABLE.type} with ID {VARIABLE.messageId}.");
                break;
            }
        }
    }

    public bool IsTheNextMessage(MessageType messageType, MessageCache value, BaseMessage baseMessage)
    {
        Debug.Log($"The id of the message is {value.messageId}");
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }

        if (lastReceiveMessage[messageType].messageId + 1 == value.messageId)
        {
            lastReceiveMessage[messageType] = value;
            CheckPendingMessages(messageType, value.messageId);

            return true;
        }
        else
        {
            Debug.Log($"The message that I need is {lastReceiveMessage[messageType].messageId}");
            pendingMessages.TryAdd(messageType, new List<MessageCache>());
            pendingMessages[messageType].Add(new MessageCache(messageType, value.messageId));
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

    private void SetPlayer(List<Player> newPlayersList)
    {
        Dictionary<string, Player> playerTags = players.ToDictionary(p => p.nameTag, p => p);

        foreach (Player player in newPlayersList)
        {
            bool playerAlreadyExists = playerTags.ContainsKey(player.nameTag);
            foreach (Player currentPlayer in players)
            {
                bool playerExistsInNewPlayersList = newPlayersList.Any(np => np.nameTag == currentPlayer.nameTag);

                if (!playerExistsInNewPlayersList)
                {
                    //BUG:Destroyed other player 
                    Debug.LogWarning($"Destroy Player{currentPlayer.id}");
                    OnPlayerDestroyed.RaiseEvent(currentPlayer.id);
                }
            }

            if (!playerAlreadyExists)
            {
                if (player.nameTag == tagName)
                {
                    clientId = player.id;
                    Debug.Log("Entre por player");
                    OnMyPlayerCreated.RaiseEvent(player.id, player.nameTag);
                    BaseMessage.PlayerID = clientId;
                }
                else
                {
                    OnPlayerCreated.RaiseEvent(player.id, player.nameTag);
                }
            }
        }

        players = newPlayersList;
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    private bool IsTheLastMesagge(MessageType messageType, MessageCache value)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }

        // Debug.Log($"The message id is {value}");
        // Debug.Log($"The last id is {lastReceiveMessage[messageType]}");
        if (lastReceiveMessage[messageType].messageId > value.messageId)
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

    private void SendPosition(Vector3 newPos)
    {
        // NetPlayerPos netPlayerPos = new NetPlayerPos(newPos.ToSystemVector3(), clientId);
        // SendToServer(netPlayerPos.Serialize());
    }
}