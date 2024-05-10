using System;
using ScriptableObjects;
using UnityEngine.UI;

public class ChatScreen : MonoBehaviourSingleton<ChatScreen>
{
    public Text messages;
    public InputField inputMessage;
    public Button exitNetwork;
    public StringChannelSO onTextCreated;
    public VoidChannelSO closeChatScreen;

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

    public void AddText(string textString)
    {
        messages.text += textString + Environment.NewLine;
    }

    void OnEndEdit(string str)
    {
        if (inputMessage.text != "")
        {
            onTextCreated.RaiseEvent(inputMessage.text);
            
            inputMessage.ActivateInputField();
            inputMessage.Select();
            inputMessage.text = "";
        }
    }

    public void SwitchToNetworkScreen()
    {
        // Todo: Add Server Disconnect
        closeChatScreen.RaiseEvent();
        NetworkScreen.Instance.gameObject.SetActive(true);
        this.gameObject.SetActive(false);
    }
}