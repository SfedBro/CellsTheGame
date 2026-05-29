using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager instance;
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
