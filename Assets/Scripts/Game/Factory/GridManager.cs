using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    private Dictionary<Vector3Int, IItemReceiver> receivers = new();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterReceiver(Vector3Int pos, IItemReceiver receiver)
    {
        receivers[pos] = receiver;
        Debug.Log($"Registered {receiver} at {pos}");
    }

    public IItemReceiver GetReceiver(Vector3Int pos)
    {
        receivers.TryGetValue(pos, out var receiver);

        //Debug.Log($"Lookup {pos} -> {receiver}");

        return receiver;
    }
    public void NotifyNeighbours(Vector3Int pos)
    {
        TryRebuild(pos);
        TryRebuild(pos + Vector3Int.right);
        TryRebuild(pos + Vector3Int.left);
        TryRebuild(pos + Vector3Int.up);
        TryRebuild(pos + Vector3Int.down);
    }

    void TryRebuild(Vector3Int pos)
    {
        if (receivers.TryGetValue(pos, out var r))
        {
            if (r is IBuildable interactable)
                interactable.RebuildConnections();
        }
    }
}
