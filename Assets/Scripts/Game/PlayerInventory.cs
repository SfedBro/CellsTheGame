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
        Load();
    }

    private string saveKey = "PlayerInventorySave";

    private SaveData.PlayerInventoryData GetSaveSnapshot()
    {
        var data = new SaveData.PlayerInventoryData()
        {
            inventory = this.inventory
        };
        return data;
    }

    public void Save()
    {
        SaveManager.Save(saveKey, GetSaveSnapshot());
        Debug.Log("Saved PlayerInventory");
    }

    public void Load()
    {
        var data = SaveManager.Load<SaveData.PlayerInventoryData>(saveKey);
        if (data != null && data.inventory != null && data.inventory.slots != null)
        {
            this.inventory = data.inventory;
        }
        Debug.Log("Loaded PlayerInventory");
    }
}