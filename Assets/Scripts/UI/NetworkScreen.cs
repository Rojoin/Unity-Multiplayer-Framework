using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;


public class NetworkScreen : MonoBehaviourSingleton<NetworkScreen>
{
    public Button connectBtn;
    public Button startServerBtn;
    public InputField portInputField;
    public InputField addressInputField;
    public InputField nameTagInputField;

    public ClientNetManager client;
    public ServerNetManager server;
    public CanvasGroup loadingScreen;
    public CanvasGroup loginScreen;

    private bool isLoginCanvasActive = false;
    
    protected override void Initialize()
    {
        connectBtn.onClick.AddListener(OnConnectBtnClick);
        startServerBtn.onClick.AddListener(OnStartServerBtnClick);
        ToggleLoadScreen();
    }

    private void OnDestroy()
    {
        connectBtn.onClick.RemoveListener(OnConnectBtnClick);
        startServerBtn.onClick.RemoveListener(OnStartServerBtnClick);
    }

    void OnConnectBtnClick()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        client.tagName = nameTagInputField.text;
        client.ipAddress = ipAddress;
        client.port = port;
        client.enabled = true;

        ToggleLoadScreen();
    }

    public void ToggleLoadScreen()
    {
        isLoginCanvasActive = !isLoginCanvasActive;
        loadingScreen.SetCanvasActive(!isLoginCanvasActive);
        loginScreen.SetCanvasActive(isLoginCanvasActive);
    }

    void OnStartServerBtnClick()
    {
        int port = System.Convert.ToInt32(portInputField.text);
        server.port = port;
        server.enabled = true;
        SwitchToChatScreen();
    }

    public void SwitchToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        ToggleLoadScreen();
        this.gameObject.SetActive(false);
    }
}