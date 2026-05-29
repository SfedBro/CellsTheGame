using UnityEngine;

public class MachineOutput: MonoBehaviour
{
    public ConveyorItemView itemPrefab;
    [SerializeField]
    private Vector3Int gridPosition;
    private IItemReceiver output;
    private void Start()
    {
        GetGridPosition();
        GetOutput();
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
            return false;

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnItem(ItemType.TestOre);
        }
    }
}
