using System.Collections.Generic;
using UnityEngine;

public class ResourceGenerator : MonoBehaviour
{
    [Header("Resource generation")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject resourcePrefab;
    [SerializeField] private List<CollectableData> resourcesData;
    [SerializeField] private List<int> initialAmount;
    [SerializeField] private List<int> minAmount;
    [SerializeField] private List<int> maxAmount;
    [SerializeField] private int generationMaxAttemp = 5;

    private ResourcesManager rm = ResourcesManager.instance;

    private List<int> curAmount = new();
    private Dictionary<ItemType, int> typeToIndex = new();

    private Rect cameraView;

    void Start()
    {
        float height = 2f * playerCamera.orthographicSize;
        float width = height * playerCamera.aspect;
        Vector3 bottomLeft = playerCamera.transform.position - new Vector3(width/2, height/2, 0);
        cameraView = new Rect(bottomLeft.x, bottomLeft.y, width, height);

        for (int i = 0; i < resourcesData.Count; i++)
        {
            CollectableData data = resourcesData[i];
            int amount = initialAmount[i];
            curAmount.Add(amount);
            typeToIndex.Add(data.GetItemType(), i);

            for (int j = 0; j < amount; j++)
            {
                spawnResource(data);
            }
        }
    }

    private void spawnResource(CollectableData data)
    {
        cameraView.center = playerCamera.transform.position;
        Vector2 newPos = data.getNewPosition();

        for (int i = 0; i < generationMaxAttemp; i++)
        {
            if (cameraView.Contains(newPos))
            {
                newPos = data.getNewPosition();
            }
            else
            {
                break;
            }
        }

        LootItem spawned = Instantiate(resourcePrefab, new Vector3(newPos.x, newPos.y, 0), Quaternion.identity).GetComponent<LootItem>();
        spawned.Setup(data, onCollected, 1);
        spawned.transform.parent = transform;
    }

    private void onCollected(CollectableData data)
    {
        int index = typeToIndex.GetValueOrDefault(data.GetItemType(), -1);
        if (index == -1) return;

        curAmount[index]--;

        int newIndex = typeToIndex.GetValueOrDefault(data.newType(), -1);
        if (newIndex == -1) return;

        curAmount[newIndex]++;
        spawnResource(resourcesData[newIndex]);
    }

    public void spawnLoot(Vector3 pos, ItemType type, int amount)
    {
        int index = typeToIndex.GetValueOrDefault(type, -1);
        if (index == -1) return;

        LootItem spawned = Instantiate(resourcePrefab, pos, Quaternion.identity).GetComponent<LootItem>();
        spawned.Setup(resourcesData[index], null, amount);
        spawned.transform.localScale = new Vector3(amount, amount, 1);
        spawned.transform.parent = transform;
    }
}
