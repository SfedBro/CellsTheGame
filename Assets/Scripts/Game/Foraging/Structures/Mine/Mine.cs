using System;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    private MineData data;
    private SpriteRenderer sr;
    private List<TickEvent> actions = new();
    private Action<List<LootAmount>, Vector3> spawnLoot;
    private PauseController pauseController;

    [Header("Resource Spawn")]
    [SerializeField] private GameObject resourcePrefab;
    private CollectableData resourceData;
    private int resourceCapacity;
    private int curResources = 0;

    private EnemyBase enemyBase;
    private int enemyCapacity;
    private int curEnemies = 0;
    private int enemyLevel;
    private float enemyWalkRadius;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(MineData d, Action<List<LootAmount>, Vector3> s, PauseController pause)
    {
        data = d;
        sr.sprite = d.GetSprite();
        spawnLoot = s;
        pauseController = pause;

        resourceData = d.GeItemData();
        resourceCapacity = d.GetMaxResources();
        TickEvent loot = new(d.GetResourceSpawnRate(), spawnResource);
        actions.Add(loot);
        for (int i = 0; i < d.GetInitialResources(); i++)
        {
            spawnResource();
        }

        enemyBase = d.GetEnemyBase();
        enemyCapacity = d.GetMaxEnemies();
        enemyLevel = d.GetEnemyLevel();
        enemyWalkRadius = d.GetEnemyWalkRadius();
        TickEvent enemy = new(d.GetEnemySpawnRate(), spawnEnemy);
        actions.Add(enemy);
        for (int i = 0; i < d.GetInitialEnemies(); i++)
        {
            spawnEnemy();
        }
    }

    private void spawnResource()
    {
        if (curResources >= resourceCapacity) return;

        Vector2 newPos = data.getResourcePosition();
        LootItem spawned = Instantiate(resourcePrefab, new Vector3(newPos.x, newPos.y, -2), Quaternion.identity).GetComponent<LootItem>();
        spawned.Setup(resourceData, onCollected, 1);
        spawned.transform.SetParent(transform, false);
        curResources++;
    }

    private void spawnEnemy()
    {
        if (curEnemies >= enemyCapacity) return;

        Vector2 newPos = data.getEnemyPosition();
        EnemyBase spawned = Instantiate(enemyBase.gameObject, new Vector3(newPos.x, newPos.y, -2), Quaternion.identity).GetComponent<EnemyBase>();
        spawned.Prepare(enemyLevel, onKilled, pauseController);
        spawned.transform.SetParent(transform, false);
        spawned.SetBounds(transform.position, enemyWalkRadius);
        curEnemies++;
    }

    private void onCollected(CollectableData d)
    {
        curResources--;
    }

    public void Tick()
    {
        foreach (TickEvent e in actions)
        {
            e.Tick();
        }
    }

    private void onKilled(int level, List<LootAmount> loot, Vector3 pos)
    {
        curEnemies -= 1;
        spawnLoot(loot, pos);
    }
}

class TickEvent
{
    private int time;
    private Action action;
    private int timer = 0;

    public TickEvent(int t, Action a)
    {
        time = t;
        action = a;
    }

    public void Tick()
    {
        timer++;
        if (timer == time)
        {
            timer = 0;
            action();
        }
    }
}