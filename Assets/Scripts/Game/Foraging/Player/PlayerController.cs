using System;
using System.Resources;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private ForagingManager foragingManager;
    [SerializeField] private HintsController hintsController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private UpgradingManager upgradingManager;
    [SerializeField] private PlayerExperienceManager playerExperienceManager;
    private InputSystem_Actions inputActions;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerModuleController moduleController;

    [Header("Movement")]
    [SerializeField] private float frictionCoefficient = 1f;
    [SerializeField] private float minAxisSpeed = 0.01f;
    private Vector2 moveInput;
    private float targetAngle;
    private float curAngle;

    [Header("Factory Entering")]
    [SerializeField] private float factoryEnteringTime = 2f;
    private int playerAttackersCounter = 0;
    private float factoryEnteringTimer = 0f;
    private bool factoryIsEntering = false;

    [Header("Atack")]
    [SerializeField] private Transform bulletParent;
    public GameObject attackPrefab;
    public AttackData attackData;
    public Func<GameObject, AttackData, Transform, float, int> attackStart;

    [Header("Stats")]
    [SerializeField] private PlayerStats basicPlayerStats;
    [SerializeField] private PlayerStats curPlayerStats;
    private PlayerStats addIncrements = new();
    private float curHP;

    [Header("Fight")]
    [SerializeField] private float invinsibilityTime = 1.5f;
    [SerializeField] private Color invinsibleColor;
    [SerializeField] private GameObject bulletPrefab;
    private float nextHit = 0f;
    private bool isInvinsible = true;

    [Header("Active Ability - E")]
    public Action<PlayerController> activeAbilityE;


    #endregion


    #region initialization

    void Awake()
    {
        inputActions = foragingManager.GetInputSystem();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        moduleController = GetComponent<PlayerModuleController>();
    }

    void Start()
    {
        // Correct stats
        upgradingManager.correctPlayerStats(this);
        curPlayerStats = basicPlayerStats;

        curHP = curPlayerStats.maxHP;
        curAngle = rb.rotation;
        nextHit = Time.time + invinsibilityTime;

        // Set hints
        hintsController.SetPlayerHP(curHP);
        hintsController.OnPlayerLeaveDanger();

        // Equip modules
        moduleController.InitializeModules();
    }

    void OnEnable()
    {
        inputActions.Player.Move.performed += onMove;
        inputActions.Player.Move.canceled += onMove;
        inputActions.Player.Crouch.started += onStartEnteringFactory;
        inputActions.Player.Crouch.canceled += onCancelEnteringFactory;
        inputActions.Player.Attack.started += OnAttackStart;
        inputActions.Player.Interact.started += onActivateAbility;
    }

    void OnDisable()
    {
        inputActions.Player.Move.performed -= onMove;
        inputActions.Player.Move.canceled -= onMove;
        inputActions.Player.Crouch.started -= onStartEnteringFactory;
        inputActions.Player.Crouch.canceled -= onCancelEnteringFactory;
        inputActions.Player.Attack.started -= OnAttackStart;
        inputActions.Player.Interact.started -= onActivateAbility;
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
    }

    void Update()
    {
        // ENTERING HUB
        if (factoryIsEntering)
        {
            factoryEnteringTimer += Time.deltaTime; // Upfate timer

            sr.color = new Color(1, 1, 1, Mathf.Clamp01(1 - factoryEnteringTimer / factoryEnteringTime)); // Update player's transparency

            if (factoryEnteringTimer > factoryEnteringTime) SceneManager.LoadScene("FactorySampleScene");
        }

        // INVINCIBILITY
        if (isInvinsible)
        {
            if (nextHit < Time.time)
            {
                isInvinsible = false;
                sr.color = Color.white;
            }
        }

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


    #region playerDeath

    public void Respawn()
    {
        // Move to spawn point
        transform.position = Vector2.zero;

        // Update hp
        curHP = curPlayerStats.maxHP;  
        hintsController.SetPlayerHP(curHP);

        // Notify
        hintsController.OnPlayerRespawn();

        // Respawn
        gameObject.SetActive(true);
        isInvinsible = true;
    }

    private void Die(EnemyBase killer)
    {
        // Stop moving
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // Notify
        foragingManager.onPlayerDeath();
        hintsController.OnPlayerDeath();
        ResourcesManager.instance.onPlayerDeath(killer);
        upgradingManager.onPlayerDeath();
        upgradingManager.correctPlayerStats(this);
        playerExperienceManager.onPlayerDeath(killer);
        killer.onPlayerKilled();

        // Death
        gameObject.SetActive(false);
    }

    #endregion


    #region movement

    private void onMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
    }

    #endregion

    
    #region enteringFactory

    private void onStartEnteringFactory(InputAction.CallbackContext context)
    {
        if (playerAttackersCounter > 0 || isInvinsible) return; // Cannot enter hub while in fight

        if (factoryIsEntering) onCancelEnteringFactory(context); // Reenter - cancels first entering
        
        // Entering start
        factoryIsEntering = true;
        factoryEnteringTimer = 0f;
    }

    private void onCancelEnteringFactory(InputAction.CallbackContext context)
    {
        factoryIsEntering = false;
        sr.color = new Color(1, 1, 1, 1);
    }

    #endregion


    #region fight
    private void OnAttackStart(InputAction.CallbackContext context)
    {
        int cost = attackStart(attackPrefab, attackData, bulletParent, sr.flipX? rb.rotation - 180 : rb.rotation);
        // if (b != null) apply modifiers
    }

    public void getDMG(EnemyBase killer)
    {
        if (Time.time < nextHit) return;

        // Become invinsible
        nextHit = Time.time + invinsibilityTime;
        sr.color = invinsibleColor;
        isInvinsible = true;

        // Correct hp
        curHP -= killer.GetAttack();
        hintsController.SetPlayerHP(curHP);
        
        // Player death
        if (curHP <= 0)
        {
            Die(killer);
        }
    }

    public void UpdateAttackData()
    {
        if (attackData == null) attackData = new();

        attackData.dmg = curPlayerStats.dmg;
        attackData.speed = 10f;
        attackData.timeToLive = 2f;
    }

    #endregion


    #region upgrades

    public void UpgradeStat(StatType type, float newValue)
    {
        switch (type)
        {
            case StatType.Speed:
                basicPlayerStats.engineForce = newValue;
                basicPlayerStats.maxSpeed = newValue * 0.5f;
                break;
            case StatType.Health:
                basicPlayerStats.maxHP = (int)newValue;
                curHP = basicPlayerStats.maxHP;
                hintsController.SetPlayerHP(curHP);
                break;
            case StatType.Damage:
                basicPlayerStats.dmg = (int)newValue;
                UpdateAttackData();
                break;
        }
        
        recalculateStats();
    }

    #endregion


    #region stats

    private void recalculateStats()
    {
        curPlayerStats = basicPlayerStats;

        curPlayerStats += addIncrements;
    }

    public void AddMass(int m)
    {
        addIncrements.mass += m;

        recalculateStats();
    }

    #endregion


    #region activeAbility

    private void onActivateAbility(InputAction.CallbackContext context)
    {
        if (activeAbilityE != null) activeAbilityE(this);
    }

    #endregion


    #region modulesFeatures

    public void Heal(float amount)
    {
        curHP = Math.Clamp(curHP + amount, 1, curPlayerStats.maxHP);
        hintsController.SetPlayerHP(curHP);
    }

    #endregion
}


[Serializable]
class PlayerStats
{
    [Header("Movement")]
    public float mass;
    public float engineForce;
    public float maxSpeed;
    public float rotationSpeed;

    [Header("Fight")]
    public float maxHP;
    public float dmg;

    public static PlayerStats operator +(PlayerStats s1, PlayerStats s2)
    {
        return new()
        {
            mass = s1.mass + s2.mass,
            engineForce = s1.engineForce + s2.engineForce,
            maxSpeed = s1.maxSpeed + s2.maxSpeed,
            rotationSpeed = s1.rotationSpeed + s2.rotationSpeed,
            maxHP = s1.maxHP + s2.maxHP,
            dmg = s1.dmg + s2.dmg
        };
    }
}