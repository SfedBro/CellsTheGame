using UnityEngine;
using UnityEngine.Rendering;

public class ConveyorSegment : MonoBehaviour, IItemReceiver
{
    [SerializeField]
    private Vector3Int gridPosition;
    public IItemReceiver Next;
    [SerializeField]
    private int slotsLength = 8;
    public ConveyorItem[] Slots;
    private Vector3[] visualSlots;
    private void Awake()
    {
        //Initialize();
    }
    private void Start()
    {
        Initialize();
        Invoke(nameof(GetNext), 0.1f);
    }
    private void Initialize()
    {
        GetGridPosition();

        if (GridManager.Instance == null)
        {
            Debug.LogError($"{name}: GridManager is null");
            return;
        }

        GridManager.Instance.RegisterReceiver(gridPosition, this);

        Slots = new ConveyorItem[slotsLength];
        visualSlots = new Vector3[slotsLength];
        GetVisualPositions();
    }
    private void GetGridPosition()
    {
        Grid grid = FindFirstObjectByType<Grid>();
        gridPosition = grid.WorldToCell(transform.position);
    }
    private void GetNext()
    {
        Vector3Int nextPos = gridPosition + GetDirection();
        Next = GridManager.Instance.GetReceiver(nextPos);

        //Debug.Log($"{name} -> {Next}");
    }
    private Vector3Int GetDirection()
    {
        Vector3 dir = transform.right;

        return new Vector3Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y), 0);
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
        if (lastItem != null && Next != null && Next.TryReceiveItem(lastItem))
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
    private void OnDrawGizmos()
    {
        if (visualSlots == null)
            return;

        foreach (var pos in visualSlots)
        {
            Gizmos.DrawSphere(pos, 0.05f);
        }
    }
    public bool TryReceiveItem(ConveyorItem item)
    {
        if (item == null)
        {
            Debug.Log("Received NULL item");
            return false;
        }

        //Debug.Log($"{name} received {item.Type}");

        if (item == null || Slots[0] != null)
            return false;

        Slots[0] = item;
        item.movedThisTick = true;

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
