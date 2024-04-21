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
                    NetHandShake handShake = new NetHandShake(id);
                    Debug.Log("La ip de el cliente es: " + ep.Address + " y el ID es: " + id);
                    NetworkManager.Instance.SendToClient(handShake.Serialize(), ep);
                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String:
                    NetworkManager.Instance.Broadcast(data);
                    break;
                case MessageType.Exit:
                    NetworkManager.Instance.RemoveClient(ep);
                    break;
            }
        }

        if (NetworkManager.Instance.connection.playerId == -1 && type ==  MessageType.HandShake && !NetworkManager.Instance.isServer)
        {
            NetHandShake handShake = new NetHandShake();
            NetworkManager.Instance.connection.playerId = handShake.Deserialize(data);
            Debug.Log("New ID is: " + NetworkManager.Instance.connection.playerId);
        }

        if (NetworkManager.Instance.connection.playerId == playerID ||
            NetworkManager.Instance.connection.playerId == -2)
        {
            switch (type)
            {
                case MessageType.HandShake:
                    //TODO: YA no tengo que mandar handShake
                    if (!NetworkManager.Instance.isServer)
                    {
                        Debug.Log("My ID is: " + NetworkManager.Instance.connection.playerId);
                    }

                    break;
                case MessageType.Console:
                    break;
                case MessageType.Position:
                    break;
                case MessageType.String:
                    NetConsole message = new();
                    Debug.Log("MessageType is String");
                    chat.messages.text +=  message.Deserialize(data) + System.Environment.NewLine;
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