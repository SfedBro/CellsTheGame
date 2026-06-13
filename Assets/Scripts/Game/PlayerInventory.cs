using UnityEngine;

public class PlayerInventory : MonoBehaviour, IInventoryProvider, IGameService
{
    public static PlayerInventory Instance;

    private Inventory inventory = new();

    public Inventory Inventory => inventory;

    [SerializeField] private int startingSlotCount = 36;

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        inventory.Initialize(startingSlotCount); // Initialize player inventory with slots
        DontDestroyOnLoad(gameObject);
    }

    public void StartService()
    {
        if (Instance != this) return;
    }
}