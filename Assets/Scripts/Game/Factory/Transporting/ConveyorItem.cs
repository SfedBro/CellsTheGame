using UnityEngine;

public class ConveyorItem
{
    public ItemType Type = ItemType.Default;
    public ConveyorItemView View;
    public bool movedThisTick = false;
}

public enum ItemType
{
    Default,
    OreIron,
    OreCopper,
    OreTin,
    IngotIron,
    IngotCopper,
    IngotTin,
    PlateIron,
    PlateCopper,
    PlateTin
}