using UnityEngine;
using UnityEngine.Rendering;

public class ConveyorSegment : FactoryBlock
{
    #region State & Configuration
    [SerializeField]
    private int slotsLength = 8;
    public ConveyorItem[] Slots;
    private Vector3[] visualSlots;
    #endregion

    #region Lifecycle & Initialization
    protected override void Start()
    {
        base.Start();
        GetVisualPositions();
    }

    public override void Initialize()
    {
        if (Ports == null) Ports = new System.Collections.Generic.List<Port>();
        if (Ports.Count == 0)
        {
            Ports.Add(new Port { CellOffset = Vector2Int.zero, Direction = PortDirection.Left, IsInput = true });
            Ports.Add(new Port { CellOffset = Vector2Int.zero, Direction = PortDirection.Right, IsOutput = true });
        }
        base.Initialize();
        Slots = new ConveyorItem[slotsLength];
        visualSlots = new Vector3[slotsLength];
        GetVisualPositions();
    }

    public override void RebuildConnections()
    {
        base.RebuildConnections();
        GetVisualPositions();
    }
    #endregion

    #region Logic & Movement
    public override void Tick()
    {
        MoveItems();
    }

    private void MoveItems()
    {
        for (int i = 0; i < slotsLength; i++)
        {
            if (Slots[i] != null)
                Slots[i].movedThisTick = false;
        }

        var lastItem = Slots[slotsLength - 1];
        if (lastItem != null)
        {
            Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
            if (outPort != null)
            {
                if (outPort.ConnectedBlock.TryReceiveItem(lastItem, outPort.ConnectedPort))
                {
                    Slots[slotsLength - 1] = null;
                }
            }
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

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (item == null || Slots[0] != null)
            return false;

        if (receivingPort != null && receivingPort.Filter.HasValue && receivingPort.Filter.Value != item.Type)
            return false;

        Slots[0] = item;
        item.movedThisTick = true;

        return true;
    }
    #endregion

    #region Visuals & Utilities
    private void Update()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        for (int i = 0; i < slotsLength; i++)
        {
            if (Slots[i] != null && Slots[i].View != null)
            {
                Slots[i].View.transform.position = visualSlots[i];
            }
        }
    }

    private void GetVisualPositions()
    {
        Vector3 dir = transform.right;
        float step = 1f / slotsLength;
        Vector3 start = transform.position - dir * 0.5f + dir * step * 0.5f;
        Vector3 end = transform.position + dir * 0.5f;
        for (int i = 0; i < slotsLength; i++)
        {
            float t = slotsLength >= 1 ? (float)i / (slotsLength - 1) : 0f;
            visualSlots[i] = Vector3.Lerp(start, end, t);
        }
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= slotsLength)
        {
            throw new System.IndexOutOfRangeException($"Invalid index on object {gameObject.name}");
        }
        return visualSlots[index];
    }

    protected override void OnDrawGizmos()
    {
        if (Ports == null) Ports = new System.Collections.Generic.List<Port>();
        if (Ports.Count == 0)
        {
            Ports.Add(new Port { CellOffset = Vector2Int.zero, Direction = PortDirection.Left, IsInput = true });
            Ports.Add(new Port { CellOffset = Vector2Int.zero, Direction = PortDirection.Right, IsOutput = true });
        }
        base.OnDrawGizmos();

        if (visualSlots == null)
            return;

        foreach (var pos in visualSlots)
        {
            Gizmos.DrawSphere(pos, 0.05f);
        }
    }
    #endregion
}
