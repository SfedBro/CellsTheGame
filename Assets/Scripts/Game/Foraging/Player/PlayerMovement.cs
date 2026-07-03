using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour, IPausable
{
    #region fields
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PauseController pauseController;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public PlayerStats curPlayerStats;
    
    [Header("Movement")]
    [SerializeField] private float frictionCoefficient = 1f;
    [SerializeField] private float minAxisSpeed = 0.01f;
    public Vector2 moveInput;
    private float targetAngle;
    private float curAngle;

    #endregion


    #region initialization

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        curAngle = rb.rotation;
        pauseController.Subscribe(this);
    }

    #endregion


    #region lifecycle

    void FixedUpdate()
    {
        // MOVEMENT
        rb.AddForce(moveInput * curPlayerStats.engineForce); // Engine force
        // Friction force
        if (rb.linearVelocity.magnitude > minAxisSpeed)
        {
            Vector2 friction = -rb.linearVelocity.normalized * curPlayerStats.mass * frictionCoefficient;

            if (friction.magnitude > rb.linearVelocity.magnitude / Time.fixedDeltaTime)
            {
                friction = -rb.linearVelocity / Time.fixedDeltaTime;
            }

            rb.AddForce(friction);
        }
        // Upper bound
        if (rb.linearVelocity.magnitude > curPlayerStats.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * curPlayerStats.maxSpeed;
        }
        // Lower bound
        if (math.abs(rb.linearVelocityX) < minAxisSpeed) rb.linearVelocityX = 0;
        if (math.abs(rb.linearVelocityY) < minAxisSpeed) rb.linearVelocityY = 0;

        // ROTATION
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        curAngle = Mathf.MoveTowardsAngle(curAngle, targetAngle, curPlayerStats.rotationSpeed * Time.deltaTime);
        bool flip = false;

        if (curAngle > 90f || curAngle < -90f)
        {
            rb.MoveRotation(curAngle - 180);
            flip = true;
        }
        else
        {
            rb.MoveRotation(curAngle);
        }

        sr.flipX = flip;
    }

    #endregion


    #region pause

    public void SetPause(bool isPaused)
    {
        enabled = !isPaused;
    }

    #endregion
}
