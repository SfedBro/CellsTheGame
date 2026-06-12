using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Inventory : ISerializationCallbackReceiver
{
    public int MaxCapacity = -1; // -1 means infinite

    [SerializeField]
    private List<ItemStack> startingItems = new();

    public Dictionary<ItemType, int> items = new();

    public void OnBeforeSerialize()
    {
        // Update the inspector list before saving (only needed if we want runtime changes visible)
        // startingItems = ToList();
    }

    public void OnAfterDeserialize()
    {
        // Load items from the inspector list into the dictionary
        items.Clear();
        foreach (var item in startingItems)
        {
            if (items.ContainsKey(item.type))
                items[item.type] += item.amount;
            else
                items[item.type] = item.amount;
        }
    }

    public int CurrentTotalAmount => items.Values.Sum();

    public bool CanAddItem(int amount = 1)
    {
        if (MaxCapacity < 0) return true;
        return CurrentTotalAmount + amount <= MaxCapacity;
    }

    public bool AddItem(ItemType type, int amount = 1)
    {
        if (!CanAddItem(amount)) return false;
        items[type] = GetAmount(type) + amount;
        return true;
    }

    public bool AddItems(Dictionary<ItemType, int> otherItems)
    {
        int totalToAdd = otherItems.Values.Sum();
        if (!CanAddItem(totalToAdd)) return false;

        foreach (var item in otherItems)
        {
            items[item.Key] = GetAmount(item.Key) + item.Value;
        }
        return true;
    }

    public bool ContainsItems(Dictionary<ItemType, int> requiredItems)
    {
        foreach (var item in requiredItems)
        {
            if (GetAmount(item.Key) < item.Value)
                return false;
        }
        return true;
    }

    public int GetAmount(ItemType type)
    {
        return items.GetValueOrDefault(type);
    }

    public bool RemoveItem(ItemType type, int amount = 1)
    {
        if (GetAmount(type) < amount)
            return false;
        items[type] -= amount;
        return true;
    }

    public bool RemoveItems(Dictionary<ItemType, int> otherItems)
    {
        if (!ContainsItems(otherItems))
            return false;

        foreach (var item in otherItems)
        {
            RemoveItem(item.Key, item.Value);
        }
        return true;
    }

    public bool TransferTo(Inventory target, ItemType type, int amount)
    {
        if (GetAmount(type) < amount) return false;
        if (!target.CanAddItem(amount)) return false;

        if (RemoveItem(type, amount))
        {
            target.AddItem(type, amount);
            return true;
        }
        return false;
    }

    public List<ItemStack> ToList()
    {
        List<ItemStack> result = new();
        foreach (var item in items)
        {
            result.Add(new ItemStack(item.Key, item.Value));
        }
        return result;
    }

    public void FromList(List<ItemStack> data)
    {
        items.Clear();
        foreach (var item in data)
        {
            items[item.type] = item.amount;
        }
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemType type = ItemType.Default;
    public int amount = 0;
    public ItemStack(ItemType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}


