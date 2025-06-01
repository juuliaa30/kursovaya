using MyNetwork;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public bool IsLocal { get; private set; }
    private string username;
    [SerializeField] private Transform camTransform;

    private void Move(Vector3 newPosition, Vector3 forward)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.MovePosition(newPosition);
            rb.MoveRotation(Quaternion.LookRotation(forward));
        }
        else
        {
            transform.position = newPosition;
            transform.forward = forward;
        }

        if (!IsLocal)
        {
            camTransform.forward = forward;
        }
    }

    private void OnDestroy()
    {
        list.Remove(Id);
    }

    public static void Spawn(ushort id, string username, Vector3 position)
    {
        Player player;

        if (id == NetworkManager.Singleton.Client.Id)
        {
            player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocal = true;
        }
        else
        {
            player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocal = false;
        }

        player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        

        player.name = $"PLayer {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";

        player.Id = id;
        player.username = username;

        list.Add(id, player);

    }

    #region Messages
    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void SpawnPlayer(Message message)
    {
        ushort id = message.GetUShort();
        string username = message.GetString();
        Vector3 position = message.GetVector3();

        Spawn(id, username, position);
    }

    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMovement(Message message)
    {
        ushort id = message.GetUShort();

        if (list.TryGetValue(id, out Player player))
        {
            Vector3 position = message.GetVector3();
            Vector3 forward = message.GetVector3();

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(position);
                rb.MoveRotation(Quaternion.LookRotation(forward));
                rb.velocity = message.GetVector3();
                rb.angularVelocity = message.GetVector3();
            }
            else
            {
                player.transform.position = position;
                player.transform.forward = forward;
            }
        }
        else
        {
            Debug.LogWarning($"Player {id} not found in local list");
        }
    }

    [MessageHandler((ushort)ServerToClientId.PlayerRespawned)]
    private static void HandleRespawn(Message message)
    {
        ushort playerId = message.GetUShort();
        Vector3 position = message.GetVector3();
        Quaternion rotation = message.GetQuaternion();

        if (Player.list.TryGetValue(playerId, out Player player))
        {
            player.transform.SetPositionAndRotation(position, rotation);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    [MessageHandler((ushort)ServerToClientId.RaceFinished)]
    private static void HandleRaceFinish(Message message)
    {
        ushort winnerId = message.GetUShort();
        string winnerName = list[winnerId].username;

        Debug.Log($"Race finished! Winner: {winnerName}");
        UIManager.Singleton.ShowWinner(winnerName);
    }

    [MessageHandler((ushort)ServerToClientId.CountdownUpdate)]
    private static void HandleCountdown(Message message)
    {
        int secondsLeft = message.GetInt();
        UIManager.Singleton.UpdateCountdown(secondsLeft);
    }

    #endregion
}
