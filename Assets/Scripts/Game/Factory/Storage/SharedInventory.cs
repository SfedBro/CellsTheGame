using System.Collections.Generic;

public class SharedInventory : Inventory
{
    private List<Inventory> subInventories = new List<Inventory>();

    public void Rebuild(List<Inventory> inventories)
    {
        // Unsubscribe from old inventories
        foreach (var inv in subInventories)
        {
            if (inv != null) inv.OnInventoryChanged -= HandleSubInventoryChanged;
        }

        subInventories = inventories;

        // Subscribe to new inventories
        int totalSlots = 0;
        foreach (var inv in subInventories)
        {
            if (inv != null)
            {
                inv.OnInventoryChanged += HandleSubInventoryChanged;
                totalSlots += inv.slotCount;
            }
        }

        slotCount = totalSlots;
        slots = new ItemStack[slotCount];

        int index = 0;
        foreach (var inv in subInventories)
        {
            if (inv == null) continue;
            for (int i = 0; i < inv.slotCount; i++)
            {
                slots[index] = inv.slots[i];
                index++;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    private void HandleSubInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
