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
        NetHandShake.SetPlayerId(10);
        NetHandShake message = new NetHandShake(11);
        
        Debug.Log(message.GetID());
        NetConsole me = new NetConsole();
        
        Debug.Log(me.GetID());
        
        inputMessage.onEndEdit.AddListener(OnEndEdit);

        this.gameObject.SetActive(false);
        //TODO: Buscar donde mandan la data.
    }

    private void OnDestroy()
    {
        inputMessage.onEndEdit.RemoveAllListeners();
    }

    public void AddText(string textString, int id)
    { 
        string idName = id != -10 ? "Player " + id + ":" : "Server:";
        messages.text += idName + textString + System.Environment.NewLine;
    }
    void OnEndEdit(string str)
    {
        if (inputMessage.text != "")
        {
            if (NetworkManager.Instance.isServer)
            {
                NetConsole message = new(inputMessage.text);
                AddText(inputMessage.text,-10);
                NetworkManager.Instance.Broadcast(message.Serialize());
                
            }
            else
            {
                NetConsole message = new(inputMessage.text);
                NetworkManager.Instance.SendToServer(message.Serialize());
            }

            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }
}