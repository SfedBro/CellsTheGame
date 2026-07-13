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

    [Header("Tank Visuals (Optional)")]
    [SerializeField] private Transform hullTransform;
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private float hullRotationSpeed = 360f;
    [SerializeField] private float gunRotationSpeed = 360f;
    [Tooltip("Angle offset for sprites (e.g. -90 if the sprite is drawn facing UP, or 0 if facing RIGHT).")]
    [SerializeField] private float spriteAngleOffset = -90f;
    private float curHullAngle;
    private float curGunAngle;

    public Transform GunTransform => gunTransform;
    public float SpriteAngleOffset => spriteAngleOffset;

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

        // Initialize visual/body angles to avoid snapping on first movement frame
        if (hullTransform != null)
        {
            curHullAngle = hullTransform.eulerAngles.z;
        }
        else if (bodyTransform != null)
        {
            curHullAngle = bodyTransform.eulerAngles.z;
        }

        if (gunTransform != null)
        {
            curGunAngle = gunTransform.eulerAngles.z;
        }

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
        if (hullTransform != null || gunTransform != null || bodyTransform != null)
        {
            // If child visuals are active, we hide/disable the main SpriteRenderer component on the parent object
            if (sr != null && sr.enabled)
            {
                sr.enabled = false;
            }

            // Keep main Rigidbody2D rotation at 0 (all colliders are circles, so rotation is not needed)
            rb.SetRotation(0f);

            // Rotate Gun towards Mouse position
            if (gunTransform != null)
            {
                Vector2 mouseWorldPos = playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 aimDirection = (mouseWorldPos - (Vector2)gunTransform.position).normalized;
                float targetGunAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg + spriteAngleOffset;
                curGunAngle = Mathf.MoveTowardsAngle(curGunAngle, targetGunAngle, gunRotationSpeed * Time.deltaTime);
                gunTransform.rotation = Quaternion.Euler(0f, 0f, curGunAngle);
            }

            // Rotate Hull and Body towards Movement direction (WASD)
            if (moveInput.sqrMagnitude > 0.001f)
            {
                float targetHullAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + spriteAngleOffset;
                curHullAngle = Mathf.MoveTowardsAngle(curHullAngle, targetHullAngle, hullRotationSpeed * Time.deltaTime);

                if (hullTransform != null)
                {
                    hullTransform.rotation = Quaternion.Euler(0f, 0f, curHullAngle);
                }

                if (bodyTransform != null)
                {
                    bodyTransform.rotation = Quaternion.Euler(0f, 0f, curHullAngle);
                }
            }
        }
        else
        {
            // Fallback: rotate the entire parent GameObject (old behavior)
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

            if (sr != null)
            {
                sr.flipX = flip;
            }
        }
    }

    #endregion


    #region pause

    public void SetPause(bool isPaused)
    {
        enabled = !isPaused;
    }

    #endregion
}
