using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera playerCamera;

    [Header("Hints Settings")]
    [SerializeField] private GameObject hintEnterHUB;
    [SerializeField] private float HUBEnterTime = 2.0f;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;
    private bool isHintEnterHUBActive = false;
    private bool isInteractHold = false;
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
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Interact.performed -= OnInteractStart;
        inputActions.Player.Interact.canceled -= OnInteractEnd;
        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnInteractStart(InputAction.CallbackContext context)
    {
        isInteractHold = isHintEnterHUBActive;
    }

    private void OnInteractEnd(InputAction.CallbackContext context)
    {
        isInteractHold = false;
        enterHUBTimer = 0;
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (isHintEnterHUBActive && isInteractHold)
        {
            enterHUBTimer += Time.deltaTime;
            if (enterHUBTimer >= HUBEnterTime)
            {
                print("Entering HUB");
            }
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
        RotateToMouse();
    }

    private void RotateToMouse()
    {
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        rb.MoveRotation(angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HUB"))
        {
            hintEnterHUB.SetActive(true);
            isHintEnterHUBActive = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HUB"))
        {
            hintEnterHUB.SetActive(false);
            isHintEnterHUBActive = false;
            enterHUBTimer = 0;
        }
    }
}