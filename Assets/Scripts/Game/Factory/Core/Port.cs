using System;
using UnityEngine;

public enum PortDirection
{
    Up,
    Down,
    Left,
    Right
}

[Serializable]
public class Port
{
    public Vector2Int CellOffset = Vector2Int.zero;
    public PortDirection Direction = PortDirection.Right;
    public bool IsInput = false;
    public bool IsOutput = false;
    public ItemType? Filter = null;

    [HideInInspector]
    public FactoryBlock ConnectedBlock;
    [HideInInspector]
    public Port ConnectedPort;

    public void ClearConnection()
    {
        ConnectedBlock = null;
        ConnectedPort = null;
    }

    public Vector3Int GetGlobalPosition(Vector3Int blockGridPosition)
    {
        return blockGridPosition + new Vector3Int(CellOffset.x, CellOffset.y, 0);
    }

    public Vector3Int GetTargetGlobalPosition(Vector3Int blockGridPosition)
    {
        Vector3Int pos = GetGlobalPosition(blockGridPosition);
        switch (Direction)
        {
            case PortDirection.Up: return pos + Vector3Int.up;
            case PortDirection.Down: return pos + Vector3Int.down;
            case PortDirection.Left: return pos + Vector3Int.left;
            case PortDirection.Right: return pos + Vector3Int.right;
        }
        return pos;
    }

    public PortDirection GetOppositeDirection()
    {
        switch (Direction)
        {
            case PortDirection.Up: return PortDirection.Down;
            case PortDirection.Down: return PortDirection.Up;
            case PortDirection.Left: return PortDirection.Right;
            case PortDirection.Right: return PortDirection.Left;
        }
        return PortDirection.Up;
    }
}
