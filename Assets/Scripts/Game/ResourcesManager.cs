using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager instance;
    [SerializeField] private List<Sprite> ResourceSpritesForItemSprites;
    [SerializeField] private Sprite defaultSprite;
    protected Inventory inventory;

    private List<Action<ItemType, int>> observers;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        inventory = new Inventory();
        DontDestroyOnLoad(gameObject);
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
            for (int i = 0; i < amount; i++)
            {
                inventory.RemoveItem(t);
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                inventory.AddItem(t);
            }
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
        foreach(var itemType in inventory.items.Keys)
        {
            inventory.items[itemType] = 0;
            foreach (var action in observers)
            {
                action(itemType, getResourceAmount(itemType));
            }
        }
    }
}
