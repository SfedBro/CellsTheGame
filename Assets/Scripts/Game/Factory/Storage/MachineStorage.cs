using System.Collections.Generic;
using UnityEditor.SceneManagement;
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
    }

    public override void Initialize()
    {
        base.Initialize();
        inventory.MaxCapacity = 100; // Limit storage to 100 items max
    }

    public override void Tick()
    {
        TryOutput();
    }

    private void TryOutput()
    {
        if (inventory.CurrentTotalAmount <= 0) return;

        Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPort == null) return;

        // Find any item to output
        ItemType typeToOutput = ItemType.OreIron;
        bool hasItem = false;
        foreach (var kvp in inventory.items)
        {
            if (kvp.Value > 0)
            {
                typeToOutput = kvp.Key;
                hasItem = true;
                break;
            }
        }

        if (!hasItem) return;

        ConveyorItem item = new ConveyorItem();
        item.Type = typeToOutput;
        
        ConveyorItemView itemView = null;
        if (conveyorItemPrefab != null)
        {
            itemView = Instantiate(conveyorItemPrefab, transform.position, Quaternion.identity);
            itemView.GetComponent<SpriteRenderer>().sprite = ResourcesManager.instance.getResourceSprite(typeToOutput);
            item.View = itemView;
        }

        if (outPort.ConnectedBlock.TryReceiveItem(item, outPort.ConnectedPort))
        {
            inventory.RemoveItem(typeToOutput);
        }
        else
        {
            if (itemView != null) Destroy(itemView.gameObject);
        }
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
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
        StorageWindow.Instance.Open(this);
    }
}
