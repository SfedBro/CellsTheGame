using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerControlls : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera playerCamera;

    [Header("Hints Settings")]
    [SerializeField] private GameObject hintEnterHUB;
    [SerializeField] private float HUBEnterTime = 2.0f;

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

    private void Start()
    {
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
                SceneManager.LoadScene("FactorySampleScene");
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
}