
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class ChatScreen : MonoBehaviourSingleton<ChatScreen>
{
    public Text messages;
    public InputField inputMessage;

    protected override void Initialize()
    {
        inputMessage.onEndEdit.AddListener(OnEndEdit);

        this.gameObject.SetActive(false);
        //TODO: Buscar donde mandan la data.
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
                messages.text += message.Deserialize(data) + System.Environment.NewLine;
                break;
            default:
                Debug.Log("MessageType not found");
                break;

        }


    }

    void OnEndEdit(string str)
    {
        if (inputMessage.text != "")
        {
            NetConsole message = new(inputMessage.text);
            if (NetworkManager.Instance.isServer)
            {
                NetworkManager.Instance.Broadcast(message.Serialize());
                messages.text += inputMessage.text + System.Environment.NewLine;
            }
            else
            {
                NetworkManager.Instance.SendToServer(message.Serialize());
            }

            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }

    }

}

