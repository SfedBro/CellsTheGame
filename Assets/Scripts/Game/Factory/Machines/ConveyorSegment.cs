using UnityEngine;
using UnityEngine.Rendering;

public class ConveyorSegment : FactoryBlock
{
    #region Variables
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

    private void OnDestroy()
    {
        if (Slots != null)
        {
            foreach (var item in Slots)
            {
                if (item != null && item.View != null)
                {
                    Destroy(item.View.gameObject);
                }
            }
        }
    }

    public override void Initialize()
    {
        if (isInitialized) return;
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

    public override string GetSaveState()
    {
        if (Slots == null) return "";
        var data = new ConveyorSaveData { SlotItems = new int[slotsLength] };
        for (int i = 0; i < slotsLength; i++)
        {
            data.SlotItems[i] = Slots[i] != null ? (int)Slots[i].Type : -1;
        }
        return JsonUtility.ToJson(data);
    }

    public override void LoadSaveState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson)) return;
        var data = JsonUtility.FromJson<ConveyorSaveData>(stateJson);
        if (data != null && data.SlotItems != null)
        {
            if (Slots == null) Slots = new ConveyorItem[slotsLength];
            for (int i = 0; i < Mathf.Min(slotsLength, data.SlotItems.Length); i++)
            {
                if (data.SlotItems[i] != -1 && data.SlotItems[i] != (int)ItemType.Default)
                {
                    ItemType t = (ItemType)data.SlotItems[i];
                    ConveyorItem item = new ConveyorItem { Type = t };
                    GameObject go = new GameObject("ConveyorItem");
                    go.transform.SetParent(this.transform);
                    float scale = ResourcesManager.instance != null ? ResourcesManager.instance.getResourceScale(t) : 0.5f;
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                    item.View = go.AddComponent<ConveyorItemView>();
                    var renderer = go.AddComponent<SpriteRenderer>();
                    if (ResourcesManager.instance != null)
                        renderer.sprite = ResourcesManager.instance.getResourceSprite(t);
                    renderer.sortingOrder = 32767;
                    
                    Slots[i] = item;
                }
            }
            GetVisualPositions();
            UpdateVisual();
        }
    }
    #endregion

    #region Logic & Movement
    public override void PreTick()
    {
        MoveInternalItems();
    }

    public override void Tick()
    {
        PushItemsToNext();
    }

    private void MoveInternalItems()
    {
        for (int i = 0; i < slotsLength; i++)
        {
            if (Slots[i] != null)
                Slots[i].movedThisTick = false;
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

    private void PushItemsToNext()
    {
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
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (item == null || Slots[0] != null || receivingPort == null || !receivingPort.IsInput || (receivingPort.Filter.HasValue && receivingPort.Filter.Value != item.Type))
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
        Port inPort = Ports.Find(p => p.IsInput);
        Port outPort = Ports.Find(p => p.IsOutput);

        if (inPort != null && outPort != null)
        {
            Vector3 inDir = GetDirectionVector(inPort.Direction);
            Vector3 outDir = GetDirectionVector(outPort.Direction);
            
            float step = 1f / slotsLength;
            
            // start - откуда приходят предметы (от границы входа)
            Vector3 start = transform.position + inDir * 0.5f - inDir * step * 0.5f;
            // end - куда уходят (граница выхода)
            Vector3 end = transform.position + outDir * 0.5f - outDir * step * 0.5f;
            Vector3 center = transform.position;

            // Если направления противоположны (например, Left и Right), то это прямая
            bool isStraight = Vector3.Dot(inDir, outDir) < -0.9f;

            for (int i = 0; i < slotsLength; i++)
            {
                float t = slotsLength > 1 ? (float)i / (slotsLength - 1) : 0f;
                if (isStraight)
                {
                    visualSlots[i] = Vector3.Lerp(start, end, t);
                }
                else
                {
                    // Поворот 90 градусов (строгий угол)
                    if (t <= 0.5f)
                        visualSlots[i] = Vector3.Lerp(start, center, t * 2f);
                    else
                        visualSlots[i] = Vector3.Lerp(center, end, (t - 0.5f) * 2f);
                }
            }
        }
        else
        {
            Vector3 dir = transform.right;
            float step = 1f / slotsLength;
            Vector3 start = transform.position - dir * 0.5f + dir * step * 0.5f;
            Vector3 end = transform.position + dir * 0.5f - dir * step * 0.5f;
            for (int i = 0; i < slotsLength; i++)
            {
                float t = slotsLength > 1 ? (float)i / (slotsLength - 1) : 0f;
                visualSlots[i] = Vector3.Lerp(start, end, t);
            }
        }
    }

    private Vector3 GetDirectionVector(PortDirection dir)
    {
        switch (dir)
        {
            case PortDirection.Up: return Vector3.up;
            case PortDirection.Down: return Vector3.down;
            case PortDirection.Left: return Vector3.left;
            case PortDirection.Right: return Vector3.right;
        }
        return transform.right; // fallback, though it shouldn't be reached
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= slotsLength)
        {
            throw new System.IndexOutOfRangeException($"Invalid index on object {gameObject.name}");
        }
        return visualSlots[index];
    }

#if UNITY_EDITOR
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
#endif
    #endregion
}

[System.Serializable]
public class ConveyorSaveData
{
    public int[] SlotItems;
}
