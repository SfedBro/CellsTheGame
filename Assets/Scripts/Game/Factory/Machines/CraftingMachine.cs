using System.Collections.Generic;
using UnityEngine;

public class CraftingMachine : FactoryBlock
{
    [Header("Inventory")]
    public Inventory inventory = new();

    [Header("Processing")]
    public float progress;
    public float processTime = 2f;
    public ItemType inputType = ItemType.OreIron;
    public ItemType outputType = ItemType.IngotIron;
    
    [SerializeField]
    private ConveyorItemView conveyorItemPrefab;

    public override void Tick()
    {
        ProcessRecipe();
        TryOutput();
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (item.Type != inputType) return false;
        
        if (inventory.AddItem(item.Type))
        {
            if (item.View != null) Destroy(item.View.gameObject);
            return true;
        }
        return false;
    }

    private void ProcessRecipe()
    {
        if (inventory.items.GetValueOrDefault(inputType) > 0)
        {
            float tickDelta = FactoryTickManager.Instance.TickRate;
            if (progress < processTime)
            {
                progress += tickDelta;
            }
            else
            {
                inventory.RemoveItem(inputType);
                inventory.AddItem(outputType);
                progress = 0f;
            }
        }
    }

    private void TryOutput()
    {
        if (inventory.items.GetValueOrDefault(outputType) <= 0) return;

        Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPort == null) return;

        ConveyorItem item = new ConveyorItem();
        item.Type = outputType;
        
        ConveyorItemView itemView = Instantiate(conveyorItemPrefab, transform.position, Quaternion.identity);
        itemView.GetComponent<SpriteRenderer>().sprite = ResourcesManager.instance.getResourceSprite(outputType);
        item.View = itemView;

        if (outPort.ConnectedBlock.TryReceiveItem(item, outPort.ConnectedPort))
        {
            inventory.RemoveItem(outputType);
        }
        else
        {
            Destroy(itemView.gameObject);
        }
    }
}
