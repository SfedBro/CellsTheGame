using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour, IItemReceiver
{
    public Inventory inventory = new();

    public bool TryReceiveItem(ConveyorItem item)
    {
        inventory.AddItem(item.Type);

        if (item.View != null)
            Destroy(item.View.gameObject);

        return true;
    }
}