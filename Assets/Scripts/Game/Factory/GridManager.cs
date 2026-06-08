using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    private Dictionary<Vector3Int, IItemReceiver> receivers = new();
    private Dictionary<Vector3Int, MonoBehaviour> buildings = new();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterReceiver(Vector3Int pos, IItemReceiver receiver)
    {
        receivers[pos] = receiver;
    }

    public void RegisterBuilding(Vector3Int pos, MonoBehaviour building)
    {
        buildings[pos] = building;
    }

    public void Unregister(Vector3Int pos)
    {
        receivers.Remove(pos);
        buildings.Remove(pos);
    }

    public bool IsOccupied(Vector3Int pos)
    {
        return buildings.ContainsKey(pos);
    }

    public IItemReceiver GetReceiver(Vector3Int pos)
    {
        receivers.TryGetValue(pos, out var receiver);
        return receiver;
    }

    public MonoBehaviour GetBuilding(Vector3Int pos)
    {
        buildings.TryGetValue(pos, out var building);
        return building;
    }

    public void NotifyNeighbours(Vector3Int pos)
    {
        TryRebuild(pos);

        TryRebuild(pos + Vector3Int.right);
        TryRebuild(pos + Vector3Int.left);
        TryRebuild(pos + Vector3Int.up);
        TryRebuild(pos + Vector3Int.down);
    }

    private void TryRebuild(Vector3Int pos)
    {
        if (!buildings.TryGetValue(pos, out var building))
            return;

        if (building is IItemGiver buildable)
        {
            buildable.RebuildConnections();
        }
    }
}