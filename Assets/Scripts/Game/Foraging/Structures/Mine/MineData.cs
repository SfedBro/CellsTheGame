using UnityEngine;

[CreateAssetMenu(fileName = "NewMine", menuName = "Structures/Mine")]
public class MineData : ScriptableObject
{
    [Header("Self")]
    [SerializeField] private CollectableData collectable;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float allowedRadiusMin;
    [SerializeField] private float allowedRadiusMax;

    [Header("Resource spawn rules")]
    [SerializeField] private float resourceSpawnRadius;
    [SerializeField] private int maxResources;
    [SerializeField] private int initialResources;
    [SerializeField] private int resourceSpawnRate;

    [Header("Enemies spawn rules")]
    [SerializeField] private EnemyBase enemyBase;
    [SerializeField] private float enemiesSpawnRadius;
    [SerializeField] private int maxEnemies;
    [SerializeField] private int initialEnemies;
    [SerializeField] private int enemiesSpawnRate;
    [SerializeField] private int enemyLevel;


    public CollectableData GeItemData() => collectable;
    public Sprite GetSprite() => sprite;
    public int GetMaxResources() => maxResources;
    public int GetInitialResources() => initialResources;
    public int GetResourceSpawnRate() => resourceSpawnRate;
    public EnemyBase GetEnemyBase() => enemyBase;
    public int GetMaxEnemies() => maxEnemies;
    public int GetInitialEnemies() => initialEnemies;
    public int GetEnemySpawnRate() => enemiesSpawnRate;
    public int GetEnemyLevel() => enemyLevel;

    public Vector2 getSelfPosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * (allowedRadiusMin + vec.magnitude * (allowedRadiusMax - allowedRadiusMin));
    }

    public Vector2 getResourcePosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * resourceSpawnRadius;
    }

    public Vector2 getEnemyPosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * resourceSpawnRadius;
    }
}
