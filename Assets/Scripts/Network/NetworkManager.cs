using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public abstract class NetworkManager : MonoBehaviour, IReceiveData
{
    //TODO: Make logic for obligatory messages
    //TODO: Make Basic shooter to test.
    //TODO: Everytime an object is created it needs to wait for his id
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

    private List<MessageCache> lastImportantMessages;
    private List<GameObject> entities;
    public StringChannelSO OnErrorMessage;
    protected virtual void OnEnable()
    {
        OnConnect();
    }

    protected virtual void OnConnect()
    {
        OnMessageCreatedChannel.Subscribe(OnTextAdded);
        OnCloseNetworkChannel.Subscribe(Deactivate);
    }

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
    }


    public void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        OnReceiveDataEvent(data, ip);
    }

    public abstract void OnReceiveDataEvent(byte[] data, IPEndPoint ep);

    void Update()
    {
        if (connection != null)
            connection.FlushReceiveData();
        CheckTimeOut(Time.deltaTime);
    }

    protected abstract void CheckTimeOut(float delta);

    protected virtual void CheckLastImportantMessages(float deltaTime)
    {
        if (lastImportantMessages.Count >0)
        {
            foreach (MessageCache VARIABLE in lastImportantMessages.ToList())
            {
                VARIABLE.timer += deltaTime;
                if (VARIABLE.timer >= messageTimer)
                {
                    lastImportantMessages.Remove(VARIABLE);
                }
            }
        }
    }

    public float messageTimer = 15;

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

    protected abstract void OnTextAdded(string text);
    protected abstract void CouldntCreateUDPConnection(string errorMessage);
}