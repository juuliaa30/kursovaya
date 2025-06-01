using MyNetwork;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public string Username {  get; private set; }

    public PlayerMovement Movement => movement; 

    [SerializeField] private PlayerMovement movement;


    private void OnDestroy()
    {
        list.Remove(Id);
    }

    public static void Spawn(ushort id, string username)
    {
        foreach (Player otherPlayer in list.Values)
            otherPlayer.SpawnedMessage(id);
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.Username = string.IsNullOrEmpty(username) ? "Guest" : username;

        if (player.Id % 2 == 0)
            player.transform.position = new Vector3(-40f, -0.7f, 138f);
        else
            player.transform.position = new Vector3(-40f, -0.7f, 132f);

        player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        list.Add(id, player);
        player.SpawnedMessage();
        RaceManager.Singleton?.OnPlayerConnect(player);
    }

    #region Messages
    private void SpawnedMessage()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.playerSpawned)));
    }

    private void SpawnedMessage(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.playerSpawned)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position);
        return message;
    }

    [MessageHandler((ushort)ClientToServerId.name)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    [MessageHandler((ushort)ClientToServerId.input)]
    private static void Input(ushort fromClientId, Message message) 
    {
        if (list.TryGetValue(fromClientId, out Player player))
            player.Movement.SetInput(message.GetBools(5), message.GetVector3());
    }

    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void HandlePlayerMovement(Message message)
    {
        ushort id = message.GetUShort();
        if (Player.list.TryGetValue(id, out Player player))
        {
            Vector3 position = message.GetVector3();
            Vector3 forward = message.GetVector3();

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.MovePosition(position);
                rb.MoveRotation(Quaternion.LookRotation(forward));
            }
            else
            {
                player.transform.position = position;
                player.transform.forward = forward;
            }
        }
    }

    #endregion
}
