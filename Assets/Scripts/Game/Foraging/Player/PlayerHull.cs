using Unity.Mathematics;
using UnityEngine;

public class PlayerHull : MonoBehaviour, IPausable
{
    #region fields
    [Header("References")]
    [SerializeField] private Rigidbody2D playerRigidBody;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PauseController pauseController;
    public PlayerStats curPlayerStats;
    
    [Header("Movement")]
    [SerializeField] private float frictionCoefficient = 1f;
    [SerializeField] private float minAxisSpeed = 0.01f;
    private Vector2 moveInput;

    #endregion


    #region getters n setters

    public void SetMoveInput(Vector2 value)
    {
        moveInput = value;
        if (value != Vector2.zero) transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg - 90, Vector3.forward);    
    }

    #endregion


    #region initialization
    
    void Start()
    {
        pauseController.Subscribe(this);
    }

    #endregion


    #region lifecycle

    void FixedUpdate()
    {
        // MOVEMENT
        playerRigidBody.AddForce(moveInput * curPlayerStats.engineForce); // Engine force
        // Friction force
        if (playerRigidBody.linearVelocity.magnitude > minAxisSpeed)
        {
            Vector2 friction = -playerRigidBody.linearVelocity.normalized * curPlayerStats.mass * frictionCoefficient;

            if (friction.magnitude > playerRigidBody.linearVelocity.magnitude / Time.fixedDeltaTime)
            {
                friction = -playerRigidBody.linearVelocity / Time.fixedDeltaTime;
            }

            playerRigidBody.AddForce(friction);
        }
        // Upper bound
        if (playerRigidBody.linearVelocity.magnitude > curPlayerStats.maxSpeed)
        {
            playerRigidBody.linearVelocity = playerRigidBody.linearVelocity.normalized * curPlayerStats.maxSpeed;
        }
        // Lower bound
        if (math.abs(playerRigidBody.linearVelocityX) < minAxisSpeed) playerRigidBody.linearVelocityX = 0;
        if (math.abs(playerRigidBody.linearVelocityY) < minAxisSpeed) playerRigidBody.linearVelocityY = 0;
    }

    #endregion


    #region pause

    public void SetPause(bool isPaused)
    {
        enabled = !isPaused;
    }

    #endregion
}
