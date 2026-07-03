using UnityEngine;
using System.Collections.Generic;

public class BuildInteractionManager
{
    private BuildManager bm;

    public Vector2Int movingSelectionCenter;
    public Vector3Int lastDraggedCell;
    public int selectedBuildingTempRotation = 0;
    public bool deletingRectangle = false;
    public bool isPaintingBuild = false;
    public bool isPaintingCancel = false;
    public bool selectingRectangle;
    public Vector3Int anchorA;

    public BuildInteractionManager(BuildManager buildManager)
    {
        bm = buildManager;
    }

    public void HandleSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (ClipboardManager.Instance.isPasteMode)
        {
            if (!pointerOverUI) bm.HandlePasteMode();
            return;
        }
        Vector3Int currentCell = BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm));
        if (bm.inputActions.Factory.Select.WasPressedThisFrame() && !bm.inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            isPaintingBuild = true;
            lastDraggedCell = currentCell;
            selectedBuildingTempRotation = 0;
            bm.ToggleCell(currentCell, false);
        }
        else if (bm.inputActions.Factory.Select.IsPressed() && isPaintingBuild)
        {
            if (currentCell != lastDraggedCell)
            {
                if (bm.selectedBuilding != null && bm.selectedBuilding.buildingName == "Conveyor")
                {
                    Vector3Int dir = currentCell - lastDraggedCell;
                    int angle = 0;
                    if (dir.x > 0) angle = 0;
                    else if (dir.x < 0) angle = 180;
                    else if (dir.y > 0) angle = 90;
                    else if (dir.y < 0) angle = -90;
                    if (bm.plannedBuilds.TryGetValue(lastDraggedCell, out var lastBuild))
                    {
                        if (lastBuild.type.buildingName == "Conveyor")
                        {
                            int prevAngle = lastBuild.rotation;
                            if (prevAngle != angle)
                            {
                                BuildingData cornerData = bm.DetermineCorner(prevAngle, angle, out int cornerRot);
                                if (cornerData != null)
                                {
                                    lastBuild.type = cornerData;
                                    lastBuild.rotation = cornerRot;
                                    var lastGhost = BuildGhostManager.Instance.GetBuildGhost(lastDraggedCell);
                                    if (lastGhost != null)
                                    {
                                        Object.Destroy(lastGhost);
                                        BuildGhostManager.Instance.RemoveBuildGhost(lastDraggedCell);
                                        BuildGhostManager.Instance.CreateBuildGhost(lastDraggedCell, cornerData, cornerRot);
                                    }
                                }
                                else
                                {
                                    lastBuild.rotation = angle;
                                    var lastGhost = BuildGhostManager.Instance.GetBuildGhost(lastDraggedCell);
                                    if (lastGhost != null)
                                    {
                                        lastGhost.transform.rotation = Quaternion.Euler(0, 0, angle);
                                    }
                                }
                            }
                        }
                    }
                    selectedBuildingTempRotation = angle;
                    bm.PaintCell(currentCell, false);
                    selectedBuildingTempRotation = 0;
                }
                else
                {
                    bm.PaintCell(currentCell, false);
                }
                lastDraggedCell = currentCell;
            }
        }

        if (bm.inputActions.Factory.Select.WasReleasedThisFrame())
        {
            isPaintingBuild = false;
        }
        if (bm.inputActions.Factory.Select.WasPressedThisFrame() && bm.inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = currentCell;
            selectingRectangle = true;
        }
        if (bm.inputActions.Factory.Select.WasReleasedThisFrame() && selectingRectangle)
        {
            selectingRectangle = false;
            SelectRectangle(anchorA, currentCell, false);
        }
        if (bm.inputActions.Factory.Delete.WasPressedThisFrame() && !bm.inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            isPaintingCancel = true;
            lastDraggedCell = currentCell;
            ProcessCancelBrush(currentCell);
        }
        else if (bm.inputActions.Factory.Delete.IsPressed() && isPaintingCancel)
        {
            if (currentCell != lastDraggedCell)
            {
                ProcessCancelBrush(currentCell);
                lastDraggedCell = currentCell;
            }
        }

        if (bm.inputActions.Factory.Delete.WasReleasedThisFrame())
        {
            isPaintingCancel = false;
        }
        if (bm.inputActions.Factory.Delete.WasPressedThisFrame() && bm.inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = currentCell;
            deletingRectangle = true;
        }
        if (bm.inputActions.Factory.Delete.WasReleasedThisFrame() && deletingRectangle)
        {
            deletingRectangle = false;
            SelectRectangle(anchorA, currentCell, true);
        }
    }

    public void HandleEditSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (ClipboardManager.Instance.isPasteMode)
        {
            if (!pointerOverUI) bm.HandlePasteMode();
            return;
        }
        Vector3Int currentCell = BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm));
        if (bm.EditState == EditInteractionState.None)
        {
            if (bm.inputActions.Factory.Select.WasPressedThisFrame())
            {
                if (pointerOverUI) return;

                if (bm.inputActions.Factory.MultiBuild.IsPressed())
                {
                    bm.EditState = EditInteractionState.SelectingRectangle;
                    anchorA = currentCell;
                }
                else
                {
                    if (bm.selectedCells.Contains(currentCell))
                    {
                        bm.TransitionToMovingSelection();
                    }
                    else
                    {
                        bm.EditState = EditInteractionState.PaintingSelection;
                        bm.ClearAll();
                        if (GridManager.Instance.IsOccupied(currentCell))
                        {
                            bm.selectedCells.Add(currentCell);
                            var block = GridManager.Instance.GetBuilding(currentCell).GetComponent<FactoryBlock>();
                            if (block != null)
                            {
                                var bData = bm.availableBuildings.Find(b => b.buildingName == block.blockId);
                                int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                                BuildGhostManager.Instance.CreateBuildGhost(currentCell, bData, rot);
                            }
                        }
                        lastDraggedCell = currentCell;
                    }
                }
            }
            else if (bm.inputActions.Factory.Delete.WasPressedThisFrame())
            {
                if (pointerOverUI) return;
                bm.EditState = EditInteractionState.PaintingDeletion;
                lastDraggedCell = currentCell;
                ProcessCancelBrush(currentCell);
            }
        }
        else if (bm.EditState == EditInteractionState.PaintingSelection)
        {
            if (bm.inputActions.Factory.Select.IsPressed())
            {
                if (currentCell != lastDraggedCell)
                {
                    if (GridManager.Instance.IsOccupied(currentCell) && !bm.selectedCells.Contains(currentCell))
                    {
                        bm.selectedCells.Add(currentCell);
                        var block = GridManager.Instance.GetBuilding(currentCell).GetComponent<FactoryBlock>();
                        if (block != null)
                        {
                            var bData = bm.availableBuildings.Find(b => b.buildingName == block.blockId);
                            int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                            BuildGhostManager.Instance.CreateBuildGhost(currentCell, bData, rot);
                        }
                    }
                    lastDraggedCell = currentCell;
                }
            }
            if (bm.inputActions.Factory.Select.WasReleasedThisFrame())
            {
                bm.EditState = EditInteractionState.None;
            }
        }
        else if (bm.EditState == EditInteractionState.SelectingRectangle)
        {
            if (bm.inputActions.Factory.Select.WasReleasedThisFrame())
            {
                SelectEditRectangle(anchorA, currentCell);
                bm.EditState = EditInteractionState.None;
            }
        }
        else if (bm.EditState == EditInteractionState.PaintingDeletion)
        {
            if (bm.inputActions.Factory.Delete.IsPressed())
            {
                if (currentCell != lastDraggedCell)
                {
                    ProcessCancelBrush(currentCell);
                    lastDraggedCell = currentCell;
                }
            }
            if (bm.inputActions.Factory.Delete.WasReleasedThisFrame())
            {
                bm.History.ApplyChanges();
                bm.EditState = EditInteractionState.None;
            }
        }
        else if (bm.EditState == EditInteractionState.MovingSelection)
        {
            bm.UpdateMovingSelectionGhosts(currentCell);
            if (bm.inputActions.Factory.Select.WasReleasedThisFrame())
            {
                if (pointerOverUI) return;
                Vector3Int offset = new Vector3Int(currentCell.x - movingSelectionCenter.x, currentCell.y - movingSelectionCenter.y, 0);
                bool collision = false;
                List<Vector3Int> foreignCells = new List<Vector3Int>();
                foreach (var kvp in bm.plannedBuilds)
                {
                    Vector3Int targetCell = kvp.Key;
                    if (GridManager.Instance.IsOccupied(targetCell) && !bm.plannedDeletes.Contains(targetCell))
                    {
                        foreignCells.Add(targetCell);
                    }
                }
                foreach (var fCell in foreignCells)
                {
                    Vector3Int swapDest = fCell - offset;
                    if (bm.plannedBuilds.ContainsKey(swapDest))
                    {
                        collision = true;
                        break;
                    }
                }
                if (collision)
                {
                    bm.ClearAll();
                }
                else
                {
                    List<KeyValuePair<Vector3Int, PlannedBuild>> swapsToAdd = new List<KeyValuePair<Vector3Int, PlannedBuild>>();

                    foreach (var fCell in foreignCells)
                    {
                        var block = GridManager.Instance.GetBuilding(fCell).GetComponent<FactoryBlock>();
                        if (block != null)
                        {
                            var bData = bm.availableBuildings.Find(b => b.buildingName == block.blockId);
                            int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);

                            swapsToAdd.Add(new KeyValuePair<Vector3Int, PlannedBuild>(fCell - offset, new PlannedBuild(bData, rot)));
                            bm.plannedDeletes.Add(fCell);
                        }
                    }
                    foreach (var swap in swapsToAdd)
                    {
                        bm.plannedBuilds[swap.Key] = swap.Value;
                    }
                    bm.History.ApplyChanges();
                }
                bm.EditState = EditInteractionState.None;
            }
            else if (bm.inputActions.Factory.Delete.WasPressedThisFrame())
            {
                if (pointerOverUI) return;
                bm.ClearAll();
                bm.EditState = EditInteractionState.None;
            }
        }
    }

    public void HandleRotation()
    {
        bool pivotAroundCenter = !bm.inputActions.Factory.MultiBuild.IsPressed();
        if (bm.inputActions.Factory.RotateLeft.WasPressedThisFrame())
        {
            if (bm.IsEditMode && (bm.EditState == EditInteractionState.MovingSelection || ClipboardManager.Instance.isPasteMode))
            {
                ClipboardManager.Instance.RotateClipboard(90, pivotAroundCenter);
                if (bm.EditState == EditInteractionState.MovingSelection)
                    bm.UpdateMovingSelectionGhosts(BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm)));
                else
                    bm.lastPasteCenter = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            }
            else if (bm.IsEditMode)
            {
                bm.RotateSelected(90);
            }
            else if (bm.IsBuildMode)
            {
                selectedBuildingTempRotation = (selectedBuildingTempRotation + 90) % 360;
                if (isPaintingBuild)
                {
                    Vector3Int currentCell = BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm));
                    bm.PaintCell(currentCell, false);
                }
            }
        }
        if (bm.inputActions.Factory.RotateRight.WasPressedThisFrame())
        {
            if (bm.IsEditMode && (bm.EditState == EditInteractionState.MovingSelection || ClipboardManager.Instance.isPasteMode))
            {
                ClipboardManager.Instance.RotateClipboard(-90, pivotAroundCenter);
                if (bm.EditState == EditInteractionState.MovingSelection)
                    bm.UpdateMovingSelectionGhosts(BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm)));
                else
                    bm.lastPasteCenter = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            }
            else if (bm.IsEditMode)
            {
                bm.RotateSelected(-90);
            }
            else if (bm.IsBuildMode)
            {
                selectedBuildingTempRotation = (selectedBuildingTempRotation - 90 + 360) % 360;
                if (isPaintingBuild)
                {
                    Vector3Int currentCell = BuildGridUtils.WorldToCell(bm, BuildGridUtils.GetMouseWorld(bm));
                    bm.PaintCell(currentCell, false);
                }
            }
        }
    }

    void ProcessCancelBrush(Vector3Int cell)
    {
        if (bm.plannedBuilds.ContainsKey(cell))
        {
            bm.plannedBuilds.Remove(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        else if (bm.plannedRotations.ContainsKey(cell))
        {
            bm.plannedRotations.Remove(cell);
            var rGhost = BuildGhostManager.Instance.GetRotationGhost(cell);
            if (rGhost != null)
            {
                Object.Destroy(rGhost);
                BuildGhostManager.Instance.RemoveRotationGhost(cell);
            }
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

    void SelectEditRectangle(Vector3Int a, Vector3Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                bm.ToggleEditCell(new Vector3Int(x, y, 0));
            }
        }
    }

    void SelectRectangle(Vector3Int a, Vector3Int b, bool deleteMode)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                bm.ToggleCell(
                    new Vector3Int(x, y, 0),
                    deleteMode
                );
            }
        }
    }

}
