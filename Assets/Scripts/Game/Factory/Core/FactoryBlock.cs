using System.Collections.Generic;
using UnityEngine;

public abstract class FactoryBlock : MonoBehaviour
{
    #region Properties & State
    [Header("Grid")]
    public Vector2Int Size = Vector2Int.one;

    [Header("Connections")]
    public List<Port> Ports = new List<Port>();

    protected Vector3Int gridPosition;
    public Vector3Int GridPosition => gridPosition;
    #endregion

    #region Lifecycle & Unity Events
    protected virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        ApplyRotationToPorts();
        MergeDuplicatePorts();
        GetGridPosition();
        RegisterInGrid();
        if (FactoryTickManager.Instance != null)
        {
            FactoryTickManager.Instance.RegisterBlock(this);
        }
    }

    public virtual void OnPlaced()
    {
    }

    public virtual void OnRemoved()
    {
        UnregisterFromGrid();
        if (FactoryTickManager.Instance != null)
        {
            FactoryTickManager.Instance.UnregisterBlock(this);
        }
    }

    public virtual void Tick()
    {
        // To be overridden by mechanisms that need tick logic (like Conveyors, Presses)
    }
    #endregion

    #region Grid Registration
    protected virtual void GetGridPosition()
    {
        Grid grid = FindFirstObjectByType<Grid>();
        gridPosition = grid.WorldToCell(transform.position);
    }

    protected virtual void RegisterInGrid()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                Vector3Int pos = gridPosition + new Vector3Int(x, y, 0);
                GridManager.Instance.RegisterBuilding(pos, this);
            }
        }
    }

    protected virtual void UnregisterFromGrid()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                Vector3Int pos = gridPosition + new Vector3Int(x, y, 0);
                GridManager.Instance.Unregister(pos);
            }
        }
    }

    #endregion

    #region Port Routing
        [ContextMenu("Merge Duplicate Ports")]
    public void MergeDuplicatePorts()
    {
        if (Ports == null || Ports.Count == 0) return;

        List<Port> mergedPorts = new List<Port>();
        foreach (var p in Ports)
        {
            var existing = mergedPorts.Find(x => x.CellOffset == p.CellOffset && x.Direction == p.Direction);
            if (existing != null)
            {
                existing.IsInput = existing.IsInput || p.IsInput;
                existing.IsOutput = existing.IsOutput || p.IsOutput;
            }
            else
            {
                mergedPorts.Add(p);
            }
        }
        Ports = mergedPorts;
    }

    public virtual void RebuildConnections()
    {
        foreach (var port in Ports)
        {
            port.ClearConnection();
            TryConnectPort(port);
        }
    }

    private void TryConnectPort(Port port)
    {
        Vector3Int targetPos = port.GetTargetGlobalPosition(gridPosition);
        if (GridManager.Instance.GetBuilding(targetPos) is not FactoryBlock neighborBlock)
            return;

        PortDirection expectedDirection = port.GetOppositeDirection();
        
        foreach (var neighborPort in neighborBlock.Ports)
        {
            if (CanPortsConnect(port, targetPos, neighborPort, neighborBlock, expectedDirection))
            {
                port.ConnectedBlock = neighborBlock;
                port.ConnectedPort = neighborPort;
                return; // Early return once connected
            }
        }
    }

    private bool CanPortsConnect(Port myPort, Vector3Int myTargetPos, Port neighborPort, FactoryBlock neighborBlock, PortDirection expectedDirection)
    {
        if (neighborPort.Direction != expectedDirection) return false;
        
        Vector3Int neighborPortPos = neighborPort.GetGlobalPosition(neighborBlock.gridPosition);
        if (neighborPortPos != myTargetPos) return false;

        return (myPort.IsOutput && neighborPort.IsInput) || (myPort.IsInput && neighborPort.IsOutput);
    }

    public virtual bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        return false;
    }

    public virtual Dictionary<ItemType, int> GetCost()
    {
        return new();
    }
    #endregion

    #region Editor / Debug
#if UNITY_EDITOR
    public static bool ShowDebugPorts = true;

    [UnityEditor.MenuItem("Factory/Toggle Port Debug")]
    public static void TogglePortDebug()
    {
        ShowDebugPorts = !ShowDebugPorts;
        UnityEditor.SceneView.RepaintAll();
    }

    private int currentPortRotationSteps = 0;

    public void UpdateRotation()
    {
        ApplyRotationToPorts();
        RebuildConnections();
    }

    private void ApplyRotationToPorts()
    {
        float zRot = transform.eulerAngles.z;
        int targetSteps = Mathf.RoundToInt(zRot / 90f) % 4;
        if (targetSteps < 0) targetSteps += 4;

        int stepsToRotate = (targetSteps - currentPortRotationSteps) % 4;
        if (stepsToRotate < 0) stepsToRotate += 4;

        if (stepsToRotate == 0) return;

        foreach (var port in Ports)
        {
            for (int i = 0; i < stepsToRotate; i++)
            {
                int oldX = port.CellOffset.x;
                int oldY = port.CellOffset.y;
                port.CellOffset.x = -oldY;
                port.CellOffset.y = oldX;

                switch (port.Direction)
                {
                    case PortDirection.Right: port.Direction = PortDirection.Up; break;
                    case PortDirection.Up: port.Direction = PortDirection.Left; break;
                    case PortDirection.Left: port.Direction = PortDirection.Down; break;
                    case PortDirection.Down: port.Direction = PortDirection.Right; break;
                }
            }
        }
        currentPortRotationSteps = targetSteps;
    }

    protected virtual void OnDrawGizmos()
    {
        if (!ShowDebugPorts) return;

        // Draw mechanism footprint
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Vector3 origin = transform.position;
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                Gizmos.DrawCube(origin + new Vector3(x, y, 0), new Vector3(0.9f, 0.9f, 0.1f));
            }
        }

        // Draw Ports
        if (Ports != null)
        {
            List<Port> drawPorts = new List<Port>();
            foreach (var p in Ports)
            {
                var existing = drawPorts.Find(x => x.CellOffset == p.CellOffset && x.Direction == p.Direction);
                if (existing != null)
                {
                    existing.IsInput = existing.IsInput || p.IsInput;
                    existing.IsOutput = existing.IsOutput || p.IsOutput;
                }
                else
                {
                    drawPorts.Add(new Port { CellOffset = p.CellOffset, Direction = p.Direction, IsInput = p.IsInput, IsOutput = p.IsOutput });
                }
            }

            foreach (var port in drawPorts)
            {
                Vector3 portCenter = origin + new Vector3(port.CellOffset.x, port.CellOffset.y, 0);
                
                // Color based on type: Output = Green, Input = Red
                if (port.IsInput && port.IsOutput)
                    Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow
                else if (port.IsOutput)
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Green
                else if (port.IsInput)
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red
                else
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray

                Vector3 dirVector = Vector3.zero;
                switch (port.Direction)
                {
                    case PortDirection.Up: dirVector = Vector3.up; break;
                    case PortDirection.Down: dirVector = Vector3.down; break;
                    case PortDirection.Left: dirVector = Vector3.left; break;
                    case PortDirection.Right: dirVector = Vector3.right; break;
                }

                Vector3 edgePos = portCenter + dirVector * 0.35f;

                // Draw port indicator as a cube on the edge
                Gizmos.DrawCube(edgePos, new Vector3(0.15f, 0.15f, 0.15f));

                // Draw direction line (shortened)
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Cyan semi-transparent
                Gizmos.DrawLine(edgePos, edgePos + dirVector * 0.15f);
                Gizmos.DrawSphere(edgePos + dirVector * 0.15f, 0.04f); // arrowhead
            }
        }
    }
#endif
    #endregion
}
