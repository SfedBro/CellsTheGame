using System.Collections.Generic;
using UnityEngine;
public class BuildManager : MonoBehaviour, IGameService
{
    #region Variables
    public Camera cam;
    [System.Serializable]
    public class PlannedBuild
    {
        public BuildingData type;
        public int rotation;
        public PlannedBuild(BuildingData type, int rotation)
        {
            this.type = type;
            this.rotation = rotation;
        }
    }
    [SerializeField]
    private bool buildMode = false;
    public bool IsBuildMode => buildMode;

    public Stack<EditAction> undoStack = new Stack<EditAction>();
    public Stack<EditAction> redoStack = new Stack<EditAction>();
    public bool isUndoingOrRedoing = false;
    
    [SerializeField]
    private bool editMode = false;

    public bool IsEditMode => editMode;
    private Grid grid;
    public BuildingData selectedBuilding;
    private HashSet<Vector3Int> selectedCells = new();
    public Dictionary<Vector3Int, PlannedBuild> plannedBuilds = new();
    public HashSet<Vector3Int> plannedDeletes = new();
    public Dictionary<Vector3Int, int> plannedRotations = new Dictionary<Vector3Int, int>();
    private Vector3Int lastPasteCenter;
    private Dictionary<Vector3Int, GameObject> pasteGhosts = new Dictionary<Vector3Int, GameObject>();
    private bool selectingRectangle;
    private Vector3Int anchorA;
    #endregion
    public void InitializeService()
    {
    }
    public void StartService()
    {
        grid = FindFirstObjectByType<Grid>();
        if (cam == null)
            cam = Camera.main;

        Load();
    }
    public static BuildManager Instance { get; private set; }
    private InputSystem_Actions inputActions;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        inputActions = new InputSystem_Actions();
    }
    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    private float autoSaveTimer = 0f;
    private const float AUTO_SAVE_INTERVAL = 10f;
    private void Update()
    {
        if (inputActions.Factory.Undo.WasPressedThisFrame()) UndoLastAction();
        if (inputActions.Factory.Redo.WasPressedThisFrame()) RedoLastAction();
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            autoSaveTimer = 0f;
            Save();
        }
        if (FactoryStateManager.Instance == null) return;

        buildMode = FactoryStateManager.Instance.CurrentState == FactoryStateManager.FactoryState.BuildMode;
        editMode = FactoryStateManager.Instance.CurrentState == FactoryStateManager.FactoryState.EditMode;
        if (!buildMode && !editMode)
            return;
        
        if (buildMode)
            HandleSelection();
        else if (editMode)
            HandleEditSelection();
        HandleRotation();
        if (inputActions.Factory.Apply.WasPressedThisFrame() && buildMode)
            ApplyChanges();
        bool ctrl = UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed;
        if (ctrl && UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (editState != EditInteractionState.MovingSelection)
                ClipboardManager.Instance.Copy(selectedCells);
        }
        if (ctrl && UnityEngine.InputSystem.Keyboard.current.xKey.wasPressedThisFrame)
        {
            if (editState != EditInteractionState.MovingSelection)
            {
                ClipboardManager.Instance.Cut(selectedCells);
                CutSelected();
                editState = EditInteractionState.None;
            }
        }
        if (ctrl && UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (ClipboardManager.Instance.GetClipboard().Count > 0)
            {
                ClipboardManager.Instance.isPasteMode = !ClipboardManager.Instance.isPasteMode;
                if (!ClipboardManager.Instance.isPasteMode) BuildGhostManager.Instance.ClearPasteGhosts();
            }
        }
    }
    #region Main Functions
    [Header("Available Buildings")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();
    public void OnBuildUIButtonClicked()
    {
        if (FactoryStateManager.Instance != null) FactoryStateManager.Instance.OnBuildUIButtonClicked();
    }
    public void OnEditUIButtonClicked()
    {
        if (FactoryStateManager.Instance != null) FactoryStateManager.Instance.OnEditUIButtonClicked();
    }

    public enum EditInteractionState
    {
        None,
        PaintingSelection,
        PaintingDeletion,
        MovingSelection,
        SelectingRectangle
    }
    private EditInteractionState editState = EditInteractionState.None;
    private Vector2Int movingSelectionCenter;
    private Vector3Int lastDraggedCell;
    private int selectedBuildingTempRotation = 0;
    private bool deletingRectangle = false;
    private bool isPaintingBuild = false;
    private bool isPaintingCancel = false;
    void HandleSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (ClipboardManager.Instance.isPasteMode)
        {
            if (!pointerOverUI) HandlePasteMode();
            return;
        }
        Vector3Int currentCell = WorldToCell(GetMouseWorld());
        if (inputActions.Factory.Select.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            isPaintingBuild = true;
            lastDraggedCell = currentCell;
            selectedBuildingTempRotation = 0;
            ToggleCell(currentCell, false);
        }
        else if (inputActions.Factory.Select.IsPressed() && isPaintingBuild)
        {
            if (currentCell != lastDraggedCell)
            {
                if (selectedBuilding != null && selectedBuilding.buildingName == "Conveyor")
                {
                    Vector3Int dir = currentCell - lastDraggedCell;
                    int angle = 0;
                    if (dir.x > 0) angle = 0;
                    else if (dir.x < 0) angle = 180;
                    else if (dir.y > 0) angle = 90;
                    else if (dir.y < 0) angle = -90;
                    if (plannedBuilds.TryGetValue(lastDraggedCell, out var lastBuild))
                    {
                        if (lastBuild.type.buildingName == "Conveyor")
                        {
                            int prevAngle = lastBuild.rotation;
                            if (prevAngle != angle)
                            {
                                BuildingData cornerData = DetermineCorner(prevAngle, angle, out int cornerRot);
                                if (cornerData != null)
                                {
                                    lastBuild.type = cornerData;
                                    lastBuild.rotation = cornerRot;
                                    var lastGhost = BuildGhostManager.Instance.GetBuildGhost(lastDraggedCell);
                                    if (lastGhost != null)
                                    {
                                        Destroy(lastGhost);
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
                    PaintCell(currentCell, false);
                    selectedBuildingTempRotation = 0;
                }
                else
                {
                    PaintCell(currentCell, false);
                }
                lastDraggedCell = currentCell;
            }
        }

        if (inputActions.Factory.Select.WasReleasedThisFrame())
        {
            isPaintingBuild = false;
        }
        if (inputActions.Factory.Select.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = currentCell;
            selectingRectangle = true;
        }
        if (inputActions.Factory.Select.WasReleasedThisFrame() && selectingRectangle)
        {
            selectingRectangle = false;
            SelectRectangle(anchorA, currentCell, false);
        }
        if (inputActions.Factory.Delete.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            isPaintingCancel = true;
            lastDraggedCell = currentCell;
            ProcessCancelBrush(currentCell);
        }
        else if (inputActions.Factory.Delete.IsPressed() && isPaintingCancel)
        {
            if (currentCell != lastDraggedCell)
            {
                ProcessCancelBrush(currentCell);
                lastDraggedCell = currentCell;
            }
        }

        if (inputActions.Factory.Delete.WasReleasedThisFrame())
        {
            isPaintingCancel = false;
        }
        if (inputActions.Factory.Delete.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = currentCell;
            deletingRectangle = true;
        }
        if (inputActions.Factory.Delete.WasReleasedThisFrame() && deletingRectangle)
        {
            deletingRectangle = false;
            SelectRectangle(anchorA, currentCell, true);
        }
    }
    void ProcessCancelBrush(Vector3Int cell)
    {
        if (plannedBuilds.ContainsKey(cell))
        {
            plannedBuilds.Remove(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        else if (plannedRotations.ContainsKey(cell))
        {
            plannedRotations.Remove(cell);
            var rGhost = BuildGhostManager.Instance.GetRotationGhost(cell);
            if (rGhost != null)
            {
                Destroy(rGhost);
                BuildGhostManager.Instance.RemoveRotationGhost(cell);
            }
        }
        else if (GridManager.Instance.IsOccupied(cell))
        {
            if (!plannedDeletes.Contains(cell))
            {
                plannedDeletes.Add(cell);
                BuildGhostManager.Instance.CreateDeleteMarker(cell);
            }
        }
    }
    void HandleEditSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (ClipboardManager.Instance.isPasteMode)
        {
            if (!pointerOverUI) HandlePasteMode();
            return;
        }
        Vector3Int currentCell = WorldToCell(GetMouseWorld());
        if (editState == EditInteractionState.None)
        {
            if (inputActions.Factory.Select.WasPressedThisFrame())
            {
                if (pointerOverUI) return;

                if (inputActions.Factory.MultiBuild.IsPressed())
                {
                    editState = EditInteractionState.SelectingRectangle;
                    anchorA = currentCell;
                }
                else
                {
                    if (selectedCells.Contains(currentCell))
                    {
                        TransitionToMovingSelection();
                    }
                    else
                    {
                        editState = EditInteractionState.PaintingSelection;
                        ClearAll();
                        if (GridManager.Instance.IsOccupied(currentCell))
                        {
                            selectedCells.Add(currentCell);
                            var block = GridManager.Instance.GetBuilding(currentCell).GetComponent<FactoryBlock>();
                            if (block != null)
                            {
                                var bData = availableBuildings.Find(b => b.buildingName == block.blockId);
                                int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                                BuildGhostManager.Instance.CreateBuildGhost(currentCell, bData, rot);
                            }
                        }
                        lastDraggedCell = currentCell;
                    }
                }
            }
            else if (inputActions.Factory.Delete.WasPressedThisFrame())
            {
                if (pointerOverUI) return;
                editState = EditInteractionState.PaintingDeletion;
                lastDraggedCell = currentCell;
                ProcessCancelBrush(currentCell);
            }
        }
        else if (editState == EditInteractionState.PaintingSelection)
        {
            if (inputActions.Factory.Select.IsPressed())
            {
                if (currentCell != lastDraggedCell)
                {
                    if (GridManager.Instance.IsOccupied(currentCell) && !selectedCells.Contains(currentCell))
                    {
                        selectedCells.Add(currentCell);
                        var block = GridManager.Instance.GetBuilding(currentCell).GetComponent<FactoryBlock>();
                        if (block != null)
                        {
                            var bData = availableBuildings.Find(b => b.buildingName == block.blockId);
                            int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                            BuildGhostManager.Instance.CreateBuildGhost(currentCell, bData, rot);
                        }
                    }
                    lastDraggedCell = currentCell;
                }
            }
            if (inputActions.Factory.Select.WasReleasedThisFrame())
            {
                editState = EditInteractionState.None;
            }
        }
        else if (editState == EditInteractionState.SelectingRectangle)
        {
            if (inputActions.Factory.Select.WasReleasedThisFrame())
            {
                SelectEditRectangle(anchorA, currentCell);
                editState = EditInteractionState.None;
            }
        }
        else if (editState == EditInteractionState.PaintingDeletion)
        {
            if (inputActions.Factory.Delete.IsPressed())
            {
                if (currentCell != lastDraggedCell)
                {
                    ProcessCancelBrush(currentCell);
                    lastDraggedCell = currentCell;
                }
            }
            if (inputActions.Factory.Delete.WasReleasedThisFrame())
            {
                ApplyChanges();
                editState = EditInteractionState.None;
            }
        }
        else if (editState == EditInteractionState.MovingSelection)
        {
            UpdateMovingSelectionGhosts(currentCell);
            if (inputActions.Factory.Select.WasReleasedThisFrame())
            {
                if (pointerOverUI) return;
                Vector3Int offset = new Vector3Int(currentCell.x - movingSelectionCenter.x, currentCell.y - movingSelectionCenter.y, 0);
                bool collision = false;
                List<Vector3Int> foreignCells = new List<Vector3Int>();
                foreach (var kvp in plannedBuilds)
                {
                    Vector3Int targetCell = kvp.Key;
                    if (GridManager.Instance.IsOccupied(targetCell) && !plannedDeletes.Contains(targetCell))
                    {
                        foreignCells.Add(targetCell);
                    }
                }
                foreach (var fCell in foreignCells)
                {
                    Vector3Int swapDest = fCell - offset;
                    if (plannedBuilds.ContainsKey(swapDest))
                    {
                        collision = true;
                        break;
                    }
                }
                if (collision)
                {
                    ClearAll();
                }
                else
                {
                    List<KeyValuePair<Vector3Int, PlannedBuild>> swapsToAdd = new List<KeyValuePair<Vector3Int, PlannedBuild>>();

                    foreach (var fCell in foreignCells)
                    {
                        var block = GridManager.Instance.GetBuilding(fCell).GetComponent<FactoryBlock>();
                        if (block != null)
                        {
                            var bData = availableBuildings.Find(b => b.buildingName == block.blockId);
                            int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);

                            swapsToAdd.Add(new KeyValuePair<Vector3Int, PlannedBuild>(fCell - offset, new PlannedBuild(bData, rot)));
                            plannedDeletes.Add(fCell);
                        }
                    }
                    foreach (var swap in swapsToAdd)
                    {
                        plannedBuilds[swap.Key] = swap.Value;
                    }
                    ApplyChanges();
                }
                editState = EditInteractionState.None;
            }
            else if (inputActions.Factory.Delete.WasPressedThisFrame())
            {
                if (pointerOverUI) return;
                ClearAll();
                editState = EditInteractionState.None;
            }
        }
    }
    void TransitionToMovingSelection()
    {
        Vector3Int center = ClipboardManager.Instance.GetCenterOfSelection(selectedCells);
        movingSelectionCenter = new Vector2Int(center.x, center.y);
        ClipboardManager.Instance.Copy(selectedCells);

        foreach (var cell in selectedCells)
        {
            plannedDeletes.Add(cell);
            BuildGhostManager.Instance.CreateDeleteMarker(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        editState = EditInteractionState.MovingSelection;
        UpdateMovingSelectionGhosts(WorldToCell(GetMouseWorld()));
    }
    void UpdateMovingSelectionGhosts(Vector3Int mouseCell)
    {
        foreach (var cell in new List<Vector3Int>(plannedBuilds.Keys))
        {
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        plannedBuilds.Clear();
        Vector3Int offset = new Vector3Int(mouseCell.x - movingSelectionCenter.x, mouseCell.y - movingSelectionCenter.y, 0);
        foreach (var item in ClipboardManager.Instance.GetClipboard())
        {
            Vector3Int targetCell = mouseCell + item.offset;

            if (!plannedBuilds.ContainsKey(targetCell))
            {
                plannedBuilds[targetCell] = new PlannedBuild(item.type, item.rotation);
                BuildGhostManager.Instance.CreateBuildGhost(targetCell, item.type, item.rotation);
            }
        }
    }
    void ToggleEditCell(Vector3Int cell)
    {
        if (selectedCells.Contains(cell))
        {
            selectedCells.Remove(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
            return;
        }
        if (GridManager.Instance.IsOccupied(cell))
        {
            selectedCells.Add(cell);
            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
            if (building != null && !BuildGhostManager.Instance.HasBuildGhost(cell))
            {
                GameObject ghost = new GameObject($"EditSelection_{building.name}");
                ghost.transform.position = building.transform.position;
                ghost.transform.rotation = building.transform.rotation;
                BuildGhostManager.Instance.CopySprites(building.gameObject, ghost);
                SpriteRenderer[] srs = ghost.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in srs)
                {
                    sr.color = new Color(0.5f, 0.8f, 1f, 0.7f);
                    sr.sortingOrder += 10;
                }
                BuildGhostManager.Instance.AddBuildGhost(cell, ghost);
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
                ToggleEditCell(new Vector3Int(x, y, 0));
            }
        }
    }
    private BuildingData DetermineCorner(int prevAngle, int newAngle, out int cornerRot)
    {
        cornerRot = 0;
        prevAngle = ((prevAngle % 360) + 360) % 360;
        newAngle = ((newAngle % 360) + 360) % 360;
        BuildingData leftCorner = availableBuildings.Find(b => b.buildingName == "ConveyorCornerLeft");
        BuildingData rightCorner = availableBuildings.Find(b => b.buildingName == "ConveyorCornerRight");
        if (prevAngle == 0 && newAngle == 90) { cornerRot = 0; return leftCorner; }
        if (prevAngle == 90 && newAngle == 180) { cornerRot = 90; return leftCorner; }
        if (prevAngle == 180 && newAngle == 270) { cornerRot = 180; return leftCorner; }
        if (prevAngle == 270 && newAngle == 0) { cornerRot = 270; return leftCorner; }
        if (prevAngle == 0 && newAngle == 270) { cornerRot = 0; return rightCorner; }
        if (prevAngle == 270 && newAngle == 180) { cornerRot = 270; return rightCorner; }
        if (prevAngle == 180 && newAngle == 90) { cornerRot = 180; return rightCorner; }
        if (prevAngle == 90 && newAngle == 0) { cornerRot = 90; return rightCorner; }
        return null;
    }
    void HandleRotation()
    {
        bool pivotAroundCenter = !inputActions.Factory.MultiBuild.IsPressed();
        if (inputActions.Factory.RotateLeft.WasPressedThisFrame())
        {
            if (editMode && (editState == EditInteractionState.MovingSelection || ClipboardManager.Instance.isPasteMode))
            {
                ClipboardManager.Instance.RotateClipboard(90, pivotAroundCenter);
                if (editState == EditInteractionState.MovingSelection)
                    UpdateMovingSelectionGhosts(WorldToCell(GetMouseWorld()));
                else
                    lastPasteCenter = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            }
            else if (editMode)
            {
                RotateSelected(90);
            }
            else if (buildMode)
            {
                selectedBuildingTempRotation = (selectedBuildingTempRotation + 90) % 360;
                if (isPaintingBuild)
                {
                    Vector3Int currentCell = WorldToCell(GetMouseWorld());
                    PaintCell(currentCell, false);
                }
            }
        }
        if (inputActions.Factory.RotateRight.WasPressedThisFrame())
        {
            if (editMode && (editState == EditInteractionState.MovingSelection || ClipboardManager.Instance.isPasteMode))
            {
                ClipboardManager.Instance.RotateClipboard(-90, pivotAroundCenter);
                if (editState == EditInteractionState.MovingSelection)
                    UpdateMovingSelectionGhosts(WorldToCell(GetMouseWorld()));
                else
                    lastPasteCenter = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            }
            else if (editMode)
            {
                RotateSelected(-90);
            }
            else if (buildMode)
            {
                selectedBuildingTempRotation = (selectedBuildingTempRotation - 90 + 360) % 360;
                if (isPaintingBuild)
                {
                    Vector3Int currentCell = WorldToCell(GetMouseWorld());
                    PaintCell(currentCell, false);
                }
            }
        }
    }
    void ApplyChanges(bool isUndoRedo = false)
    {
        if (!isUndoRedo && !isUndoingOrRedoing)
        {
            EditAction action = new EditAction();
            foreach (var kvp in plannedBuilds) action.builtCells[kvp.Key] = kvp.Value;
            foreach (var cell in plannedDeletes)
            {
                var block = GridManager.Instance.GetBuilding(cell)?.GetComponent<FactoryBlock>();
                if (block != null)
                {
                    var bData = availableBuildings.Find(b => b.buildingName == block.blockId);
                    int rot = Mathf.RoundToInt(block.transform.eulerAngles.z);
                    action.deletedBuildings[cell] = new PlannedBuild(bData, rot);
                }
            }
            foreach (var kvp in plannedRotations)
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
        foreach (var cell in plannedDeletes)
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
                if (building != null) Destroy(building.gameObject);
                changed.Add(cell);
            }
        }

        foreach (var block in blocksToDestroy)
        {
            BuildingData data = availableBuildings.Find(b => b.buildingName == block.blockId);
            if (data != null && data.cost != null)
            {
                foreach (var stack in data.cost)
                {
                    ResourcesManager.instance.addResourceAmount(stack.type, stack.amount);
                }
            }
            block.OnRemoved();
            if (block != null) Destroy(block.gameObject);
        }

        foreach (var cell in plannedDeletes)
        {
            changed.Add(cell);
        }
        foreach (var pair in plannedRotations)
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
        foreach (var pair in plannedBuilds)
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
            GameObject prefab = GetPrefab(pair.Value.type);
            if (prefab == null) continue;
            GameObject go = Instantiate(prefab,
                CellToWorld(pair.Key),
                Quaternion.Euler(0, 0, pair.Value.rotation));

            if (go.TryGetComponent<FactoryBlock>(out var block))
            {
                block.blockId = pair.Value.type.buildingName;
                block.Initialize();
                block.OnPlaced();
            }
            changed.Add(pair.Key);
        }
        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }
        ClearAll();
        if (changed.Count > 0)
        {
            Save();
        }
    }
    public void SetSelectedBuilding(BuildingData data)
    {
        if (data == null) return;

        buildMode = true;
        selectedBuilding = data;
        selectedBuildingTempRotation = 0;

        if (StorageWindow.Instance != null)
        {
            StorageWindow.Instance.Close();
        }

        Debug.Log($"Selected building from menu: {data.buildingName}");
    }
    public void SpawnBuildingFromSave(string blockId, Vector3Int cell, int rotation, string customState)
    {
        BuildingData data = availableBuildings.Find(b => b.buildingName == blockId);
        if (data == null || data.prefab == null) return;
        GameObject go = Instantiate(data.prefab, CellToWorld(cell), Quaternion.Euler(0, 0, rotation));
        if (go.TryGetComponent<FactoryBlock>(out var block))
        {
            block.blockId = blockId;
            block.Initialize();
            block.LoadSaveState(customState);
            block.OnPlaced();
        }

        GridManager.Instance.NotifyNeighbours(cell);
    }
    private string saveKey = "FactoryBuildingsSave";
    private SaveData.FactoryData GetSaveSnapshot()
    {
        var data = new SaveData.FactoryData();
        FactoryBlock[] allBlocks = FindObjectsByType<FactoryBlock>(FindObjectsSortMode.None);
        foreach (var block in allBlocks)
        {
            if (string.IsNullOrEmpty(block.blockId)) continue;
            SaveData.BuildingSaveData bsd = new SaveData.BuildingSaveData
            {
                blockId = block.blockId,
                position = block.GridPosition,
                zRotation = Mathf.RoundToInt(block.transform.eulerAngles.z),
                customDataJson = block.GetSaveState()
            };
            data.buildings.Add(bsd);
        }
        return data;
    }
    public void Save()
    {
        SaveManager.Save(saveKey, GetSaveSnapshot());
        Debug.Log("Saved Factory Buildings");
    }
    public void Load()
    {
        var data = SaveManager.Load<SaveData.FactoryData>(saveKey);
        if (data != null && data.buildings != null)
        {
            FactoryBlock[] allBlocks = FindObjectsByType<FactoryBlock>(FindObjectsSortMode.None);
            foreach (var block in allBlocks)
            {
                block.OnRemoved();
                Destroy(block.gameObject);
            }
            foreach (var bsd in data.buildings)
            {
                SpawnBuildingFromSave(bsd.blockId, bsd.position, bsd.zRotation, bsd.customDataJson);
            }
        }
        Debug.Log("Loaded Factory Buildings");
    }
    #endregion
    #region Grid Math
    Grid GetGrid()
    {
        if (grid == null) grid = FindFirstObjectByType<Grid>();
        return grid;
    }
    Camera GetCamera()
    {
        if (cam == null) cam = Camera.main;
        return cam;
    }
    Vector3Int WorldToCell(Vector3 pos)
    {
        return GetGrid().WorldToCell(pos + new Vector3(0.5f, 0.5f, 0f));
    }
    Vector3 GetMouseWorld()
    {
        Ray ray =
            GetCamera().ScreenPointToRay(
                Input.mousePosition);
        Plane plane =
            new Plane(
                Vector3.forward,
                Vector3.zero);
        if (plane.Raycast(
            ray,
            out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
    #endregion
    #region Selection Logic

    void HandlePasteMode()
    {
        Vector3Int currentCenter = WorldToCell(GetMouseWorld());
        if (currentCenter != lastPasteCenter)
        {
            BuildGhostManager.Instance.ClearPasteGhosts();
            lastPasteCenter = currentCenter;
            foreach (var item in ClipboardManager.Instance.GetClipboard())
            {
                Vector3Int targetCell = currentCenter + item.offset;
                BuildGhostManager.Instance.CreatePasteGhost(targetCell, item.type, item.rotation);
            }
        }
        if (inputActions.Factory.Select.WasPressedThisFrame())
        {
            bool pasted = false;
            foreach (var item in ClipboardManager.Instance.GetClipboard())
            {
                Vector3Int targetCell = currentCenter + item.offset;
                if (!GridManager.Instance.IsOccupied(targetCell) && !plannedBuilds.ContainsKey(targetCell))
                {
                    plannedBuilds[targetCell] = new PlannedBuild(item.type, item.rotation);
                    BuildGhostManager.Instance.CreateBuildGhost(targetCell, item.type, item.rotation);
                    pasted = true;
                }
            }

            if (pasted && editMode)
            {
                ApplyChanges();
            }
        }
        if (inputActions.Factory.Delete.WasPressedThisFrame() || inputActions.Factory.Paste.WasPressedThisFrame())
        {
            ClipboardManager.Instance.isPasteMode = false;
            BuildGhostManager.Instance.ClearPasteGhosts();
        }
    }
    public void CutSelected()
    {
        foreach (var cell in selectedCells)
        {
            if (plannedBuilds.ContainsKey(cell))
            {
                plannedBuilds.Remove(cell);
                BuildGhostManager.Instance.RemoveBuildGhost(cell);
            }
            else if (GridManager.Instance.IsOccupied(cell))
            {
                if (!plannedDeletes.Contains(cell))
                {
                    plannedDeletes.Add(cell);
                    BuildGhostManager.Instance.CreateDeleteMarker(cell);
                }
            }
        }

        if (editMode)
        {
            ApplyChanges();
        }
    }
    void ToggleCell(Vector3Int cell, bool deleteMode)
    {
        if (selectedCells.Contains(cell))
        {
            selectedCells.Remove(cell);
            plannedBuilds.Remove(cell);
            plannedDeletes.Remove(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
            BuildGhostManager.Instance.RemoveDeleteMarker(cell);
            var rGhost = BuildGhostManager.Instance.GetRotationGhost(cell);
            if (rGhost != null)
            {
                Destroy(rGhost);
                BuildGhostManager.Instance.RemoveRotationGhost(cell);
            }
            plannedRotations.Remove(cell);
            return;
        }
        PaintCell(cell, deleteMode);
    }
    void PaintCell(Vector3Int cell, bool deleteMode)
    {
        if (deleteMode)
        {
            if (plannedBuilds.ContainsKey(cell))
            {
                plannedBuilds.Remove(cell);
                BuildGhostManager.Instance.RemoveBuildGhost(cell);
            }
            if (!plannedDeletes.Contains(cell))
            {
                selectedCells.Add(cell);
                plannedDeletes.Add(cell);
                BuildGhostManager.Instance.CreateDeleteMarker(cell);
            }
            return;
        }
        if (selectedBuilding == null) return;
        if (plannedDeletes.Contains(cell))
        {
            plannedDeletes.Remove(cell);
            BuildGhostManager.Instance.RemoveDeleteMarker(cell);
        }
        if (!GridManager.Instance.IsOccupied(cell))
        {
            selectedCells.Add(cell);

            if (!plannedBuilds.ContainsKey(cell))
            {
                plannedBuilds[cell] = new PlannedBuild(selectedBuilding, selectedBuildingTempRotation);
                BuildGhostManager.Instance.CreateBuildGhost(cell, selectedBuilding, selectedBuildingTempRotation);
            }
            else
            {
                plannedBuilds[cell].type = selectedBuilding;
                plannedBuilds[cell].rotation = selectedBuildingTempRotation;
                BuildGhostManager.Instance.RemoveBuildGhost(cell);
                BuildGhostManager.Instance.CreateBuildGhost(cell, selectedBuilding, selectedBuildingTempRotation);
            }
        }
        else
        {
            if (!selectedCells.Contains(cell))
            {
                selectedCells.Add(cell);
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                if (building != null)
                {
                    var ghost = BuildGhostManager.Instance.GetRotationGhost(cell);
                    if (ghost == null)
                    {
                        ghost = new GameObject($"GhostRotation_{building.name}");
                        ghost.transform.position = building.transform.position;
                        BuildGhostManager.Instance.CopySprites(building.gameObject, ghost);
                        BuildGhostManager.Instance.SetRotationGhost(cell, ghost);
                    }
                    ghost.transform.rotation = Quaternion.Euler(0, 0, Mathf.RoundToInt(building.transform.eulerAngles.z));
                }
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
                ToggleCell(
                    new Vector3Int(x, y, 0),
                    deleteMode
                );
            }
        }
    }
    #endregion
    #region Operations
    void RotateSelected(int angle)
    {
        HashSet<Vector3Int> changed = new HashSet<Vector3Int>();
        foreach (var cell in selectedCells)
        {
            if (plannedBuilds.TryGetValue(cell, out var build))
            {
                build.rotation += angle;
                var ghost = BuildGhostManager.Instance.GetBuildGhost(cell);
                if (ghost != null)
                {
                    ghost.transform.rotation = Quaternion.Euler(0, 0, build.rotation);
                }
                changed.Add(cell);
            }
            else if (editMode)
            {
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                if (building != null)
                {
                    if (!plannedRotations.ContainsKey(cell))
                    {
                        plannedRotations[cell] = Mathf.RoundToInt(building.transform.eulerAngles.z) + angle;
                    }
                    else
                    {
                        plannedRotations[cell] += angle;
                    }
                    var ghost = BuildGhostManager.Instance.GetRotationGhost(cell);
                    if (ghost == null)
                    {
                        ghost = new GameObject($"GhostRotation_{building.name}");
                        ghost.transform.position = building.transform.position;
                        BuildGhostManager.Instance.CopySprites(building.gameObject, ghost);
                        BuildGhostManager.Instance.SetRotationGhost(cell, ghost);
                    }
                    ghost.transform.rotation = Quaternion.Euler(0, 0, plannedRotations[cell]);
                    changed.Add(cell);
                }
            }
        }
        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }
    }
    public GameObject GetPrefab(BuildingData data)
    {
        if (data != null && data.prefab != null) return data.prefab;
        return null;
    }
    public Vector3 CellToWorld(Vector3Int cell)
    {
        return GetGrid().CellToWorld(cell);
    }
    #endregion
    #region Visuals & Ghosts

    public void UndoLastAction()
    {
        if (undoStack.Count == 0) return;
        
        EditAction action = undoStack.Pop();
        redoStack.Push(action);
        
        ClearAll();
        
        foreach (var kvp in action.builtCells)
        {
            plannedDeletes.Add(kvp.Key);
        }
        
        foreach (var kvp in action.originalRotations)
        {
            plannedRotations[kvp.Key] = kvp.Value;
        }
        
        foreach (var kvp in action.deletedBuildings)
        {
            plannedBuilds[kvp.Key] = kvp.Value;
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
        
        ClearAll();
        
        foreach (var kvp in action.deletedBuildings)
        {
            plannedDeletes.Add(kvp.Key);
        }
        
        foreach (var kvp in action.newRotations)
        {
            plannedRotations[kvp.Key] = kvp.Value;
        }
        
        foreach (var kvp in action.builtCells)
        {
            plannedBuilds[kvp.Key] = kvp.Value;
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
    
    public bool ClearAll()

    {
        bool clearedSomething = true;
        BuildGhostManager.Instance.ClearAllGhosts();
        plannedBuilds.Clear();
        plannedDeletes.Clear();
        plannedRotations.Clear();
        selectedCells.Clear();
        BuildGhostManager.Instance.ClearPasteGhosts();
        plannedBuilds.Clear();
        plannedDeletes.Clear();
        plannedRotations.Clear();
        selectedCells.Clear();

        if (ClipboardManager.Instance != null) ClipboardManager.Instance.isPasteMode = false;
        return clearedSomething;
    }
    #endregion
}
public class EditAction
{
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, BuildManager.PlannedBuild> builtCells = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, BuildManager.PlannedBuild>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, BuildManager.PlannedBuild> deletedBuildings = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, BuildManager.PlannedBuild>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int> originalRotations = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int> newRotations = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
}
