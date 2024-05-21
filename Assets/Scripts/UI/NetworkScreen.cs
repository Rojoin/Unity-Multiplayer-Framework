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
    public InputField timerForGameField;
    public InputField addressInputField;
    public InputField nameTagInputField;

    public ClientNetManager client;
    public ServerNetManager server;
    public GameManager gameManager;
    public CanvasGroup loadingScreen;
    public CanvasGroup loginScreen;
    public Text errorDisplayText;

    [SerializeField] private float errorTime = 5.0f;
    [SerializeField] private StringChannelSO errorChannel;

    private bool isLoginCanvasActive = false;
    private Coroutine isErrorShowing;

    protected override void Initialize()
    {
        connectBtn.onClick.AddListener(OnConnectBtnClick);
        startServerBtn.onClick.AddListener(OnStartServerBtnClick);
        errorChannel.Subscribe(StartErrorShowing);
        ToggleLoadScreen();
    }

    private void OnDestroy()
    {
        connectBtn.onClick.RemoveListener(OnConnectBtnClick);
        startServerBtn.onClick.RemoveListener(OnStartServerBtnClick);
        errorChannel.Unsubscribe(StartErrorShowing);
    }

    void OnConnectBtnClick()
    {
        if (string.IsNullOrWhiteSpace(nameTagInputField.text))
            return;

        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        client.tagName = nameTagInputField.text;
        client.ipAddress = ipAddress;
        client.port = port;
        client.enabled = true;
        gameManager.enabled = true;
        ToggleLoadScreen();
    }

    public void ToggleLoadScreen()
    {
        isLoginCanvasActive = !isLoginCanvasActive;
        loadingScreen.SetCanvasActive(!isLoginCanvasActive);
        loginScreen.SetCanvasActive(isLoginCanvasActive);
    }

    public void SetLoginScreen(bool state = true)
    {
        isLoginCanvasActive = state;
        loadingScreen.SetCanvasActive(!isLoginCanvasActive);
        loginScreen.SetCanvasActive(isLoginCanvasActive);
    }

    void OnStartServerBtnClick()
    {
        SwitchToChatScreen();
        int port = System.Convert.ToInt32(portInputField.text);
        int waitTimer = System.Convert.ToInt32(timerForGameField.text);
        int minTimer = 10;
        server.port = port;
        server.timerUntilStart = waitTimer <= minTimer ? minTimer : waitTimer;
        gameManager.enabled = true;
        server.enabled = true;
    }

    public void SwitchToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        ToggleLoadScreen();
        this.gameObject.SetActive(false);
    }

    private void StartErrorShowing(string errorMessage)
    {
        if (isErrorShowing != null)
        {
            StopCoroutine(isErrorShowing);
        }

        isErrorShowing = StartCoroutine(ErrorMessageDisplay(errorMessage));
    }

    IEnumerator ErrorMessageDisplay(string errorMessage)
    {
        errorDisplayText.text = errorMessage;
        yield return new WaitForSeconds(errorTime);
        errorDisplayText.text = "";
    }
}