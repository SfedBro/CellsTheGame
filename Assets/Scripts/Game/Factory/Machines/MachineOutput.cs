using UnityEngine;

public class MachineOutput : MonoBehaviour, IItemGiver
{
    public ConveyorItemView itemPrefab;
    [SerializeField]
    private Vector3Int gridPosition;
    private IItemReceiver output;
    public bool shouldSpawnManually = false;
    private void Start()
    {
        GetGridPosition();
        RebuildConnections();
        GridManager.Instance.RegisterBuilding(gridPosition, this);
    }
    public void RebuildConnections()
    {
        Invoke(nameof(GetOutput), 0.01f);
    }
    private void GetGridPosition()
    {
        Grid grid = FindFirstObjectByType<Grid>();
        gridPosition = grid.WorldToCell(transform.position);
    }
    private void GetOutput()
    {
        Vector3Int nextPos = gridPosition + GetDirection();
        //Debug.Log($"gridPosition = {gridPosition}");
        //Debug.Log($"transform.right = {transform.right}");
        //Debug.Log($"direction = {GetDirection()}");
        output = GridManager.Instance.GetReceiver(nextPos);
    }
    private Vector3Int GetDirection()
    {
        Vector3 dir = transform.right;

        return new Vector3Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y), 0);
    }
    public bool SpawnItem(ItemType type)
    {
        if (output == null)
        {
            Debug.Log("Output IS NULL");
            return false;
        }

        ConveyorItem item = new ConveyorItem();

        item.Type = type;
        item.View = Instantiate(itemPrefab, transform.position, Quaternion.identity);

        if (!output.TryReceiveItem(item))
        {
            Destroy(item.View.gameObject);
            return false;
        }
        return true;
    }
    private void Update()
    {
        if (shouldSpawnManually && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");

            SpawnItem(ItemType.TestOre);
        }
    }
}
