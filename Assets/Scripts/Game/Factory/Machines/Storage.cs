using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour, IItemReceiver
{
    public Inventory inventory = new();
    private void Start()
    {
        Grid grid = FindFirstObjectByType<Grid>();

        Vector3Int pos = grid.WorldToCell(transform.position);

        GridManager.Instance.RegisterReceiver(pos, this);
    }
    public bool TryReceiveItem(ConveyorItem item)
    {
        inventory.AddItem(item.Type);

        if (item.View != null)
            Destroy(item.View.gameObject);

        return true;
    }
}