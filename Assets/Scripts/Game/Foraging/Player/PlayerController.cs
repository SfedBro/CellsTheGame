using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IPausable
{
    #region fields

    [Header("References")]
    [SerializeField] private ForagingManager foragingManager;
    [SerializeField] private HintsController hintsController;
    [SerializeField] private PauseController pauseController;
    private InputSystem_Actions inputActions;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Header("Player Parts")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerModuleController moduleController;

    [Header("Factory Entering")]
    [SerializeField] private float factoryEnteringTime = 2f;
    private int playerAttackersCounter = 0;
    private float factoryEnteringTimer = 0f;
    private bool factoryIsEntering = false;

    [Header("Atack")]
    [SerializeField] private Transform bulletParent;
    [SerializeField] private PlayerModule baseCannonModule;
    public AttackData attackData = new();
    public IModuleCannon baseCannon;
    public IModuleCannon curCannon;

    [Header("Stats")]
    [SerializeField] private PlayerStats basicPlayerStats;
    [SerializeField] private PlayerStats curPlayerStats;
    private PlayerStats addIncrements = new(0);
    private PlayerStats multIncrements = new(1);
    private float curHP;

    [Header("Fight")]
    [SerializeField] private float invinsibilityTime = 1.5f;
    [SerializeField] private Color invinsibleColor;
    private float nextHit = 0f;
    private bool isInvinsible = true;
    private bool onPause = false;
    private bool endAttack = false;

    [Header("Active Ability - E")]
    public Action<PlayerController> activeAbilityE;

    #endregion


    #region initialization

    void Awake()
    {
        inputActions = foragingManager.GetInputSystem();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (baseCannonModule is IModuleCannon)
        {
            baseCannon = (IModuleCannon)Instantiate(baseCannonModule);
            curCannon = baseCannon;
        } 
        else
        {
            Debug.LogError("Invalid Base Cannon Set in Player -> PlayerController!");
        }
    }

    void Start()
    {
        // Correct stats
        recalculateStats();
        movement.curPlayerStats = curPlayerStats;

        curHP = curPlayerStats.maxHP;
        nextHit = Time.time + invinsibilityTime;

        // Set hints
        hintsController.SetPlayerHP(curHP);
        hintsController.OnPlayerLeaveDanger();

        // Equip modules
        moduleController.InitializeModules();

        pauseController.Subscribe(this);
    }

    void OnEnable()
    {
        inputActions.Player.Move.performed += onMove;
        inputActions.Player.Move.canceled += onMove;
        inputActions.Player.Crouch.started += onStartEnteringFactory;
        inputActions.Player.Crouch.canceled += onCancelEnteringFactory;
        inputActions.Player.Attack.started += OnAttackStart;
        inputActions.Player.Attack.canceled += OnAttackEnd;
        inputActions.Player.Interact.started += onActivateAbility;
    }

    void OnDisable()
    {
        inputActions.Player.Move.performed -= onMove;
        inputActions.Player.Move.canceled -= onMove;
        inputActions.Player.Crouch.started -= onStartEnteringFactory;
        inputActions.Player.Crouch.canceled -= onCancelEnteringFactory;
        inputActions.Player.Attack.started -= OnAttackStart;
        inputActions.Player.Attack.canceled -= OnAttackEnd;
        inputActions.Player.Interact.started -= onActivateAbility;
    }

    #endregion


    #region lifecycle

    void Update()
    {
        // ENTERING HUB
        if (factoryIsEntering)
        {
            factoryEnteringTimer += Time.deltaTime; // Upfate timer

            sr.color = new Color(1, 1, 1, Mathf.Clamp01(1 - factoryEnteringTimer / factoryEnteringTime)); // Update player's transparency

            if (factoryEnteringTimer > factoryEnteringTime) SceneManager.LoadScene("Factory");
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
        movement.moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // Notify
        foragingManager.onPlayerDeath(killer);
        hintsController.OnPlayerDeath();

        // Death
        gameObject.SetActive(false);
    }

    #endregion


    #region movement

    private void onMove(InputAction.CallbackContext context)
    {
        movement.moveInput = context.ReadValue<Vector2>().normalized;
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
        if (onPause) return;
        // Update attack data
        attackData.attakCoolDown = curPlayerStats.attackCoolDown;

        // Attack
        curCannon.AttackStart(attackData, bulletParent, sr.flipX? rb.rotation - 180 : rb.rotation);
    }

    private void OnAttackEnd(InputAction.CallbackContext context)
    {
        if (onPause)
        {
            endAttack = true;
            return;
        }
        curCannon.AttackEnd();
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
                basicPlayerStats.maxHP = newValue;
                curHP = basicPlayerStats.maxHP;
                hintsController.SetPlayerHP(curHP);
                break;
            case StatType.Damage:
                basicPlayerStats.dmg = newValue;
                recalculateStats();
                UpdateAttackData();
                return;
        }
        
        recalculateStats();
    }

    #endregion


    #region stats

    private void recalculateStats()
    {
        curPlayerStats.Copy(basicPlayerStats);
        curPlayerStats.Add(addIncrements);
        curPlayerStats.Multiply(multIncrements);
    }

    public void AddMass(int m)
    {
        addIncrements.mass += m;

        recalculateStats();
    }

    public void AddAddIncrements(PlayerStats addition)
    {
        addIncrements.Add(addition);
        recalculateStats();
    }

    public void AddMultIncrements(PlayerStats multiplication)
    {
        multIncrements.Add(multiplication);
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


    #region pause

    public void SetPause(bool pause)
    {
        onPause = pause;
        if (endAttack) curCannon.AttackEnd();
    }

    #endregion
}


[Serializable]
public class PlayerStats
{
    [Header("Movement")]
    public float mass;
    public float engineForce;
    public float maxSpeed;
    public float minSpeed;
    public float rotationSpeed;

    [Header("Fight")]
    public float maxHP;
    public float dmg;
    public float attackCoolDown;

    public PlayerStats(int initial)
    {
        mass = initial;
        engineForce = initial;
        maxSpeed = initial;
        rotationSpeed = initial;
        maxHP = initial;
        minSpeed = initial;
        dmg = initial;
        attackCoolDown = initial;
    }

    public void Add(PlayerStats other)
    {
        mass += other.mass;
        engineForce += other.engineForce;
        maxSpeed += other.maxSpeed;
        rotationSpeed += other.rotationSpeed;
        maxHP += other.maxHP;
        minSpeed += other.minSpeed;
        dmg += other.dmg;
        attackCoolDown += other.attackCoolDown;
    }

    public void Multiply(PlayerStats other)
    {
        mass *= other.mass;
        engineForce *= other.engineForce;
        maxSpeed *= other.maxSpeed;
        rotationSpeed *= other.rotationSpeed;
        maxHP *= other.maxHP;
        minSpeed *= other.minSpeed;
        dmg *= other.dmg;
        attackCoolDown *= other.attackCoolDown;
    }

    public static PlayerStats operator *(PlayerStats s1, float multiplication)
    {
        PlayerStats result = new(0);
        result.Copy(s1);
        
        result.mass *= multiplication;
        result.engineForce *= multiplication;
        result.maxSpeed *= multiplication;
        result.rotationSpeed *= multiplication;
        result.maxHP *= multiplication;
        result.minSpeed *= multiplication;
        result.dmg *= multiplication;
        result.attackCoolDown *= multiplication;

        return result;
    }

    public void Copy(PlayerStats other)
    {
        mass = other.mass;
        engineForce = other.engineForce;
        maxSpeed = other.maxSpeed;
        rotationSpeed = other.rotationSpeed;
        maxHP = other.maxHP;
        minSpeed = other.minSpeed;
        dmg = other.dmg;
        attackCoolDown = other.attackCoolDown;
    }
}