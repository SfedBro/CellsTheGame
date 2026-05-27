using UnityEngine;

public class MachineOutput: MonoBehaviour
{
    public ConveyorItemView itemPrefab;
    public ConveyorSegment outputConveyor;
    public bool SpawnItem(ItemType type)
    {
        if (outputConveyor == null)
            return false;

        if (outputConveyor.Slots[0] != null)
            return false;

        ConveyorItem item = new ConveyorItem();

        item.Type = type;

        item.View = Instantiate(itemPrefab, outputConveyor.GetSlotPosition(0), Quaternion.identity);

        return outputConveyor.InsertItem(item);
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            SpawnItem(ItemType.TestOre);
        }
    }
}
