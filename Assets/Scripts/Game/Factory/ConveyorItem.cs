using UnityEngine;

public class ConveyorItem
{
    public ItemType Type = ItemType.TestOre;
    public ConveyorItemView View;
    public bool movedThisTick = false;
}

public enum ItemType
{
    TestOre = 0,
    TestPlate = 1,
    TestCopper = 2,
}