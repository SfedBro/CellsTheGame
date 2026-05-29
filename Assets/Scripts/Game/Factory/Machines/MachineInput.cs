using System.Collections.Generic;
using UnityEngine;

public class MachineInput : MonoBehaviour, IItemReceiver
{
    public MachineInterior machine;
    private void Start()
    {
        Grid grid = FindFirstObjectByType<Grid>();

        Vector3Int pos = grid.WorldToCell(transform.position);

        GridManager.Instance.RegisterReceiver(pos, this);
    }
    public bool TryReceiveItem(ConveyorItem item)
    {
        //Debug.Log($"{machine.name} received {item.Type}");

        machine.AddItem(item.Type);

        if (item.View != null)
            Destroy(item.View.gameObject);

        return true;
    }
}
