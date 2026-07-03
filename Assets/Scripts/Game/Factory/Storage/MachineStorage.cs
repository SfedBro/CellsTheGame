using System.Collections.Generic;
using UnityEngine;

public class MachineStorage : FactoryBlock, IInteractable, IInventoryProvider
{
    public ResourcesManager resourcesManager;
    [SerializeField]
    private Inventory inventory = new Inventory();
    public Inventory Inventory => inventory;

    [SerializeField]
    private ConveyorItemView conveyorItemPrefab;

    protected override void Start()
    {
        base.Start();
        resourcesManager = FindAnyObjectByType<ResourcesManager>();
        if (resourcesManager != null) resourcesManager.RegisterStorage(this);
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        if (resourcesManager != null) resourcesManager.UnregisterStorage(this);
    }

    [SerializeField]
    private int startingSlotCount = 24;

    public override void Initialize()
    {
        if (isInitialized) return;
        base.Initialize();
        inventory.Initialize(startingSlotCount); // Limit storage to slots
    }

    public override string GetSaveState()
    {
        return JsonUtility.ToJson(inventory);
    }

    public override void LoadSaveState(string stateJson)
    {
        if (!string.IsNullOrEmpty(stateJson))
        {
            JsonUtility.FromJsonOverwrite(stateJson, inventory);
        }
    }

    public override void Tick()
    {
        // Debug.Log("[Storage] Tick called.");
        TryOutput();
    }

    private void TryOutput()
    {
        if (inventory.CurrentTotalAmount <= 0) 
        {
            // // Debug.Log("[Storage] Inventory is empty!"); 
            return;
        }

        Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPort == null) 
        {
            // Debug.Log("[Storage] No connected output port found!");
            return;
        }

        // Find any item to output
        ItemType typeToOutput = ItemType.OreIron;
        bool hasItem = false;
        if (inventory.slots != null)
        {
            foreach (var slot in inventory.slots)
            {
                if (!slot.IsEmpty)
                {
                    typeToOutput = slot.type;
                    hasItem = true;
                    break;
                }
            }
        }

        if (!hasItem) return;

        ConveyorItem item = new ConveyorItem();
        item.Type = typeToOutput;

        if (outPort.ConnectedBlock.TryReceiveItem(item, outPort.ConnectedPort))
        {
            ConveyorItemView itemView = null;
            if (conveyorItemPrefab != null)
            {
                itemView = Instantiate(conveyorItemPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                GameObject go = new GameObject("ConveyorItem");
                go.transform.position = transform.position;
                float scale = ResourcesManager.instance.getResourceScale(typeToOutput);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                itemView = go.AddComponent<ConveyorItemView>();
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 32767;
            }

            var sr = itemView.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) 
            {
                sr.sprite = ResourcesManager.instance.getResourceSprite(typeToOutput);
                sr.sortingOrder = 5;
            }
            item.View = itemView;

            // Debug.Log($"[Storage] Successfully output {typeToOutput} to {outPort.ConnectedBlock.name}");
            inventory.RemoveItem(typeToOutput);
        }
        else
        {
            // Debug.Log($"[Storage] Failed to output {typeToOutput} to {outPort.ConnectedBlock.name} (Conveyor full?)");
        }
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (receivingPort == null || !receivingPort.IsInput) return false;

        if (inventory.AddItem(item.Type))
        {
            if (item.View != null)
                Destroy(item.View.gameObject);
            return true;
        }
        return false;
    }

    public void InsertItems(Dictionary<ItemType, int> items)
    {
    }

    public void RemoveItems(Dictionary<ItemType, int> items)
    {
        PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
        InventoryUtils.Transfer(this, playerInventory, ItemType.OreIron, 10);
    }

    public void Interact(PlayerInteractor player)
    {
        // Debug.Log("[MachineStorage] Interact called!");
        if (StorageWindow.Instance == null)
        {
            // Debug.Log("[MachineStorage] StorageWindow.Instance is null, searching for it...");
            StorageWindow.Instance = FindFirstObjectByType<StorageWindow>(FindObjectsInactive.Include);
        }
        
        if (StorageWindow.Instance != null)
        {
            // Debug.Log($"[MachineStorage] Opening StorageWindow: {StorageWindow.Instance.name}");
            StorageWindow.Instance.Open(this);
        }
        else
        {
            // Debug.LogError("[MachineStorage] StorageWindow not found in scene!");
        }
    }
}
