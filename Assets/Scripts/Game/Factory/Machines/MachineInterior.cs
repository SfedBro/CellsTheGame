using System.Collections.Generic;
using UnityEngine;

public class MachineInterior : MonoBehaviour
{
    [Header("Inventory")]
    public Inventory inventory = new();

    [Header("Output")]
    public MachineOutput output;

    [Header("Processing")]
    public float progress;
    public float processTime = 2f;

    private void Update()
    {
        ProcessOre();
    }

    void ProcessOre()
    {
        var item = inventory.FindItem(ItemType.Default);
        if (item != null && item.amount > 0)
        {
            if (progress < processTime)
            {
                progress += Time.deltaTime;
            }
            else
            {
                inventory.RemoveItem(ItemType.Default);
                if (output.SpawnItem(ItemType.Default))
                    progress = 0;
            }
        }
    }
}