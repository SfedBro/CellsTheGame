using System;
using System.Collections;
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
    private SpriteRenderer[] renderers;

    [Header("Player Parts")]
    [SerializeField] private PlayerHull hull;
    [SerializeField] private PlayerGun gun;
    [SerializeField] private PlayerModuleController moduleController;

    [Header("Factory Entering")]
    [SerializeField] private float factoryEnteringTime = 2f;
    private int playerAttackersCounter = 0;
    private float factoryEnteringTimer = 0f;
    private bool factoryIsEntering = false;

    [Header("Attack")]
    [SerializeField] private Transform bulletParent;
    [SerializeField] private PlayerModule baseCannonModule;
    public IWeapon DefaultWeapon { get; set; }
    public IWeapon CurrentWeapon { get; set; }
    [Header("Stats")]
    [SerializeField] private PlayerStats basicPlayerStats;
    public PlayerStats curPlayerStats;
    private System.Collections.Generic.List<StatModifier> statModifiers = new System.Collections.Generic.List<StatModifier>();
    private float curHP;

    [Header("Fight")]
    [SerializeField] private float invulnerabilityDuration = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;
    public AttackData attackData = new();
    private bool isInvulnerable;
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
        renderers = GetComponentsInChildren<SpriteRenderer>();

        if (baseCannonModule != null)
        {
            PlayerModule runtimeBaseModule = Instantiate(baseCannonModule);
            foreach (var effect in runtimeBaseModule.effects)
            {
                if (effect is IWeapon weapon)
                {
                    DefaultWeapon = weapon;
                    CurrentWeapon = weapon;
                    break;
                }
            }
        }

        if (DefaultWeapon == null)
        {
            Debug.LogError("No IWeapon effect found in Base Cannon Module on Player -> PlayerController!");
        }
    }

    void Start()
    {
        // Correct stats
        recalculateStats();
        hull.curPlayerStats = curPlayerStats;
        gun.curPlayerStats = curPlayerStats;

        curHP = curPlayerStats.maxHP;

        // Set hints
        hintsController.SetPlayerHP(curHP);
        hintsController.OnPlayerLeaveDanger();

        // Equip modules
        moduleController.InitializeModules();

        gun.SetAttackData(attackData);

        pauseController.Subscribe(this);

        ActivateInvulnerability();
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

            SetAllRenderersColor(new Color(1, 1, 1, Mathf.Clamp01(1 - factoryEnteringTimer / factoryEnteringTime))); // Update player's transparency

            if (factoryEnteringTimer > factoryEnteringTime) SceneManager.LoadScene("Factory");
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
        ActivateInvulnerability();
    }

    private void Die(EnemyBase killer)
    {
        // Stop moving
        hull.SetMoveInput(Vector2.zero);
        rb.linearVelocity = Vector2.zero;

        // Stop attack
        gun.AttackEnd();

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
        hull.SetMoveInput(context.ReadValue<Vector2>().normalized);
    }

    #endregion

    
    #region enteringFactory

    private void onStartEnteringFactory(InputAction.CallbackContext context)
    {
        if (playerAttackersCounter > 0 || isInvulnerable) return; // Cannot enter hub while in fight

        if (factoryIsEntering) onCancelEnteringFactory(context); // Reenter - cancels first entering
        
        // Entering start
        factoryIsEntering = true;
        factoryEnteringTimer = 0f;
    }

    private void onCancelEnteringFactory(InputAction.CallbackContext context)
    {
        factoryIsEntering = false;
        SetAllRenderersColor(Color.white);
    }

    #endregion


    #region fight
    private void OnAttackStart(InputAction.CallbackContext context)
    {
        if (onPause) return;

        // Attack
        gun.AttackStart();
    }

    private void OnAttackEnd(InputAction.CallbackContext context)
    {
        if (onPause)
        {
            endAttack = true;
            return;
        }
        gun.AttackEnd();
    }

    public void getDMG(EnemyBase killer)
    {
        if (isInvulnerable) return;

        // Become invinsible
        ActivateInvulnerability();

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
        attackData.speed = curPlayerStats.bulletSpeed;
        attackData.range = curPlayerStats.attackRange;
    }

    private void SetAllRenderersColor(Color color)
    {
        foreach (var rend in renderers) rend.color = color;
    }

    private void ActivateInvulnerability()
    {
        isInvulnerable = true;
        SetAllRenderersColor(Color.gray);
        StartCoroutine(BlinkCoroutine());
        StartCoroutine(DisableInvulnerabilityAfterDelay(invulnerabilityDuration));
    }

    private IEnumerator BlinkCoroutine()
    {
        while (isInvulnerable)
        {
            yield return new WaitForSeconds(blinkInterval);
            
            SetAllRenderersColor(Color.white);

            yield return new WaitForSeconds(blinkInterval);

            SetAllRenderersColor(Color.gray);
        }

        SetAllRenderersColor(Color.white);
    }

    private IEnumerator DisableInvulnerabilityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isInvulnerable = false;
        SetAllRenderersColor(Color.white);
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
                break;
            case StatType.ShootSpeed:
                basicPlayerStats.attackCoolDown = newValue;
                break;
            case StatType.ShootRange:
                basicPlayerStats.attackRange = newValue;
                break;
            case StatType.BulletSpeed:
                basicPlayerStats.bulletSpeed = newValue;
                break;
        }
        
        recalculateStats();
        UpdateAttackData();
    }

    #endregion


    #region stats

    public void AddModifier(StatModifier mod)
    {
        statModifiers.Add(mod);
        recalculateStats();
    }

    public void RemoveModifier(StatModifier mod)
    {
        statModifiers.Remove(mod);
        recalculateStats();
    }

    public void RemoveModifiersFromSource(object source)
    {
        statModifiers.RemoveAll(m => m.Source == source);
        recalculateStats();
    }

    public void recalculateStats()
    {
        // 1. Start with basic stats
        curPlayerStats.Copy(basicPlayerStats);

        // 2. Apply all Flat modifiers first
        foreach (var mod in statModifiers)
        {
            if (mod.Type == ModifierType.Flat)
            {
                ApplyModifier(curPlayerStats, mod.StatType, mod.Value, isMultiply: false);
            }
        }

        // 3. Apply all Percent modifiers next
        foreach (var mod in statModifiers)
        {
            if (mod.Type == ModifierType.Percent)
            {
                ApplyModifier(curPlayerStats, mod.StatType, mod.Value, isMultiply: true);
            }
        }
        
        // Update HP boundaries if changed
        if (hintsController != null)
        {
            hintsController.SetPlayerHP(curHP);
        }
    }

    private void ApplyModifier(PlayerStats stats, PlayerStatType type, float value, bool isMultiply)
    {
        switch (type)
        {
            case PlayerStatType.Mass:
                if (isMultiply) stats.mass *= (1f + value); else stats.mass += value;
                break;
            case PlayerStatType.EngineForce:
                if (isMultiply) stats.engineForce *= (1f + value); else stats.engineForce += value;
                break;
            case PlayerStatType.MaxSpeed:
                if (isMultiply) stats.maxSpeed *= (1f + value); else stats.maxSpeed += value;
                break;
            case PlayerStatType.RotationSpeed:
                if (isMultiply) stats.rotationSpeed *= (1f + value); else stats.rotationSpeed += value;
                break;
            case PlayerStatType.MaxHP:
                if (isMultiply) stats.maxHP *= (1f + value); else stats.maxHP += value;
                break;
            case PlayerStatType.MinSpeed:
                if (isMultiply) stats.minSpeed *= (1f + value); else stats.minSpeed += value;
                break;
            case PlayerStatType.Damage:
                if (isMultiply) stats.dmg *= (1f + value); else stats.dmg += value;
                break;
            case PlayerStatType.AttackCoolDown:
                if (isMultiply) stats.attackCoolDown *= (1f + value); else stats.attackCoolDown += value;
                break;
        }
    }

    public void AddMass(int m)
    {
        AddModifier(new StatModifier(m, ModifierType.Flat, PlayerStatType.Mass, this));
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
        if (endAttack) 
        { 
            gun.AttackEnd();
            endAttack = false;
        }
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
    public float attackRange;
    public float bulletSpeed;

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
        attackRange = initial;
        bulletSpeed = initial;
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
        attackRange += other.attackRange;
        bulletSpeed += other.bulletSpeed;
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
        attackRange *= other.attackRange;
        bulletSpeed *= other.bulletSpeed;
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
        result.attackRange *= multiplication;
        result.bulletSpeed *= multiplication;

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
        attackRange = other.attackRange;
        bulletSpeed = other.bulletSpeed;
    }
}