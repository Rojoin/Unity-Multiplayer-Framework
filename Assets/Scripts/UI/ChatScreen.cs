
using System;
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
       
    }

    private void OnDestroy()
    {
        inputMessage.onEndEdit.RemoveAllListeners();
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