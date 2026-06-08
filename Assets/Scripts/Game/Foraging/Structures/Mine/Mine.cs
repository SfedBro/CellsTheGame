using UnityEngine;

public class Mine : MonoBehaviour
{
    private MineData data;
    private SpriteRenderer sr;

    [Header("Resource Spawn")]
    [SerializeField] private GameObject resourcePrefab;
    private CollectableData resourceData;
    private int resourceCapacity;
    private int curResources = 0;
    private float newResourceCoolDown;
    private float newResourceTimer = 0f;

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
        newResourceCoolDown = d.GetSpawnRate();

        for (int i = 0; i < d.GetInitialResources(); i++)
        {
            spawnResource();
        }
    }

    private void spawnResource()
    {
        Vector2 newPos = data.getResourcePosition();
        LootItem spawned = Instantiate(resourcePrefab, newPos, Quaternion.identity).GetComponent<LootItem>();
        spawned.Setup(resourceData, onCollected, 1);
        spawned.transform.SetParent(transform, false);
        curResources++;
    }

    private void onCollected(CollectableData d)
    {
        curResources--;
    }

    void Update()
    {
        if (curResources < resourceCapacity)
        {
            newResourceTimer += Time.deltaTime;

            if (newResourceTimer >= newResourceCoolDown)
            {
                newResourceTimer = 0f;
                spawnResource();
            }
        }
    }
}
