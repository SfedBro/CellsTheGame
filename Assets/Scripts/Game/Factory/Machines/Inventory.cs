using System.Collections.Generic;

[System.Serializable]
public class Inventory
{
    public List<ItemStack> items = new();

    public void AddItem(ItemType type)
    {
        ItemStack stack = FindItem(type);

        if (stack != null)
        {
            stack.amount++;
            return;
        }

        items.Add(new ItemStack
        {
            type = type,
            amount = 1
        });
    }

    public ItemStack FindItem(ItemType type)
    {
        foreach (var stack in items)
        {
            if (stack.type == type)
                return stack;
        }

        return null;
    }
    public bool RemoveItem(ItemType type)
    {
        ItemStack ore = FindItem(type);
        if (ore.amount < 1)
            return false;
        ore.amount--;
        items.RemoveAll(x => x.amount <= 0);
        return true;
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemType type = ItemType.Default;
    public int amount = 0;
}