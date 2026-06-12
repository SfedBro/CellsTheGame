using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    #region Singleton & State
    private static GridManager _instance;
    public static GridManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GridManager>();
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }
    private Dictionary<Vector3Int, MonoBehaviour> buildings = new();
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }
        _instance = this;
        buildings.Clear();
    }
    #endregion

    #region Registration
    public void RegisterBuilding(Vector3Int pos, MonoBehaviour building)
    {
        buildings[pos] = building;
    }

    public void Unregister(Vector3Int pos)
    {
        buildings.Remove(pos);
    }
    #endregion

    #region Lookups
    public bool IsOccupied(Vector3Int pos)
    {
        return buildings.ContainsKey(pos);
    }

    public MonoBehaviour GetBuilding(Vector3Int pos)
    {
        buildings.TryGetValue(pos, out var building);
        return building;
    }
    #endregion

    #region Neighbor Notification
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

        if (building is FactoryBlock factoryBlock)
        {
            factoryBlock.RebuildConnections();
        }
    }
    #endregion
}
