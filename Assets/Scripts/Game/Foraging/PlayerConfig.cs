using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerConfig : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera playerCamera;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bullet;
    [SerializeField] private GameObject bulletParent;
    [SerializeField] private float shootCooldown = 1f;

    private float nextFireTime = 0f;

    [Header("Hints Settings")]
    [SerializeField] private GameObject hintEnterHUB;
    [SerializeField] private float HUBEnterTime = 2.0f;

    [Header("Stats")]
    [SerializeField] private int MaxHP = 5;
    [SerializeField] private ResourceGenerator rg;
    [SerializeField] private float invinsibleTime = 0.5f;

    private int curHP;
    private float nextHit = 0f;

    private Rigidbody2D rb;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool isInteracting = false;

    private bool inInteractingZone = false;
    private bool inHUBInteractingZone = false;
    private float enterHUBTimer = 0.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        hintEnterHUB.SetActive(false);
    }

    private void OnEnable()
    {
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Interact.performed += OnInteractStart;
        inputActions.Player.Interact.canceled += OnInteractEnd;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Interact.performed -= OnInteractStart;
        inputActions.Player.Interact.canceled -= OnInteractEnd;
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
    }

    private void OnInteractStart(InputAction.CallbackContext context)
    {
        isInteracting = inInteractingZone;
        enterHUBTimer = 0;
    }

    private void OnInteractEnd(InputAction.CallbackContext context)
    {
        isInteracting = false;
        enterHUBTimer = 0;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + shootCooldown;

            GameObject b = Instantiate(bullet, transform.position, transform.rotation);
        }
    }

    private void Start()
    {
        curHP = MaxHP;
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (inHUBInteractingZone && isInteracting)
        {
            enterHUBTimer += Time.deltaTime;
            if (enterHUBTimer >= HUBEnterTime)
            {
                print("Entering HUB");
            }
        }

        rb.linearVelocity = moveInput * moveSpeed;
        RotateToMouse();
    }

    private void RotateToMouse()
    {
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        rb.MoveRotation(angle);
    }

    public void onHUBZoneEnter()
    {
        inInteractingZone = true;
        inHUBInteractingZone = true;
        enterHUBTimer = 0;
        hintEnterHUB.SetActive(true);
    }

    public void onHUBZoneExit()
    {
        inInteractingZone = false;
        inHUBInteractingZone = false;
        enterHUBTimer = 0;
        hintEnterHUB.SetActive(false);
    }

    public void getDMG(int dmg)
    {
        if (Time.time < nextHit) return;

        nextHit = Time.time + invinsibleTime;
        curHP -= dmg;

        if (curHP <= 0)
        {
            transform.position = new Vector3(0, 0, 0);
            curHP = MaxHP;

            rg.onPlayerDeath();
        }
    }
}