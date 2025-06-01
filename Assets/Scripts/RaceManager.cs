using UnityEngine;
using MyNetwork;
using System.Collections;
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    private static RaceManager _singleton;
    public static RaceManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(RaceManager)}: instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    private Dictionary<ushort, int> playerLaps = new Dictionary<ushort, int>();
    private int totalLaps = 1;
    private bool raceFinished;
    private ushort winnerId;

    private int requiredPlayers = 2;
    private float countdownTime = 5f;

    private bool raceStarted;
    private Coroutine countdownCoroutine;
    private readonly HashSet<ushort> readyPlayers = new HashSet<ushort>();

    private void Awake()
    {
        Singleton = this;
    }

    public void RespawnPlayer(Player player)
    {
        Vector3 respawnPosition = player.Id % 2 == 0
            ? new Vector3(-40f, -0.7f, 138f)
            : new Vector3(-40f, -0.7f, 132f);

        Quaternion respawnRotation = Quaternion.Euler(0f, -90f, 0f);

        player.transform.SetPositionAndRotation(respawnPosition, respawnRotation);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.PlayerRespawned);
        message.AddUShort(player.Id);
        message.AddVector3(respawnPosition);
        message.AddQuaternion(respawnRotation);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    public void OnPlayerCrossedFinishLine(Player player)
    {

        if (!playerLaps.ContainsKey(player.Id))
        {
            playerLaps[player.Id] = 0;
        }

        playerLaps[player.Id]++;
        Debug.Log($"Player {player.Id} ({player.Username}) completed lap {playerLaps[player.Id]}/{totalLaps}");

        if (playerLaps[player.Id] >= totalLaps)
        {
            raceFinished = true;
            winnerId = player.Id;
            string winnerName = Player.list[winnerId].Username;
            Debug.Log($"Player {winnerId} ({winnerName}) won the race!");
            SendRaceFinished(winnerId);
        }
    }

    public void OnPlayerConnect(Player player)
    {
        if (raceStarted) return;

        readyPlayers.Add(player.Id);
        Debug.Log($"Player {player.Id} connected. Ready: {readyPlayers.Count}/{requiredPlayers}");

        if (readyPlayers.Count >= requiredPlayers)
        {
            StartRaceCountdown();
        }
    }

    private void StartRaceCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        foreach (var player in Player.list.Values)
        {
            player.Movement.enabled = false;
        }

        SendRaceStateUpdate("WAITING");
        yield return new WaitForSeconds(1f);

        for (int i = 3; i > 0; i--)
        {
            SendCountdownUpdate(i);
            SendRaceStateUpdate($"STARTING IN {i}");
            yield return new WaitForSeconds(1f);
        }

        SendRaceStateUpdate("GO!");
        raceStarted = true;

        foreach (var player in Player.list.Values)
        {
            player.Movement.enabled = true;
        }

        Debug.Log("Race started! Players can move now");
    }

    private void StartRace()
    {
        SendRaceStateUpdate("GO!");

        foreach (var player in Player.list.Values)
        {
            player.Movement.EnableMovement();
        }
    }

    private void SendCountdownUpdate(int secondsLeft)
    {
        Message msg = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.CountdownUpdate);
        msg.AddInt(secondsLeft);
        NetworkManager.Singleton.Server.SendToAll(msg);
    }

    private void SendRaceStateUpdate(string message)
    {
        Message msg = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.RaceStateUpdate);
        msg.AddString(message);
        NetworkManager.Singleton.Server.SendToAll(msg);
    }

    private void SendRaceFinished(ushort winnerId)
    {
        Message msg = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.RaceFinished);
        msg.AddUShort(winnerId);
        NetworkManager.Singleton.Server.SendToAll(msg);
    }
}