using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ResourcesManager : MonoBehaviour, IGameService
{
    public static ResourcesManager instance;
    [SerializeField] private List<Sprite> ResourceSpritesForItemSprites;
    [SerializeField] private Sprite defaultSprite;
    protected Inventory inventory;

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
            inventory = PlayerInventory.Instance.Inventory;
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

    public int getResourceAmount(ItemType t)
    {
        return inventory.GetAmount(t);
    }

    public void addResourceAmount(ItemType t, int amount)
    {
        if (amount < 0)
        {
            amount *= -1;
            inventory.RemoveItem(t, amount);
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

    public void onPlayerDeath()
    {
        if (inventory.slots == null) return;
        var distinctTypes = inventory.slots.Where(s => !s.IsEmpty).Select(s => s.type).Distinct().ToList();
        
        foreach (var slot in inventory.slots)
        {
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
