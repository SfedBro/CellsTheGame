using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ConveyorSegment : MonoBehaviour
{
    public ConveyorSegment Next;
    private int slotsLength = 8;
    public ConveyorItem[] Slots;
    private Vector3[] visualSlots;
    private void Start()
    {
        Reset();
    }
    private void Reset()
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
    void Update()
    {
        MoveItems();
        UpdateVisual(Slots, visualSlots);
    }
    void MoveItems()
    {
        if (Slots[slotsLength - 1] != null && Next.Slots[0] == null)
        {
            Next.Slots[0] = Slots[slotsLength - 1];
            Slots[slotsLength - 1] = null;
        }
        for (int i = slotsLength - 2; i >= 0; i--)
        {
            if (Slots[i] != null && Slots[i + 1] == null)
            {
                Slots[i + 1] = Slots[i];
                Slots[i] = null;
            }
        }
    }
    void UpdateVisual(ConveyorItem[] slots, Vector3[] positions)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (Slots[i] != null && Slots[i].View != null)
            {
                Slots[i].View.transform.position = visualSlots[i];
            }
        }
    }
}
