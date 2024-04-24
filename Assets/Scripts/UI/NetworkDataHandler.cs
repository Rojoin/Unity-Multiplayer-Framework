using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkDataHandler : MonoBehaviourSingleton<NetworkDataHandler>
{
    public ChatScreen chat;

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

                    //Todo darle nueva lista de clientes.
                    //Hacer handshake OK 


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
                case MessageType.HandShakeOK:
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