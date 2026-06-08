using UnityEngine;

[CreateAssetMenu(fileName = "NewMine", menuName = "Structures/Mine")]
public class MineData : ScriptableObject
{
    [Header("Self")]
    [SerializeField] private CollectableData collectable;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float allowedRadiusMin;
    [SerializeField] private float allowedRadiusMax;

    [Header("Spawn rules")]
    [SerializeField] private float spawnRadius;
    [SerializeField] private int maxResources;
    [SerializeField] private int initialResources;
    [SerializeField] private float spawnRate;


    public CollectableData GeItemData() => collectable;
    public Sprite GetSprite() => sprite;
    public int GetMaxResources() => maxResources;
    public int GetInitialResources() => initialResources;
    public float GetSpawnRate() => spawnRate;

    public Vector2 getSelfPosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * (allowedRadiusMin + vec.magnitude * (allowedRadiusMax - allowedRadiusMin));
    }

    public Vector2 getResourcePosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * spawnRadius;
    }
}
