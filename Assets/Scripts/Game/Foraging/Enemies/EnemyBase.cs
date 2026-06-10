using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] protected List<EnemySO> levelStats;

    [Header("Map")]
    [SerializeField] protected Vector3 centerPosition;
    [SerializeField] protected float mapRadius;

    [Header("Current stats")]
    [SerializeField] protected int curHP;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected int dmg;
    [SerializeField] protected EnemyState state;
    private Action<int, List<LootAmount>, Vector3> onKilled;
    private int level;
    private List<LootAmount> loot;

    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Vector2 moveDirection;

    protected bool playerFound = false;
    protected Transform player;
    protected float lostPlayerTime = 4f;
    protected float seekTimer = 0f;

    public void Prepare(int level, Action<int, List<LootAmount>, Vector3> killed)
    {
        onKilled = killed;
        this.level = level;

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

    public void GetDamage(int dmg)
    {
        curHP -= dmg;

        if (curHP <= 0)
        {   
            onKilled(level, loot, transform.position);

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
}

public enum EnemyState
{
    Default,
    Seek
}