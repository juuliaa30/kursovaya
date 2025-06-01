using UnityEngine;
using MyNetwork;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    private bool canInput = false;


    private bool[] inputs;

    private void Start()
    {
        inputs = new bool[5];
    }

    private void Update()
    {
        inputs[0] = Input.GetKey(KeyCode.W);
        inputs[1] = Input.GetKey(KeyCode.S);
        inputs[2] = Input.GetKey(KeyCode.A);
        inputs[3] = Input.GetKey(KeyCode.D);
        inputs[4] = Input.GetKey(KeyCode.LeftShift);
    }

    private void FixedUpdate()
    {
        if (!canInput)
            return;

        bool hasInput = inputs.Any(i => i);
        if (hasInput)
            SendInput();
    }

    public void EnableInput()
    {
        canInput = true;
    }

    #region Messages

    private void SendInput()
    {
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ClientToServerId.input);
        message.AddBools(inputs, false);
        message.AddVector3(camTransform.forward);
        NetworkManager.Singleton.Client.Send(message);
    }

    #endregion
}
