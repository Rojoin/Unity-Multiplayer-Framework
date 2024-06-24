using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPosition;
    [SerializeField] private List<PlayerController> players;
    [SerializeField] private GameObject playerPrefab;
   // [SerializeField] private Text timerText;
    public int maxPlayers = 4;
    public float timer = 120;
    private bool isFirstTime = false;
    private PlayerController myPlayer;
    private int currentPlayersConnected = 0;
    [SerializeField] private Vector3ChannelSO OnMyPlayerMovement;
    [SerializeField] private AskForPlayerChannelSo OnPlayerCreated;
    [SerializeField] private AskForPlayerChannelSo OnMyPlayerCreated;
    [SerializeField] private MovePlayerChannelSO OnPlayerMoved;
    [SerializeField] private IntChannelSO OnPlayerDestroyed;
    [SerializeField] private IntChannelSO OnHittedPlayer;
    [SerializeField] private AskforBulletChannelSO askforBulletChannelSo;
    [SerializeField] private FloatChannelSO OnTimerChanged;
    [SerializeField] private VoidChannelSO OnExitChannel;
    [SerializeField] private StringChannelSO OnWinnerChannel;
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
        currentPlayersConnected = 0;
        isFirstTime = true;
     //   timerText.text = "";
    }


    private void ChangeTimer(float obj)
    {
        if (isFirstTime)
        {
            isFirstTime = false;
            SetAllPlayerPos();
        }
        timer -= obj;
        TimeSpan time = TimeSpan.FromSeconds(timer);
        string str = time.ToString(@"hh\:mm\:ss\:fff");
       // timerText.text = str;
    }

    public void SetAllPlayerPos()
    {
        for (int i = 0; i < currentPlayersConnected; i++)
        {
            players[i].SetPosition(spawnPosition[i].position);
        }
    }

    private void OnDisable()
    {
        OnPlayerCreated.Unsubscribe(CreateNewPlayer);
        OnMyPlayerCreated.Unsubscribe(CreateMyNewPlayer);
        OnPlayerMoved.Unsubscribe(SetPlayerPos);
        OnPlayerDestroyed.Unsubscribe(DisconnectPlayer);
        OnExitChannel.Unsubscribe(ResetConfig);
        OnTimerChanged.Unsubscribe(ChangeTimer);
        inputs.OnMoveChannel.RemoveAllListeners();
        isFirstTime = true;
        ResetConfig();
    }

    private void ResetConfig()
    {
        currentPlayersConnected = 0;
        foreach (PlayerController player in players)
        {
            player.GetComponent<PlayerShooting>().OnBulletShoot.RemoveAllListeners();
            player.OnMovement.RemoveAllListeners();
            player.OnHit.RemoveAllListeners();
            if (player != null)
            {
                Destroy(player.gameObject);
            }
        }

        players.Clear();
        isFirstTime = true;
        this.enabled = false;
    }

    private void CreateNewPlayer(int id, string nameTag)
    {
        GameObject newObject = Instantiate(playerPrefab);
        newObject.name = $"Player{currentPlayersConnected}";
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        newPlayer.nameTagPlayer = nameTag;
        newObject.transform.position = spawnPosition[currentPlayersConnected].position;

        players.Add(newPlayer);
        currentPlayersConnected++;
    }

    private void CreateMyNewPlayer(int id, string newNameTag)
    {
        GameObject newObject = Instantiate(playerPrefab);
        newObject.name = $"Player{currentPlayersConnected}";
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        newPlayer.nameTagPlayer = newNameTag;
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
                player.OnMovement.RemoveAllListeners();
                player.OnHit.RemoveAllListeners();
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
            Debug.Log("I got Killed");
            OnHittedPlayer.RaiseEvent(playerID);
        }
    }

    public string GetWinnerString()
    {
        int maxLives = players.Max(p => p.currentHealth);

        var topPlayers = players.Where(p => p.currentHealth == maxLives).ToList();

        if (topPlayers.Count == 1)
        {
            return $"The winner is {topPlayers[0].nameTagPlayer} with {topPlayers[0].currentHealth} lives.";
        }
        else
        {
            var drawPlayers = string.Join(", ", topPlayers.Select(p => p.nameTagPlayer));
            return $"It's a draw between {drawPlayers}, each with {maxLives} lives.";
        }
    }

 
}