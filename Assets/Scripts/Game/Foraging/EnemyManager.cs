using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy spawning")]
    public int maxEnemiesOnMap = 5;
    public int initialEnemiesOnMap = 3;
    public float spawnRate = 5.0f;
    public GameObject enemy;
    public float spawnBoundsMinX;
    public float spawnBoundsMaxX;
    public float spawnBoundsMinY;
    public float spawnBoundsMaxY;

    private float timer = 0f;
    private int enemyCounter;

    void Start()
    {
        for (int i = 0; i < initialEnemiesOnMap; i++)
        {
            spawnEnemy();
        }
        enemyCounter = initialEnemiesOnMap;
    }

    void Update()
    {
        if (enemyCounter >= maxEnemiesOnMap) return;

        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            spawnEnemy();
        }
    }

    void spawnEnemy()
    {
        GameObject spawned = Instantiate(enemy, new Vector3(Random.Range(spawnBoundsMinX, spawnBoundsMaxX), Random.Range(spawnBoundsMinY, spawnBoundsMaxY), 0), Quaternion.identity);
        spawned.transform.parent = this.transform;
    }

    void onKilled()
    {
        enemyCounter--;
    }
}
