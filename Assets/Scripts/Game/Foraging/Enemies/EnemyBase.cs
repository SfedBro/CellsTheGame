using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemyBase : MonoBehaviour, IPausable
{
    [Header("Levels")]
    [SerializeField] protected List<EnemySO> levelStats;

    [Header("Map")]
    [SerializeField] protected Vector3 centerPosition;
    [SerializeField] protected float mapRadius;

    [Header("Current stats")]
    [SerializeField] protected float curHP;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected int dmg;
    [SerializeField] protected EnemyState state;
    [SerializeField] protected Sprite killerSprite;
    private Action<int, List<LootAmount>, Vector3> onKilled;
    private int level;
    private List<LootAmount> loot;
    private int upgradePoints = 0;
    private PauseController pauseController;

    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Vector2 moveDirection;

    protected bool playerFound = false;
    protected Transform player;
    protected float lostPlayerTime = 4f;
    protected float seekTimer = 0f;

    public void Prepare(int level, Action<int, List<LootAmount>, Vector3> killed, PauseController pause)
    {
        onKilled = killed;
        this.level = level;
        pauseController = pause;
        pause.Subscribe(this);

        EnemySO stats = levelStats[math.clamp(level - 1, 0, levelStats.Count)];
        curHP = stats.GetHP();
        moveSpeed = stats.GetSpeed();
        dmg = stats.getDMG();
        loot = stats.generateLoot();

        moveDirection = UnityEngine.Random.insideUnitCircle.normalized;
    }

    protected void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        state = EnemyState.Default;
    }

    void Update()
    {
        if (state == EnemyState.Seek)
        {
            seekTimer += Time.deltaTime;
            if (seekTimer >= lostPlayerTime)
            {
                seekTimer = 0f;
                state = EnemyState.Default;
                OnStopSeek();
            }
        }
    }

    public void OnPlayerFound()
    {
        playerFound = true;
    }

    public void OnPlayerLost()
    {
        playerFound = false;
        state = EnemyState.Seek;
        OnStartSeek();
    }

    public void GetDamage(float dmg)
    {
        curHP -= dmg;

        if (curHP <= 0)
        {
            pauseController.Unsubscribe(this);
            
            onKilled(math.clamp(level, 1, levelStats.Count), loot, transform.position);

            Destroy(gameObject);
        }
    }

    public int GetAttack()
    {
        return dmg;
    }

    protected void OnStartSeek() {}
    protected void OnStopSeek() {}

    public void SetBounds(Vector3 center, float radius)
    {
        centerPosition = center;
        mapRadius = radius;
    }

    public void AddLoot(ItemType type, int amount) 
    {
        if (type == ItemType.Default)
        {
            amount /= 2;
        }
        upgradePoints += amount;

        foreach (LootAmount l in loot)
        {
            if (l.GetItemType() == type)
            {
                l.amount += amount;
                return;
            }
        }

        loot.Add(new LootAmount(type, amount));
    }

    public void onPlayerKilled()
    {
        sr.sprite = killerSprite;

        if (upgradePoints >= level * 50)
        {
            level++;
            upgradePoints = 0;
        }

        EnemySO stats = levelStats[math.clamp(level - 1, 0, levelStats.Count)];
        curHP = stats.GetHP();

        int diff = level - levelStats.Count;
        if (diff > 0)
        {
            curHP += (int)(stats.GetHP() * 0.5 * diff);
            dmg = stats.getDMG() + diff;
        }

        transform.localScale = new Vector3((level + 1) * 0.5f, (level + 1) * 0.5f, 1);
    }

    public void SetPause(bool pause)
    {
        enabled = !pause;
    }
}

public enum EnemyState
{
    Default,
    Seek
}