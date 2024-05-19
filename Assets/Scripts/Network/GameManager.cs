using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPosition;
    [SerializeField] private List<PlayerController> players;
    [SerializeField] private GameObject playerPrefab;
    public int maxPlayers = 4;
    private int currentPlayersConnected = 0;
    public Vector3ChannelSO OnMyPlayerMovement;
    public IntChannelSO OnPlayerCreated;
    public IntChannelSO OnMyPlayerCreated;
    public MovePlayerChannelSO OnPlayerMoved;
    public IntChannelSO OnPlayerDestroyed;
    public VoidChannelSO OnExitChannel;
    [Header("GameInputs")]
    public InputController inputs;


    private void OnEnable()
    {
        OnPlayerCreated.Subscribe(CreateNewPlayer);
        OnMyPlayerCreated.Subscribe(CreateMyNewPlayer);
        OnPlayerMoved.Subscribe(SetPlayerPos);
        OnPlayerDestroyed.Subscribe(DisconnectPlayer);
        OnExitChannel.Subscribe(ResetConfig);
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

    public void CreateNewPlayer(int id)
    {
        GameObject newObject = Instantiate(playerPrefab);
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        newObject.transform.position = spawnPosition[currentPlayersConnected].position;

        players.Add(newPlayer);
        currentPlayersConnected++;
    }

    public void CreateMyNewPlayer(int id)
    {
        GameObject newObject = Instantiate(playerPrefab);
        PlayerController newPlayer = newObject.GetComponent<PlayerController>();
        newPlayer.id = id;
        newObject.transform.position = spawnPosition[currentPlayersConnected].position;
        inputs.OnMoveChannel.AddListener(newPlayer.Move);
        newPlayer.OnMovement.AddListener(MovePlayerPos);
        players.Add(newPlayer);
        currentPlayersConnected++;
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
        Debug.Log("Set player");
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
}