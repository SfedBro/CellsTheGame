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
}