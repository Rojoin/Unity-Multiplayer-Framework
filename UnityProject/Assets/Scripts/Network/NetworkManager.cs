using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RojoinNetworkSystem;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public abstract class NetworkManager : MonoBehaviour ,IReceiveData
{
    //Todo: Add ScrollView to see messages below
  
    public IPAddress ipAddress { get; set; }
    public int port { get; set; }

    public int timeOut = 30;

    public UdpConnection connection;

    public List<Player> players = new List<Player>();
    public string tagName = "";

    public int clientId = 0;

    public UnityEvent<Client> OnPlayerDisconnect;
    public UnityEvent OnServerDisconnect;
    public UnityEvent OnNewPlayer;
    public UnityEvent<string> OnChatMessage;
    public StringChannelSO OnMessageCreatedChannel;
    public VoidChannelSO OnCloseNetworkChannel;
    public AskForPlayerChannelSo OnPlayerCreated;
    public MovePlayerChannelSO OnPlayerMoved;
    public IntChannelSO OnPlayerDestroyed;
    public AskforBulletChannelSO AskforBulletChannelSo;
    [SerializeField] public AskforBulletChannelSO OnCreatedBullet;
    protected List<MessageCache> lastImportantMessages = new();
    private List<GameObject> entities;
    public StringChannelSO OnErrorMessage;
    public FloatChannelSO OnTimerChanged;
    public UnityEvent<MessageCache> OnResendMessage = new();

    protected virtual void OnEnable()
    {
        OnConnect();
    }

    protected virtual void OnConnect()
    {
        OnMessageCreatedChannel.Subscribe(OnTextAdded);
        OnCloseNetworkChannel.Subscribe(Deactivate);
        OnResendMessage.AddListener(ReSendMessage);
       
        lastImportantMessages.Clear();
        players.Clear();
        clientId = 0;
    }

    protected abstract void ReSendMessage(MessageCache arg0);


    protected void OnDisable()
    {
        
        OnDisconect();
    }

    private void Deactivate() => this.enabled = false;
    public abstract void CloseConnection();

    protected virtual void OnDisconect()
    {
        OnMessageCreatedChannel.Unsubscribe(OnTextAdded);
        OnCloseNetworkChannel.Unsubscribe(Deactivate);
        OnPlayerDisconnect.RemoveAllListeners();
        OnServerDisconnect.RemoveAllListeners();
        OnResendMessage.RemoveAllListeners();
    }


    public void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        OnReceiveDataEvent(data, ip);
    }

    public abstract void OnReceiveDataEvent(byte[] data, IPEndPoint ep);

    void Update()
    {
        float deltaTime = Time.deltaTime;
        CheckTimeOut(deltaTime);
        CheckLastImportantMessages(deltaTime);
        OnUpdate(deltaTime);
        
        
        if (connection != null)
            connection.FlushReceiveData();
    }

    protected abstract void OnUpdate(float deltaTime);

    protected abstract void CheckTimeOut(float delta);

    protected virtual void CheckLastImportantMessages(float deltaTime)
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


    [FormerlySerializedAs("messageTimer")] public float timeUntilResend = 15;

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

    protected abstract void OnTextAdded(string text);
    protected abstract void CouldntCreateUDPConnection(string errorMessage);
}