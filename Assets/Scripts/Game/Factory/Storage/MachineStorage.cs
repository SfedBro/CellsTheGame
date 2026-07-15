using System.Collections.Generic;
using UnityEngine;

public class MachineStorage : FactoryBlock, IInteractable, IInventoryProvider
{
    public ResourcesManager resourcesManager;
    [SerializeField]
    private Inventory inventory = new Inventory();
    
    public Inventory LocalInventory => inventory;
    public Inventory Inventory => Multiblock != null ? Multiblock.sharedInventory : inventory;

    public MachineStorageMultiblock Multiblock { get; private set; }
    public void SetMultiblock(MachineStorageMultiblock mb) => Multiblock = mb;

    [SerializeField]
    private ConveyorItemView conveyorItemPrefab;

    protected override void Start()
    {
        base.Start();
        resourcesManager = FindAnyObjectByType<ResourcesManager>();
        if (resourcesManager != null) resourcesManager.RegisterStorage(this);
        StorageMultiblockManager.RecalculateMultiblocks();
    }

    public override void OnPlaced()
    {
        base.OnPlaced();
        StorageMultiblockManager.RecalculateMultiblocks();
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        if (resourcesManager != null) resourcesManager.UnregisterStorage(this);
        StorageMultiblockManager.RecalculateMultiblocks();
    }

    public override void RebuildConnections()
    {
        base.RebuildConnections();
        StorageMultiblockManager.RecalculateMultiblocks();
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
        if (Inventory.CurrentTotalAmount <= 0) 
        {
            return;
        }

        // Find all output ports that are connected to other blocks
        List<Port> outPorts = Ports.FindAll(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPorts == null || outPorts.Count == 0) 
        {
            return;
        }

        foreach (var outPort in outPorts)
        {
            // If the connected block is another storage in the same multiblock, do not transfer items to it
            if (outPort.ConnectedBlock is MachineStorage targetStorage && this.Multiblock != null && targetStorage.Multiblock == this.Multiblock)
            {
                continue;
            }

            // Find any item to output
            ItemType typeToOutput = ItemType.OreIron;
            bool hasItem = false;
            if (Inventory.slots != null)
            {
                foreach (var slot in Inventory.slots)
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

                Debug.Log($"[Storage] Successfully output {typeToOutput} to {outPort.ConnectedBlock.name}");
                Inventory.RemoveItem(typeToOutput);
                return; // Successfully output 1 item, stop checking other ports for this tick
            }
        }
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (receivingPort == null || !receivingPort.IsInput) return false;

        if (Inventory.AddItem(item.Type))
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
        if (PlayerInventoryWindow.Instance != null)
        {
            PlayerInventoryWindow.Instance.SelectMachine(this);
            PlayerInventoryWindow.Instance.Open();
            if (PlayerInventoryWindow.Instance.rightPanel != null)
            {
                PlayerInventoryWindow.Instance.rightPanel.SetTab(PanelTabType.Machine);
            }
        }
        else
        {
            Debug.LogError("[MachineStorage] PlayerInventoryWindow not found in scene!");
        }
    }
}
