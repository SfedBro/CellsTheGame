using UnityEngine;

public class PlayerInventory : MonoBehaviour, IInventoryProvider
{
    public static PlayerInventory Instance;

    private Inventory inventory = new();

    public Inventory Inventory => inventory;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }
}