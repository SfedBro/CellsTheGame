using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Inventory : ISerializationCallbackReceiver
{
    public int slotCount = 24;
    public int defaultMaxStackSize = 100;

    public System.Action OnInventoryChanged;

    [SerializeField]
    private List<ItemStack> startingItems = new();

    public ItemStack[] slots;

    public void Initialize(int count)
    {
        slotCount = count;
        slots = new ItemStack[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new ItemStack(ItemType.Default, 0);
        }
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (slots == null || slots.Length != slotCount)
        {
            Initialize(slotCount);
            foreach (var item in startingItems)
            {
                AddItem(item.type, item.amount);
            }
        }
    }

    public int CurrentTotalAmount => slots?.Sum(s => s.amount) ?? 0;

    public bool CanAddItem(ItemType type, int amount = 1)
    {
        if (slots == null) return false;
        int remainingAmount = amount;
        foreach (var slot in slots)
        {
            if (slot.type == type && slot.amount < defaultMaxStackSize)
            {
                remainingAmount -= (defaultMaxStackSize - slot.amount);
            }
            else if (slot.IsEmpty)
            {
                remainingAmount -= defaultMaxStackSize;
            }

            if (remainingAmount <= 0) return true;
        }
        return false;
    }

    public bool AddItem(ItemType type, int amount = 1)
    {
        if (slots == null || !CanAddItem(type, amount)) return false;

        int remaining = amount;
        
        foreach (var slot in slots)
        {
            if (slot.type == type && slot.amount < defaultMaxStackSize)
            {
                int space = defaultMaxStackSize - slot.amount;
                if (remaining <= space)
                {
                    slot.amount += remaining;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                else
                {
                    slot.amount += space;
                    remaining -= space;
                }
            }
        }

        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.type = type;
                int space = defaultMaxStackSize;
                if (remaining <= space)
                {
                    slot.amount = remaining;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                else
                {
                    slot.amount = space;
                    remaining -= space;
                }
            }
        }

        if (remaining < amount) OnInventoryChanged?.Invoke();
        return false;
    }

    public int GetAmount(ItemType type)
    {
        if (slots == null) return 0;
        return slots.Where(s => s.type == type).Sum(s => s.amount);
    }

    public bool RemoveItem(ItemType type, int amount = 1)
    {
        if (slots == null || GetAmount(type) < amount) return false;

        int remaining = amount;
        for (int i = slots.Length - 1; i >= 0; i--)
        {
            var slot = slots[i];
            if (slot.type == type && slot.amount > 0)
            {
                if (slot.amount > remaining)
                {
                    slot.amount -= remaining;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                else
                {
                    remaining -= slot.amount;
                    slot.Clear();
                    if (remaining == 0) 
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }
        if (remaining < amount) OnInventoryChanged?.Invoke();
        return false;
    }

    public bool AddItems(Dictionary<ItemType, int> otherItems)
    {
        Inventory clone = Clone();
        foreach (var kvp in otherItems)
        {
            if (!clone.AddItem(kvp.Key, kvp.Value)) return false;
        }

        foreach (var kvp in otherItems) AddItem(kvp.Key, kvp.Value);
        return true;
    }

    public bool AddItems(List<ItemStack> otherItems)
    {
        Inventory clone = Clone();
        foreach (var item in otherItems)
        {
            if (!clone.AddItem(item.type, item.amount)) return false;
        }

        foreach (var item in otherItems) AddItem(item.type, item.amount);
        return true;
    }

    public bool ContainsItems(Dictionary<ItemType, int> requiredItems)
    {
        foreach (var kvp in requiredItems)
        {
            if (GetAmount(kvp.Key) < kvp.Value) return false;
        }
        return true;
    }

    public bool ContainsItems(List<ItemStack> requiredItems)
    {
        foreach (var item in requiredItems)
        {
            if (GetAmount(item.type) < item.amount) return false;
        }
        return true;
    }

    public bool CanAddItems(List<ItemStack> itemsToAdd)
    {
        Inventory clone = Clone();
        foreach (var item in itemsToAdd)
        {
            if (!clone.AddItem(item.type, item.amount)) return false;
        }
        return true;
    }

    public bool RemoveItems(Dictionary<ItemType, int> otherItems)
    {
        if (!ContainsItems(otherItems)) return false;
        foreach (var kvp in otherItems)
        {
            RemoveItem(kvp.Key, kvp.Value);
        }
        return true;
    }

    public bool RemoveItems(List<ItemStack> otherItems)
    {
        if (!ContainsItems(otherItems)) return false;
        foreach (var item in otherItems)
        {
            RemoveItem(item.type, item.amount);
        }
        return true;
    }

    public bool TransferTo(Inventory target, ItemType type, int amount)
    {
        if (GetAmount(type) < amount) return false;
        if (!target.CanAddItem(type, amount)) return false;

        if (RemoveItem(type, amount))
        {
            target.AddItem(type, amount);
            return true;
        }
        return false;
    }

    public Inventory Clone()
    {
        Inventory inv = new Inventory { slotCount = slotCount, defaultMaxStackSize = defaultMaxStackSize };
        inv.Initialize(slotCount);
        for (int i = 0; i < slotCount; i++)
        {
            inv.slots[i].type = slots[i].type;
            inv.slots[i].amount = slots[i].amount;
        }
        return inv;
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

    public bool IsEmpty => amount <= 0 || type == ItemType.Default;

    public void Clear()
    {
        type = ItemType.Default;
        amount = 0;
    }
}
