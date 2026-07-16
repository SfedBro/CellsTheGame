using UnityEngine;
using System.Linq;

public class PlayerInventory : MonoBehaviour, IInventoryProvider, IGameService
{
    public static PlayerInventory Instance;

    public Inventory ForagingInventory { get; private set; } = new();
    public Inventory FactoryInventory { get; private set; } = new();

    // Default values if no modules are equipped
    [SerializeField] private int baseForagingSlots = 12;
    [SerializeField] private int baseForagingStack = 50;
    
    [SerializeField] private int factorySlotCount = 100;

    public int BaseForagingSlots => baseForagingSlots;
    public int FactorySlotCount => factorySlotCount;

    // Interface implementation (defaulting to Foraging for general interactions)
    public Inventory Inventory => ForagingInventory;

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        ForagingInventory.Initialize(baseForagingSlots);
        ForagingInventory.SetMaxStackSize(baseForagingStack);
        
        FactoryInventory.Initialize(factorySlotCount);
        // Factory items can have huge stack sizes, e.g. 9999
        FactoryInventory.SetMaxStackSize(9999);

        DontDestroyOnLoad(gameObject);
    }

    public void StartService()
    {
        if (Instance != this) return;
        Load();

        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged += HandleModuleChanged;
            UpdateForagingLimits();
        }
    }

    private void OnDestroy()
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged -= HandleModuleChanged;
        }
    }

    private void HandleModuleChanged(PlayerModule module, bool isEquipped)
    {
        if (module.moduleType == ModuleType.Body || module.moduleType == ModuleType.Move)
        {
            UpdateForagingLimits();
        }
    }

    private void UpdateForagingLimits()
    {
        int newSize = baseForagingSlots;
        int newStack = baseForagingStack;

        foreach (var mod in PlayerModuleManager.Instance.EquipedModules)
        {
            if (mod.moduleType == ModuleType.Body && mod.inventorySize > 0)
            {
                newSize = mod.inventorySize;
            }
            if (mod.moduleType == ModuleType.Move && mod.stackCapacity > 0)
            {
                newStack = mod.stackCapacity;
            }
        }

        ForagingInventory.Resize(newSize);
        ForagingInventory.SetMaxStackSize(newStack);
    }

    private string saveKey = "PlayerInventorySave";

    private SaveData.PlayerInventoryData GetSaveSnapshot()
    {
        return new SaveData.PlayerInventoryData()
        {
            foragingInventory = this.ForagingInventory,
            factoryInventory = this.FactoryInventory
        };
    }

    public void Save()
    {
        SaveManager.Save(saveKey, GetSaveSnapshot());
        Debug.Log("Saved PlayerInventory");
    }

    public void Load()
    {
        var data = SaveManager.Load<SaveData.PlayerInventoryData>(saveKey);
        if (data != null)
        {
            if (data.foragingInventory != null && data.foragingInventory.slots != null)
            {
                this.ForagingInventory = data.foragingInventory;
            }
            if (data.factoryInventory != null && data.factoryInventory.slots != null)
            {
                this.FactoryInventory = data.factoryInventory;
            }
        }
        UpdateForagingLimits();
        Debug.Log("Loaded PlayerInventory");
    }
}