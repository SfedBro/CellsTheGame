using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour, IItemReceiver
{
    public ResourcesManager resourcesManager = new();
    private void Start()
    {
        Grid grid = FindFirstObjectByType<Grid>();

        Vector3Int pos = grid.WorldToCell(transform.position);

        GridManager.Instance.RegisterReceiver(pos, this);

        resourcesManager = FindAnyObjectByType<ResourcesManager>();
    }
    public bool TryReceiveItem(ConveyorItem item)
    {
        resourcesManager.addResourceAmount(item.Type, 1);

        if (item.View != null)
            Destroy(item.View.gameObject);

        return true;
    }
}