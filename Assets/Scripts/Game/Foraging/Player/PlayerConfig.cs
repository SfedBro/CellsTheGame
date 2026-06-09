using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerConfig : MonoBehaviour
{

    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;
    private ResourcesManager rm = ResourcesManager.instance;
    private SpriteRenderer sr;
    private bool gray;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera playerCamera;
    private Vector2 moveInput;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private float shootCooldown = 1f;
    private float nextShootTime = 0f;

    [Header("HUB building")]
    [SerializeField] private GameObject HUBPrefab;
    [SerializeField] private CircleCollider2D  HUBCollider;
    [SerializeField] private SpriteRenderer HUBSprite;
    [SerializeField] private TMPro.TextMeshProUGUI HUBHint;
    [SerializeField] private float buildingTime;
    [SerializeField] private TMPro.TextMeshProUGUI buildHint;
    private bool isBuilding = false;
    private float buildingHUBTimer;
    private bool canBuild = false;
    private Vector3 respawn = Vector3.zero;


    [Header("Hints Settings")]
    [SerializeField] private TMPro.TextMeshProUGUI hpIndicatorUI;
    [SerializeField] private DeathScreen deathScreen;
    private bool isInteracting = false;
    private Action curInteractAction;
    private float interactTime;
    private float interactTimer;


    [Header("Stats")]
    [SerializeField] private UpgradingManager upgradingManager;
    [SerializeField] private int MaxHP = 5;
    [SerializeField] private int curHP;
    [SerializeField] private int dmg;
    [SerializeField] private float invinsibleTime = 0.5f;
    private float nextHit = 0f;

    [Header("References")]
    [SerializeField] private PlayerExperienceManager playerExperienceManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        sr = GetComponent<SpriteRenderer>();

        deathScreen.player = this;
        deathScreen.gameObject.SetActive(false);
        
        upgradingManager.correctPlayerStats(this);
    }

    private void OnEnable()
    {
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Interact.performed += OnHUBBuild;
        inputActions.Player.Interact.canceled += OnHUBBuildCanceled;
        inputActions.Player.Crouch.performed += OnInteractStart;
        inputActions.Player.Crouch.canceled += OnInteractEnd;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Interact.performed -= OnHUBBuild;
        inputActions.Player.Interact.canceled -= OnHUBBuildCanceled;
        inputActions.Player.Crouch.performed -= OnInteractStart;
        inputActions.Player.Crouch.canceled -= OnInteractEnd;
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
    }

    private void OnHUBBuild(InputAction.CallbackContext context)
    {
        if (!canBuild) return;

        if (isBuilding) CancelBuilding();
        
        isBuilding = true;
        buildingHUBTimer = 0f;
        GameObject hub = Instantiate(HUBPrefab, transform.position, Quaternion.identity);
        HUB h = hub.GetComponent<HUB>();
        h.hint = HUBHint;
        h.onDes = () => {canBuild = true; if (buildHint != null) buildHint.enabled = true;};
        HUBCollider = hub.GetComponent<CircleCollider2D>();
        HUBCollider.enabled = false;
        HUBSprite = hub.GetComponent<SpriteRenderer>();
    }

    private void OnHUBBuildCanceled(InputAction.CallbackContext context)
    {
        if (isBuilding && buildingHUBTimer < buildingTime)
        {
            CancelBuilding();
        }
    }

    private void CancelBuilding()
    {
        if (HUBCollider != null) Destroy(HUBCollider.gameObject);
        if (HUBSprite != null) Destroy(HUBSprite.gameObject);

        isBuilding = false;
        buildingHUBTimer = 0f;
        canBuild = true;
    }

    private void OnInteractStart(InputAction.CallbackContext context)
    {
        isInteracting = curInteractAction != null;
        interactTimer = 0;
    }

    private void OnInteractEnd(InputAction.CallbackContext context)
    {
        isInteracting = false;
        interactTimer = 0;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + shootCooldown;

            GameObject b = Instantiate(bulletPrefab, transform.position, transform.rotation);
            b.transform.Rotate(new Vector3(0, sr.flipX? 180 : 0, 0));
            b.transform.parent = bulletParent;
            b.transform.localScale = new Vector3((dmg + 3f) / 8f, (dmg + 3f) / 8f, 1);
            b.GetComponent<Bullet>().dmg = dmg;
        }
    }

    private void Start()
    {
        curHP = MaxHP;
        hpIndicatorUI.text = curHP.ToString();
        buildHint.enabled = false;
        HUBSprite.gameObject.GetComponent<HUB>().onDes = () => {canBuild = true; if (buildHint != null) buildHint.enabled = true;};

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Time.time > nextHit && gray)
        {
            gray = false;
            sr.color = new Color(1, 1, 1, 1);
        }

        // Building HUB
        if (isBuilding)
        {
            buildingHUBTimer += Time.deltaTime;

            Color color = HUBSprite.color;
            color.a = Mathf.Clamp01(buildingHUBTimer / buildingTime);
            HUBSprite.color = color;

            if (buildingHUBTimer >= buildingTime)
            {
                CompleteBuilding();
            }
        }

        // Interacting
        if (isInteracting)
        {
            interactTimer += Time.deltaTime;
            if (interactTimer >= interactTime)
            {
                curInteractAction();
            }
        }

        // Moving
        rb.linearVelocity = moveInput * moveSpeed;
        RotateToMouse();
    }

    private void CompleteBuilding()
    {
        HUBCollider.enabled = true;
        isBuilding = false;
        buildHint.enabled = false;
        canBuild = false;
        respawn = transform.position;
    }

    private void RotateToMouse()
    {
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        float rawAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        bool flip = false;
        float finalAngle = rawAngle;

        if (rawAngle > 90f || rawAngle < -90f)
        {
            flip = true;
            finalAngle = rawAngle - 180f;
        }

        sr.flipX = flip;
        rb.MoveRotation(finalAngle);
    }

    public void onInteractZoneEnter(float time, Action action)
    {
        interactTimer = 0;
        interactTime = time;
        curInteractAction = action;
    }
    public void onInteractZoneExit()
    {
        isInteracting = false;
        interactTimer = 0;
        curInteractAction = null;
    }

    public void getDMG(int dmg)
    {
        if (Time.time < nextHit) return;

        nextHit = Time.time + invinsibleTime;
        sr.color = new Color(1, 1, 1, 0.5f);
        gray = true;
        curHP -= dmg;
        hpIndicatorUI.text = curHP.ToString();

        if (curHP <= 0)
        {
            moveInput = Vector2.zero;
            rm.onPlayerDeath();
            upgradingManager.onPlayerDeath();
            upgradingManager.correctPlayerStats(this);
            playerExperienceManager.onPlayerDeath();
            
            gameObject.SetActive(false);
            enabled = false;

            deathScreen.gameObject.SetActive(true);
        }
    }

    public void Respawn()
    {
        gameObject.SetActive(true);
        enabled = true;

        transform.position = respawn;
        curHP = MaxHP;  
        hpIndicatorUI.text = curHP.ToString();

        if (HUBCollider != null) {
            HUBCollider.gameObject.GetComponent<HUB>().onDes = () => {};
            Destroy(HUBCollider.gameObject);
        }

        buildingHUBTimer = 0f;
        GameObject hub = Instantiate(HUBPrefab, transform.position, Quaternion.identity);
        HUB h = hub.GetComponent<HUB>();
        h.hint = HUBHint;
        h.onDes = () => {canBuild = true; if (buildHint != null) buildHint.enabled = true;};
        HUBCollider = hub.GetComponent<CircleCollider2D>();
        HUBSprite = hub.GetComponent<SpriteRenderer>();

        HUBCollider.enabled = true;
        isBuilding = false;
        buildHint.enabled = false;
        canBuild = false;
    }

    public void upgradeStat(StatType type, float newValue)
    {
        switch (type)
        {
            case StatType.Speed:
                moveSpeed = newValue;
                break;
            case StatType.Health:
                MaxHP = (int)newValue;
                curHP = MaxHP;
                hpIndicatorUI.text = curHP.ToString();
                break;
            case StatType.Damage:
                dmg = (int)newValue;
                break;
        }
    }
}