using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ResourcesManager : MonoBehaviour, IGameService
{
    public static ResourcesManager instance;
    [SerializeField] private List<Sprite> ResourceSpritesForItemSprites;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private float defaultScale = 1f;
    protected Inventory inventory;

    [System.Serializable]
    public struct ResourceVisualData
    {
        public ItemType type;
        public float conveyorScale;
    }

    [Header("Visual Overrides")]
    public List<ResourceVisualData> resourceVisuals = new List<ResourceVisualData>();

    private List<Action<ItemType, int>> observers = new List<Action<ItemType, int>>();
    
    public void InitializeService()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartService()
    {
        if (instance != this) return;

        // Link to PlayerInventory if it exists
        if (PlayerInventory.Instance != null)
        {
            inventory = PlayerInventory.Instance.FactoryInventory;
        }
        else
        {
            inventory = new Inventory();
            inventory.Initialize(100);
        }
    }

    public Sprite getResourceSprite(ItemType item)
    {
        if (ResourceSpritesForItemSprites.Count > (int)item)
        {
            return ResourceSpritesForItemSprites[(int)item];
        }
        return defaultSprite;
    }

    private List<MachineStorage> allStorages = new List<MachineStorage>();

    public void RegisterStorage(MachineStorage s)
    {
        if (!allStorages.Contains(s)) allStorages.Add(s);
    }

    public void UnregisterStorage(MachineStorage s)
    {
        allStorages.Remove(s);
    }

    public float getResourceScale(ItemType type)
    {
        if (resourceVisuals != null)
        {
            foreach (var visual in resourceVisuals)
            {
                if (visual.type == type) return visual.conveyorScale;
            }
        }
        return defaultScale;
    }

    public int getResourceAmount(ItemType t)
    {
        int total = inventory.GetAmount(t);
        foreach (var s in allStorages)
        {
            if (s != null && s.Inventory != null)
                total += s.Inventory.GetAmount(t);
        }
        return total;
    }

    public void addResourceAmount(ItemType t, int amount)
    {
        if (amount < 0)
        {
            amount *= -1;
            
            // Сначала списываем с хранилищ на базе
            for (int i = allStorages.Count - 1; i >= 0; i--)
            {
                var s = allStorages[i];
                if (s == null)
                {
                    allStorages.RemoveAt(i);
                    continue;
                }

                int available = s.Inventory.GetAmount(t);
                int toRemove = Mathf.Min(amount, available);
                if (toRemove > 0)
                {
                    s.Inventory.RemoveItem(t, toRemove);
                    amount -= toRemove;
                }
                if (amount <= 0) break;
            }

            // Если не хватило на складах, списываем из личного инвентаря
            if (amount > 0)
            {
                inventory.RemoveItem(t, amount);
            }
        }
        else
        {
            inventory.AddItem(t, amount);
        }

        foreach (var action in observers)
        {
            action(t, getResourceAmount(t));
        }
    }

    public void Subscrive(Action<ItemType, int> action)
    {
        observers.Add(action);
    }

    public void Unsubscrive(Action<ItemType, int> action)
    {
        observers.Remove(action);
    }

    public void onPlayerDeath(EnemyBase killer)
    {
        if (inventory.slots == null) return;
        var distinctTypes = inventory.slots.Where(s => !s.IsEmpty).Select(s => s.type).Distinct().ToList();
        
        foreach (var slot in inventory.slots)
        {
            if (slot.amount > 0)
            {
                killer.AddLoot(slot.type, slot.amount);
            }

            slot.Clear();
        }

        foreach (var itemType in distinctTypes)
        {
            foreach (var action in observers)
            {
                action(itemType, 0);
            }
        }
    }
}
