using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    
    [SerializeField] private List<Transform> spawnPosition;
    [SerializeField] private  List<PlayerController> players;
    [SerializeField] private GameObject playerPrefab;
    public int maxPlayers = 4;
    private int currentPlayersConnected = 0;
    public IntChannelSO OnPlayerCreated;
    public MovePlayerChannelSO OnPlayerMoved;
    public IntChannelSO OnPlayerDestroyed;


    private void OnEnable()
    {
        OnPlayerCreated.Subscribe(CreateNewPlayer);
        OnPlayerMoved.Subscribe(SetPlayerPos);
        OnPlayerDestroyed.Subscribe(DisconnectPlayer);
    }

    private void OnDisable()
    {
        OnPlayerCreated.Unsubscribe(CreateNewPlayer);
        OnPlayerMoved.Unsubscribe(SetPlayerPos);
        OnPlayerDestroyed.Unsubscribe(DisconnectPlayer);
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
                playerController.transform.position = newPos;
            }
        }
    }
    
    
}