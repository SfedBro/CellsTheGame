using System.Collections.Generic;

public class MachineStorageMultiblock
{
    public List<MachineStorage> storages = new List<MachineStorage>();
    public SharedInventory sharedInventory = new SharedInventory();

    public void Rebuild()
    {
        List<Inventory> inventories = new List<Inventory>();
        foreach (var s in storages)
        {
            if (s != null && s.LocalInventory != null)
            {
                inventories.Add(s.LocalInventory);
            }
        }
        sharedInventory.Rebuild(inventories);
    }
}
