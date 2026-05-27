using UnityEngine;

public class ConveyorItem
{
    public ItemType Type = ItemType.TestOre;
    public ConveyorItemView View;
    public bool movedThisTick = false;
}
public enum ItemType
{
    TestOre,
    TestPlate
}