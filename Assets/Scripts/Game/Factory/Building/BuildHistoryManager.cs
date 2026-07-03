using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildHistoryManager
{
    private BuildManager bm;
    public Stack<EditAction> undoStack = new Stack<EditAction>();
    public Stack<EditAction> redoStack = new Stack<EditAction>();
    public bool isUndoingOrRedoing = false;

    public BuildHistoryManager(BuildManager buildManager)
    {
        bm = buildManager;
    }


    public void UndoLastAction()
    {
        if (undoStack.Count == 0) return;
        
        EditAction action = undoStack.Pop();
        redoStack.Push(action);
        
        bm.ClearAll();
        
        foreach (var kvp in action.builtCells)
        {
            bm.plannedDeletes.Add(kvp.Key);
        }
        
        foreach (var kvp in action.originalRotations)
        {
            bm.plannedRotations[kvp.Key] = kvp.Value;
        }
        
        foreach (var kvp in action.deletedBuildings)
        {
            bm.plannedBuilds[kvp.Key] = kvp.Value;
        }
        
        isUndoingOrRedoing = true;
        ApplyChanges(true);
        isUndoingOrRedoing = false;
    }

    
    public void RedoLastAction()
    {
        if (redoStack.Count == 0) return;
        
        EditAction action = redoStack.Pop();
        undoStack.Push(action);
        
        bm.ClearAll();
        
        foreach (var kvp in action.deletedBuildings)
        {
            bm.plannedDeletes.Add(kvp.Key);
        }
        
        foreach (var kvp in action.newRotations)
        {
            bm.plannedRotations[kvp.Key] = kvp.Value;
        }
        
        foreach (var kvp in action.builtCells)
        {
            bm.plannedBuilds[kvp.Key] = kvp.Value;
        }
        
        isUndoingOrRedoing = true;
        ApplyChanges(true);
        isUndoingOrRedoing = false;
    }

    
    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    public void ApplyChanges(bool isUndoRedo = false)
    {
        if (!isUndoRedo && !isUndoingOrRedoing)
        {
            EditAction action = new EditAction();
            foreach (var kvp in bm.plannedBuilds) action.builtCells[kvp.Key] = kvp.Value;
            foreach (var cell in bm.plannedDeletes)
            {
                var block = GridManager.Instance.GetBuilding(cell)?.GetComponent<FactoryBlock>();
                if (block != null)
                {
                    var bData = bm.availableBuildings.Find(b => b.buildingName == block.blockId);
                    int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                    action.deletedBuildings[cell] = new PlannedBuild(bData, rot);
                }
            }
            foreach (var kvp in bm.plannedRotations)
            {
                var block = GridManager.Instance.GetBuilding(kvp.Key)?.GetComponent<FactoryBlock>();
                if (block != null)
                {
                    action.originalRotations[kvp.Key] = Mathf.RoundToInt(block.transform.eulerAngles.z);
                }
                action.newRotations[kvp.Key] = kvp.Value;
            }
            if (action.builtCells.Count > 0 || action.deletedBuildings.Count > 0 || action.originalRotations.Count > 0)
            {
                undoStack.Push(action);
                redoStack.Clear();
            }
        }

        HashSet<Vector3Int> changed = new HashSet<Vector3Int>();
        HashSet<FactoryBlock> blocksToDestroy = new HashSet<FactoryBlock>();
        foreach (var cell in bm.plannedDeletes)
        {
            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
            if (building == null) continue;
            if (building is FactoryBlock block)
            {
                blocksToDestroy.Add(block);
            }
            else
            {
                GridManager.Instance.Unregister(cell);
                if (building != null) Object.Destroy(building.gameObject);
                changed.Add(cell);
            }
        }

        foreach (var block in blocksToDestroy)
        {
            BuildingData data = bm.availableBuildings.Find(b => b.buildingName == block.blockId);
            if (data != null && data.cost != null)
            {
                foreach (var stack in data.cost)
                {
                    ResourcesManager.instance.addResourceAmount(stack.type, stack.amount);
                }
            }
            block.OnRemoved();
            if (block != null) Object.Destroy(block.gameObject);
        }

        foreach (var cell in bm.plannedDeletes)
        {
            changed.Add(cell);
        }
        foreach (var pair in bm.plannedRotations)
        {
            MonoBehaviour building = GridManager.Instance.GetBuilding(pair.Key);
            if (building != null)
            {
                building.transform.rotation = Quaternion.Euler(0, 0, pair.Value);
                if (building is FactoryBlock block)
                {
                    block.UpdateRotation();
                }
                changed.Add(pair.Key);
            }
        }
        foreach (var pair in bm.plannedBuilds)
        {
            if (GridManager.Instance.IsOccupied(pair.Key))
                continue;
            bool canAfford = true;
            if (pair.Value.type.cost != null && pair.Value.type.cost.Count > 0)
            {
                foreach (var stack in pair.Value.type.cost)
                {
                    if (ResourcesManager.instance.getResourceAmount(stack.type) < stack.amount)
                    {
                        canAfford = false;
                        break;
                    }
                }
            }
            if (!canAfford)
            {
                Debug.Log($"Not enough resources to build {pair.Value.type.buildingName}!");
                continue;
            }
            if (pair.Value.type.cost != null)
            {
                foreach (var stack in pair.Value.type.cost)
                {
                    ResourcesManager.instance.addResourceAmount(stack.type, -stack.amount);
                }
            }
            GameObject prefab = bm.GetPrefab(pair.Value.type);
            if (prefab == null) continue;
            GameObject go = Object.Instantiate(prefab,
                BuildGridUtils.CellToWorld(bm, pair.Key),
                Quaternion.Euler(0, 0, pair.Value.rotation));

            if (go.TryGetComponent<FactoryBlock>(out var block))
            {
                block.blockId = pair.Value.type.buildingName;
                block.Initialize();
                block.OnPlaced();
                
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NotifyAction("Build_" + block.blockId);
                }
            }
            changed.Add(pair.Key);
        }
        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }
        bm.ClearAll();
        if (changed.Count > 0)
        {
            bm.Save();
        }
    }

    public void CutSelected()
    {
        foreach (var cell in bm.selectedCells)
        {
            if (bm.plannedBuilds.ContainsKey(cell))
            {
                bm.plannedBuilds.Remove(cell);
                BuildGhostManager.Instance.RemoveBuildGhost(cell);
            }
            else if (GridManager.Instance.IsOccupied(cell))
            {
                if (!bm.plannedDeletes.Contains(cell))
                {
                    bm.plannedDeletes.Add(cell);
                    BuildGhostManager.Instance.CreateDeleteMarker(cell);
                }
            }
        }

        if (bm.IsEditMode)
        {
            ApplyChanges();
        }
    }
}
