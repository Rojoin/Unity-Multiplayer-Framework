using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPosition;
    [SerializeField] private List<PlayerController> players;
    [SerializeField] private GameObject playerPrefab;
   [SerializeField] private Text timerText;
    public int maxPlayers = 4;
    public float timer = 120;
    private PlayerController myPlayer;
    private int currentPlayersConnected = 0;
    public Vector3ChannelSO OnMyPlayerMovement;
    public IntChannelSO OnPlayerCreated;
    public IntChannelSO OnMyPlayerCreated;
    public MovePlayerChannelSO OnPlayerMoved;
    public IntChannelSO OnPlayerDestroyed;
    public IntChannelSO OnHittedPlayer;
    public AskforBulletChannelSO askforBulletChannelSo;
    public FloatChannelSO OnTimerChanged;

    public VoidChannelSO OnExitChannel;
    [Header("GameInputs")]
    public InputController inputs;


    private void OnEnable()
    {
        timer = 120;
        OnPlayerCreated.Subscribe(CreateNewPlayer);
        OnMyPlayerCreated.Subscribe(CreateMyNewPlayer);
        OnPlayerMoved.Subscribe(SetPlayerPos);
        OnPlayerDestroyed.Subscribe(DisconnectPlayer);
        OnExitChannel.Subscribe(ResetConfig);
        OnTimerChanged.Subscribe(ChangeTimer);
    }

    private void ChangeTimer(float obj)
    {
        timer -= obj;
        TimeSpan time = TimeSpan.FromSeconds(timer);
        string str = time.ToString(@"hh\:mm\:ss\:fff");
        timerText.text = str;
    }

    private void OnDisable()
    {
        OnPlayerCreated.Unsubscribe(CreateNewPlayer);
        OnMyPlayerCreated.Unsubscribe(CreateMyNewPlayer);
        OnPlayerMoved.Unsubscribe(SetPlayerPos);
        OnPlayerDestroyed.Unsubscribe(DisconnectPlayer);
        OnExitChannel.Unsubscribe(ResetConfig);
        inputs.OnMoveChannel.RemoveAllListeners();
    }

    private void ResetConfig()
    {
        currentPlayersConnected = 0;
        foreach (PlayerController player in players)
        {
            Destroy(player.gameObject);
        }

        players.Clear();
    }

    private void CreateNewPlayer(int id)
    {
        GameObject newObject = Instantiate(playerPrefab);
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        newObject.transform.position = spawnPosition[currentPlayersConnected].position;

        players.Add(newPlayer);
        currentPlayersConnected++;
    }

    private void CreateMyNewPlayer(int id)
    {
        GameObject newObject = Instantiate(playerPrefab);
        newObject.name = $"Player{currentPlayersConnected}";
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        myPlayer = newPlayer;
        newObject.transform.position = spawnPosition[currentPlayersConnected].position;
        inputs.OnMoveChannel.AddListener(newPlayer.Move);
        
        newPlayer.GetComponent<PlayerShooting>().OnBulletShoot.AddListener(AskForBullet);
        
        newPlayer.OnMovement.AddListener(MovePlayerPos);
        
        Debug.Log($"The player with {id} has been subscribed.");
        newPlayer.OnHit.AddListener(OnPlayerHit);
        players.Add(newPlayer);
        currentPlayersConnected++;
    }

    private void AskForBullet(Transform trans)
    {
        Debug.Log(trans.name);
        askforBulletChannelSo.RaiseEvent(1, trans.position, trans.forward);
    }

    public void DisconnectPlayer(int id)
    {
        foreach (PlayerController player in players.ToList())
        {
            if (player.id == id)
            {
                players.Remove(player);
                Destroy(player.gameObject);
                currentPlayersConnected--;
            }
        }
    }

    public void SetPlayerPos(int id, Vector3 newPos)
    {
        foreach (PlayerController playerController in players)
        {
            if (playerController.id == id)
            {
                playerController.SetPosition(newPos);
            }
        }
    }

    private void MovePlayerPos(Vector3 arg0)
    {
        OnMyPlayerMovement.RaiseEvent(arg0);
    }

    private void OnPlayerHit(int playerID)
    {
        if (playerID == myPlayer.id)
        {
            OnHittedPlayer.RaiseEvent(playerID);
        }
    }
}