using UnityEngine;
using MyNetwork;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform camProxy;

    [Header("Movement Settings")]
    [SerializeField] private float thrustForce = 50f;
    [SerializeField] private float rotationSpeed = 80f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Tilt Settings")]
    [SerializeField] private float tiltAngle = 25f;
    [SerializeField] private float tiltSpeed = 4f;
    [SerializeField] private float driftFactor = 0.85f;

    private float currentTilt;
    private bool[] inputs;
    private Vector3 physicsVelocity;
    private bool canMove = false;

    private void Start()
    {
        inputs = new bool[5];
        rb.maxAngularVelocity = 5f;
    }

    private void FixedUpdate()
    {
        if (!enabled) return;
        Vector2 inputDirection = GetInputDirection();
        ProcessMovement(inputDirection);
        ProcessRotation(inputDirection);
        ApplyTiltEffect(inputDirection);
        LimitVelocity();
        SendMovement();
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    private Vector2 GetInputDirection()
    {
        Vector2 direction = Vector2.zero;
        if (inputs[0]) direction.y += 1;
        if (inputs[1]) direction.y -= 1;
        if (inputs[2]) direction.x -= 1;
        if (inputs[3]) direction.x += 1;
        return direction;
    }

    private void ProcessMovement(Vector2 inputDirection)
    {
        float thrust = inputDirection.y * thrustForce;
        if (inputs[4]) thrust *= sprintMultiplier;

        Vector3 force = transform.forward * thrust;
        rb.AddForce(force, ForceMode.Acceleration);

        if (inputDirection.x != 0)
        {
            float driftForce = thrustForce * 0.3f * inputDirection.x;
            rb.AddForce(transform.right * driftForce, ForceMode.Acceleration);
        }
    }

    private void ProcessRotation(Vector2 inputDirection)
    {
        float rotationTorque = inputDirection.x * rotationSpeed;
        rb.AddTorque(transform.up * rotationTorque, ForceMode.Acceleration);
    }

    private void ApplyTiltEffect(Vector2 inputDirection)
    {
        float targetTiltZ = -inputDirection.x * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTiltZ, tiltSpeed * Time.fixedDeltaTime);

        float targetTiltX = -inputDirection.y * tiltAngle * 0.5f;

        camProxy.localRotation = Quaternion.Lerp(
            camProxy.localRotation,
            Quaternion.Euler(targetTiltX, 0, currentTilt),
            tiltSpeed * Time.fixedDeltaTime
        );
    }

    private void LimitVelocity()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    public void SetInput(bool[] inputs, Vector3 forward)
    {
        this.inputs = inputs;
        camProxy.forward = forward;
    }

    private void SendMovement()
    {
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.playerMovement);
        message.AddUShort(player.Id);
        message.AddVector3(transform.position);
        message.AddVector3(transform.forward);
        message.AddVector3(rb.velocity);
        message.AddVector3(rb.angularVelocity);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("TrackObstacle"))
        {
            Debug.Log($"[Collision] Player {player.Id} collided with {collision.gameObject.name} at position: {transform.position}");
            RaceManager.Singleton.RespawnPlayer(player);
        }
    }
}