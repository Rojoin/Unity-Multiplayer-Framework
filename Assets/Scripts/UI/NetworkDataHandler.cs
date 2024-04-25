using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkDataHandler : MonoBehaviourSingleton<NetworkDataHandler>
{
    public ChatScreen chat;

    private float lastTime =0.0f;
    private float currentTime=0.0f;
    protected override void Initialize()
    {
        NetworkManager.Instance.OnReceiveEvent += OnReceiveDataEvent;
    }

    void OnReceiveDataEvent(byte[] data, IPEndPoint ep, int id)
    {
        var type = NetByteTranslator.getNetworkType(data);
        var playerID = NetByteTranslator.GetPlayerID(data);
        if (NetworkManager.Instance.isServer)
        {
            switch (type)
            {
                case MessageType.HandShake:
                    NetHandShake handShake = new NetHandShake();
                    string gameTag = handShake.Deserialize(data);
                    Debug.Log("La ip de el cliente es: " + ep.Address + " y el nameTag es: " + gameTag);
                    NetworkManager.Instance. AddClient(ep, out id, gameTag);
                    NetHandShakeOK handOK = new NetHandShakeOK(NetworkManager.Instance.players);
                    NetworkManager.Instance.Broadcast(handOK.Serialize());
                    
                    //Todo mandar Ping 
                    NetPing ping = new NetPing();
                    lastTime = Time.time;
                    NetworkManager.Instance.SendToClient(ping.Serialize(),gameTag,ep);
                    

                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String:
                    NetConsole message = new();
                    chat.AddText(message.Deserialize(data), NetByteTranslator.GetPlayerID(data));
                    NetworkManager.Instance.Broadcast(data);
                    break;
                case MessageType.Exit:
                    NetworkManager.Instance.RemoveClient(ep);
                    break;
                case MessageType.HandShakeOk:
                    break;
                case MessageType.Ping:
                    break;
                case MessageType.Pong:
                    NetPing pingMessage = new NetPing();
                    NetPong pongMessage = new NetPong();
                    NetworkManager.Instance.SendToClient(pingMessage.Serialize(),pingMessage.Deserialize(data),ep);
                    currentTime = Time.time;
                    var a = currentTime - lastTime;
                    Debug.Log("Pong with " + pongMessage.Deserialize(data) + "in " + a + "ms" );
                    lastTime = currentTime;
                    break;
            }
        }

        else
        {
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
                    Debug.Log("MessageType is String");
                    Debug.Log(NetByteTranslator.GetPlayerID(data));
                    chat.AddText(message.Deserialize(data), NetByteTranslator.GetPlayerID(data));
                    break;
                case MessageType.HandShakeOk:
                    NetHandShakeOK handOk = new NetHandShakeOK();
                    List<Player> players = handOk.Deserialize(data);
                    NetworkManager.Instance.SetPlayer(players);
                    foreach (Player pl in NetworkManager.Instance.players)
                    {
                        Debug.Log("This is " + pl.nameTag + "with id:" + pl.id);
                    }
                    
                    Debug.Log("My id is" + NetworkManager.Instance.clientId);

                    break;
                case MessageType.Exit:
                    break;
                case MessageType.Ping:
                    Debug.Log("Ping");
                    NetPong netPong = new NetPong();
                    NetworkManager.Instance.SendToServer(netPong.Serialize());
                    currentTime = Time.time;
                    var a = currentTime - lastTime;
                    Debug.Log("Ping in " + a + "ms" );
                    lastTime = currentTime;
                    break;
                case MessageType.Pong:
                    break;
                default:
                    Debug.Log("MessageType not found");
                    break;
            }
        }
//TODO: Mover ChatScreen aca
//TODO: Fijarse el cliente

        //void Network 
    }
}