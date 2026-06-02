using System.Collections.Generic;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager instance;
    [SerializeField] private List<Sprite> ResourceSpritesForItemSprites;
    [SerializeField] private Sprite defaultSprite;
    protected Inventory inventory;
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
        ItemStack stack = inventory.FindItem(t);

        if (stack == null)
        {
            return 0;
        }

        return stack.amount;
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
    }
}
