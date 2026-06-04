using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy spawning")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private int generationMaxAttemp = 5;
    [SerializeField] private float respawnTime = 5.0f;
    [SerializeField] private List<EnemyBase> enemies;
    [SerializeField] private List<EnemyLevelGeneration> enemiesAmountPerLevel;
    [SerializeField] private ResourceGenerator rg;

    private float timer = 0f;
    private Queue<int> enemiesLevelQueue = new();
    private Rect cameraView;


    void Start()
    {
        float height = 2f * playerCamera.orthographicSize;
        float width = height * playerCamera.aspect;
        Vector3 bottomLeft = playerCamera.transform.position - new Vector3(width/2, height/2, 0);
        cameraView = new Rect(bottomLeft.x, bottomLeft.y, width, height);

        for (int level = 1; level < enemiesAmountPerLevel.Count + 1; level++)
        {
            for (int i = 0; i < enemiesAmountPerLevel[level - 1].amount; i++) {
                spawnEnemy(level);
            }
        }
    }

    void Update()
    {
        if (enemiesLevelQueue.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= respawnTime)
        {
            timer = 0f;
            spawnEnemy(enemiesLevelQueue.Dequeue());
        }
    }

    void spawnEnemy(int level)
    {
        cameraView.center = playerCamera.transform.position;
        Vector3 newPos = enemiesAmountPerLevel[level - 1].getNewPosition();

        for (int i = 0; i < generationMaxAttemp; i++)
        {
            if (cameraView.Contains(newPos))
            {
                newPos = enemiesAmountPerLevel[level - 1].getNewPosition();
            }
            else
            {
                break;
            }
        }

        GameObject spawned = Instantiate(enemies[UnityEngine.Random.Range(0, enemies.Count)].gameObject, newPos, Quaternion.identity);
        spawned.transform.parent = transform;
        spawned.transform.localScale = new Vector3((level + 1) * 0.5f, (level + 1) * 0.5f, 0);
        spawned.GetComponent<EnemyBase>().Prepare(level, onKilled);
    }

    void onKilled(int level, List<LootAmount> loot, Vector3 pos)
    {
        enemiesAmountPerLevel[level - 1].amount -= 1;

        int newLevel = math.clamp(enemiesAmountPerLevel[level - 1].NextLevel(level), 1, enemiesAmountPerLevel.Count);
        enemiesAmountPerLevel[newLevel - 1].amount += 1;
        enemiesLevelQueue.Enqueue(newLevel);

        foreach (LootAmount l in loot)
        {
            rg.spawnLoot(pos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.5f), l.GetItemType(), l.GetAmount());
        }
    }
}

[System.Serializable]
public class EnemyLevelGeneration
{
    [Header("Amount")]
    [SerializeField] public int amount;
    [SerializeField] private float upgradeChance;

    [Header("Spawn Bounds")]
    [SerializeField] private int allowedRadiusMin;
    [SerializeField] private int allowedRadiusMax;

    public Vector3 getNewPosition()
    {
        Vector2 vec = UnityEngine.Random.insideUnitCircle;
        return vec * (allowedRadiusMin + vec.magnitude * (allowedRadiusMax - allowedRadiusMin));
    }

    public int NextLevel(int cur)
    {
        if (UnityEngine.Random.Range(0, 1) <= upgradeChance)
        {
            return cur + 1;
        }

        return cur;
    }
}