using System.Collections.Generic;
using UnityEngine;

public class MachineInterior : MonoBehaviour
{
    [Header("Inventory")]
    public List<ItemStack> inventory = new();

    [Header("Output")]
    public MachineOutput output;

    [Header("Processing")]
    public float progress;
    public float processTime = 2f;

    public void AddItem(ItemType type)
    {
        ItemStack stack = FindItem(type);

        if (stack != null)
        {
            stack.amount++;
            return;
        }

        inventory.Add(new ItemStack
        {
            type = type,
            amount = 1
        });
    }

    private void Update()
    {
        ProcessOre();
    }

    void ProcessOre()
    {
        ItemStack ore = FindItem(ItemType.TestOre);

        if (ore == null || ore.amount <= 0)
        {
            progress = 0;
            return;
        }

        progress += Time.deltaTime;

        if (progress >= processTime)
        {
            progress = 0;
            ore.amount--;
            bool success = output.SpawnItem(ItemType.TestPlate);
            if (!success)
            {
                ore.amount++;
            }
            inventory.RemoveAll(x => x.amount <= 0);
        }
    }

    ItemStack FindItem(ItemType type)
    {
        foreach (var stack in inventory)
        {
            if (stack.type == type)
                return stack;
        }

        return null;
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemType type = ItemType.TestOre;
    public int amount = 0;
}