using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public abstract class NetworkManager : MonoBehaviour, IReceiveData
{
    //Todo: Make logic for obligatory messages
    //Todo: Make Basic shooter to test.
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

    public virtual void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        var type = NetByteTranslator.getNetworkType(data);
        var playerID = NetByteTranslator.GetPlayerID(data);
    }

    void Update()
    {
        if (connection != null)
            connection.FlushReceiveData();
        CheckTimeOut(Time.deltaTime);
    }

    protected virtual void CheckTimeOut(float delta)
    {
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

    protected abstract void OnTextAdded(string text);
}