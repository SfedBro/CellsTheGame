using UnityEngine;

public class ResourceGenerator : MonoBehaviour
{
    [Header("Resource generation")]
    public int maxResourcesOnMap = 100;
    public int initialResourcesOnMap = 10;
    public float spawnRate = 3.0f;
    public GameObject[] resources;
    public float generationBoundsMinX;
    public float generationBoundsMaxX;
    public float generationBoundsMinY;
    public float generationBoundsMaxY;

    private float timer = 0f;
    private int resourceCounter;

    void Start()
    {
        for (int i = 0; i < initialResourcesOnMap; i++)
        {
            spawnResource();
        }
        resourceCounter = initialResourcesOnMap;
    }

    void Update()
    {
        if (resourceCounter >= maxResourcesOnMap) return;

        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;
            spawnResource();
        }
    }

    void spawnResource()
    {
        GameObject spawned = Instantiate(resources[Random.Range(0, resources.Length)], new Vector3(Random.Range(generationBoundsMinX, generationBoundsMaxX), Random.Range(generationBoundsMinY, generationBoundsMaxY), 0), Quaternion.identity);
        spawned.transform.parent = this.transform;
    }

    void onCollected()
    {
        resourceCounter--;
    }
}
