using System.Collections.Generic;
using UnityEngine;

public static class StorageMultiblockManager
{
    public static void RecalculateMultiblocks()
    {
        var storages = Object.FindObjectsByType<MachineStorage>(FindObjectsSortMode.None);
        var unvisited = new HashSet<MachineStorage>(storages);

        // Clear active multiblocks on all storages first
        foreach (var s in storages)
        {
            s.SetMultiblock(null);
        }

        while (unvisited.Count > 0)
        {
            var start = System.Linq.Enumerable.First(unvisited);
            var group = new List<MachineStorage>();
            var queue = new Queue<MachineStorage>();

            queue.Enqueue(start);
            unvisited.Remove(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                // Find connected unvisited storage blocks
                foreach (var neighbor in GetConnectedNeighbors(current, unvisited))
                {
                    queue.Enqueue(neighbor);
                    unvisited.Remove(neighbor);
                }
            }

            // Only form a multiblock if there is more than 1 storage in the group
            if (group.Count > 1)
            {
                var multiblock = new MachineStorageMultiblock();
                multiblock.storages = group;
                multiblock.Rebuild();

                foreach (var s in group)
                {
                    s.SetMultiblock(multiblock);
                }
            }
        }

        // Refresh UI if open
        if (PlayerInventoryWindow.Instance != null)
        {
            PlayerInventoryWindow.Instance.RefreshWindow();
        }
    }

    private static List<MachineStorage> GetConnectedNeighbors(MachineStorage storage, HashSet<MachineStorage> candidates)
    {
        var neighbors = new List<MachineStorage>();
        
        foreach (var neighborStorage in candidates)
        {
            // Verify 'storage' has a connection to 'neighborStorage'
            bool storageToNeighbor = false;
            foreach (var port in storage.Ports)
            {
                if (port.ConnectedBlock == neighborStorage)
                {
                    storageToNeighbor = true;
                    break;
                }
            }

            // Verify 'neighborStorage' has a connection to 'storage'
            bool neighborToStorage = false;
            foreach (var port in neighborStorage.Ports)
            {
                if (port.ConnectedBlock == storage)
                {
                    neighborToStorage = true;
                    break;
                }
            }

            // Both must be true for mutual connection merging
            if (storageToNeighbor && neighborToStorage)
            {
                neighbors.Add(neighborStorage);
            }
        }

        return neighbors;
    }
}
