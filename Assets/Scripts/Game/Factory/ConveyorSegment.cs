using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ConveyorSegment : MonoBehaviour
{
    public ConveyorSegment Next;
    [SerializeField]
    private int slotsLength = 8;
    public ConveyorItem[] Slots;
    private Vector3[] visualSlots;
    private void Start()
    {
        Initialize();
    }
    private void Initialize()
    {
        Slots = new ConveyorItem[slotsLength];
        visualSlots = new Vector3[slotsLength];
        GetVisualPositions();
    }
    private void GetVisualPositions()
    {
        Vector3 dir = transform.right;
        Vector3 start = transform.position - dir * 0.5f;
        Vector3 end = transform.position + dir * 0.5f;
        for (int i = 0; i < slotsLength; i++)
        {
            float t = slotsLength >= 1 ? (float)i / (slotsLength - 1) : 0f;
            visualSlots[i] = Vector3.Lerp(start, end, t);
        }
    }

    float timer;
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 0.2f)
        {
            timer -= 0.2f;

            MoveItems();
        }

        UpdateVisual();
    }
    void MoveItems()
    {
        for (int i = 0; i < slotsLength; i++)
        {
            if (Slots[i] != null)
                Slots[i].movedThisTick = false;
        }
        var lastItem = Slots[slotsLength - 1];
        if (Next != null && Next.InsertItem(lastItem))
        {
            Slots[slotsLength - 1] = null;
        }
        for (int i = slotsLength - 2; i >= 0; i--)
        {
            var item = Slots[i];
            if (item == null || item.movedThisTick)
                continue;
            if (Slots[i + 1] == null)
            {
                Slots[i + 1] = Slots[i];
                Slots[i + 1].movedThisTick = true;
                Slots[i] = null;
            }
        }
    }
    void UpdateVisual()
    {
        for (int i = 0; i < slotsLength; i++)
        {
            if (Slots[i] != null && Slots[i].View != null)
            {
                Slots[i].View.transform.position = visualSlots[i];
            }
        }
    }
    public bool InsertItem(ConveyorItem item)
    {
        if (item == null || Slots[0] != null)
            return false;

        item.movedThisTick = true;
        Slots[0] = item;
        if (item.View != null)
        {
            item.View.transform.position = GetSlotPosition(0);
        }

        return true;
    }
    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= slotsLength)
        {
            throw new System.IndexOutOfRangeException($"Invalid index::ConveyorSegment.cs::101 on object {gameObject.name}");
        }
        return visualSlots[index];
    }
}
