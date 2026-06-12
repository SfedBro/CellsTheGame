public static class InventoryUtils
{
    public static bool Transfer(IInventoryProvider from, IInventoryProvider to, ItemType type, int amount)
    {
        if (from.Inventory.GetAmount(type) < amount)
            return false;

        from.Inventory.RemoveItem(type, amount);
        to.Inventory.AddItem(type, amount);

        return true;
    }

    public static void SwapOrMergeSlots(Inventory sourceInv, int sourceIndex, Inventory targetInv, int targetIndex)
    {
        if (sourceInv == null || targetInv == null) return;
        if (sourceIndex < 0 || sourceIndex >= sourceInv.slotCount) return;
        if (targetIndex < 0 || targetIndex >= targetInv.slotCount) return;

        var sourceSlot = sourceInv.slots[sourceIndex];
        var targetSlot = targetInv.slots[targetIndex];

        if (sourceSlot.IsEmpty) return;

        if (targetSlot.IsEmpty)
        {
            // Move
            targetSlot.type = sourceSlot.type;
            targetSlot.amount = sourceSlot.amount;
            sourceSlot.Clear();
        }
        else if (targetSlot.type == sourceSlot.type)
        {
            // Merge
            int space = targetInv.defaultMaxStackSize - targetSlot.amount;
            if (space > 0)
            {
                if (sourceSlot.amount <= space)
                {
                    targetSlot.amount += sourceSlot.amount;
                    sourceSlot.Clear();
                }
                else
                {
                    targetSlot.amount += space;
                    sourceSlot.amount -= space;
                }
            }
        }
        else
        {
            // Swap
            var tempType = targetSlot.type;
            var tempAmount = targetSlot.amount;

            targetSlot.type = sourceSlot.type;
            targetSlot.amount = sourceSlot.amount;

            sourceSlot.type = tempType;
            sourceSlot.amount = tempAmount;
        }
    }
}