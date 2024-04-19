using System.Net;
using UnityEngine;

public class NetworkDataHandler : MonoBehaviourSingleton<NetworkDataHandler>
{
    public ChatScreen chat;

    protected override void Initialize()
    {
        NetworkManager.Instance.OnReceiveEvent += OnReceiveDataEvent;
    }
    
    void OnReceiveDataEvent(byte[] data, IPEndPoint ep)
    {
        var type = NetByteTranslator.getNetworkType(data);
        if (NetworkManager.Instance.isServer)
        {
            switch (type)
            {
                case MessageType.HandShake:
                    NetHandShake handShake = new NetHandShake(0,1);
                    //TODO: Mandar el HandShake de vuelta.
                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String:
                    NetworkManager.Instance.Broadcast(data);
                    break;
                default:
                    break;
            }
        }
        switch (type)
        {
            case MessageType.HandShake:
                //TODO: YA no tengo que mandar handShake

                break;
            case MessageType.Console:
                break;
            case MessageType.Position:
                break;
            case MessageType.String:
                NetConsole message = new();
                Debug.Log("MessageType is String");
                chat.messages.text += message.Deserialize(data) + System.Environment.NewLine;
                break;
            default:
                Debug.Log("MessageType not found");
                break;

        }
//TODO: Mover ChatScreen aca
//TODO: Fijarse el cliente

        //void Network 

    }
}