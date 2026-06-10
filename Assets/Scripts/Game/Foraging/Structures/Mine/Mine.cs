using System;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    private MineData data;
    private SpriteRenderer sr;
    private List<TickEvent> actions = new();

    [Header("Resource Spawn")]
    [SerializeField] private GameObject resourcePrefab;
    private CollectableData resourceData;
    private int resourceCapacity;
    private int curResources = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(MineData d)
    {
        data = d;
        sr.sprite = d.GetSprite();
        resourceData = d.GeItemData();
        resourceCapacity = d.GetMaxResources();

        TickEvent loot = new(d.GetSpawnRate(), spawnResource);
        actions.Add(loot);

        for (int i = 0; i < d.GetInitialResources(); i++)
        {
            spawnResource();
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