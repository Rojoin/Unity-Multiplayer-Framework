using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class ChatScreen : MonoBehaviourSingleton<ChatScreen>
{
    public Text messages;
    public InputField inputMessage;
    public Button exitNetwork;
    protected override void Initialize()
    {
        
        inputMessage.onEndEdit.AddListener(OnEndEdit);

        this.gameObject.SetActive(false);
        exitNetwork.onClick.AddListener(SwitchToNetworkScreen);

    }

    private void OnDestroy()
    {
        inputMessage.onEndEdit.RemoveAllListeners();
    }

    public void AddText(string textString, int id)
    { 
        string idName = id != -10 ? NetworkManager.Instance.GetPlayer(id).nameTag + ":" : "Server:";
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
    public void SwitchToNetworkScreen()
    {
        NetworkManager.Instance.OnServerDisconnect.Invoke();
        NetworkScreen.Instance.gameObject.SetActive(true);
        this.gameObject.SetActive(false);
    }
}