using System.Collections.Generic;
using UnityEngine;
public class BuildManager : MonoBehaviour, IGameService
{
    public BuildHistoryManager History { get; private set; }
    public BuildInteractionManager Interaction { get; private set; }

    public Grid Grid => grid;
    public EditInteractionState EditState
    {
        get => editState;
        set => editState = value;
    }

    #region Variables
    public Camera cam;
    [SerializeField]
    private bool buildMode = false;
    public bool IsBuildMode => buildMode;

    [SerializeField]
    private bool editMode = false;

    public bool IsEditMode => editMode;
    private Grid grid;
    public BuildingData selectedBuilding;
    public HashSet<Vector3Int> selectedCells = new();
    public Dictionary<Vector3Int, PlannedBuild> plannedBuilds = new();
    public HashSet<Vector3Int> plannedDeletes = new();
    public Dictionary<Vector3Int, int> plannedRotations = new Dictionary<Vector3Int, int>();
    public Vector3Int lastPasteCenter;
    private Dictionary<Vector3Int, GameObject> pasteGhosts = new Dictionary<Vector3Int, GameObject>();

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
    public InputSystem_Actions inputActions;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        inputActions = new InputSystem_Actions();
        History = new BuildHistoryManager(this);
        Interaction = new BuildInteractionManager(this);
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
        if (inputActions.Factory.Undo.WasPressedThisFrame()) History.UndoLastAction();
        if (inputActions.Factory.Redo.WasPressedThisFrame()) History.RedoLastAction();
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
            Interaction.HandleSelection();
        else if (editMode)
            Interaction.HandleEditSelection();
        Interaction.HandleRotation();
        if (inputActions.Factory.Apply.WasPressedThisFrame() && buildMode)
            History.ApplyChanges();
        bool ctrl = UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed;
        if (ctrl && UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (EditState != EditInteractionState.MovingSelection)
                ClipboardManager.Instance.Copy(selectedCells);
        }
        if (ctrl && UnityEngine.InputSystem.Keyboard.current.xKey.wasPressedThisFrame)
        {
            if (EditState != EditInteractionState.MovingSelection)
            {
                ClipboardManager.Instance.Cut(selectedCells);
                History.CutSelected();
                EditState = EditInteractionState.None;
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
    private EditInteractionState editState = EditInteractionState.None;

    public void TransitionToMovingSelection()
    {
        Vector3Int center = ClipboardManager.Instance.GetCenterOfSelection(selectedCells);
        Interaction.movingSelectionCenter = new Vector2Int(center.x, center.y);
        ClipboardManager.Instance.Copy(selectedCells);

        foreach (var cell in selectedCells)
        {
            plannedDeletes.Add(cell);
            BuildGhostManager.Instance.CreateDeleteMarker(cell);
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        EditState = EditInteractionState.MovingSelection;
        UpdateMovingSelectionGhosts(BuildGridUtils.WorldToCell(this, BuildGridUtils.GetMouseWorld(this)));
    }
    public void UpdateMovingSelectionGhosts(Vector3Int mouseCell)
    {
        foreach (var cell in new List<Vector3Int>(plannedBuilds.Keys))
        {
            BuildGhostManager.Instance.RemoveBuildGhost(cell);
        }
        plannedBuilds.Clear();
        Vector3Int offset = new Vector3Int(mouseCell.x - Interaction.movingSelectionCenter.x, mouseCell.y - Interaction.movingSelectionCenter.y, 0);
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
    public void ToggleEditCell(Vector3Int cell)
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
    public BuildingData DetermineCorner(int prevAngle, int newAngle, out int cornerRot)
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
    public void SetSelectedBuilding(BuildingData data)
    {
        if (data == null) return;

        buildMode = true;
        selectedBuilding = data;
        Interaction.selectedBuildingTempRotation = 0;

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
        GameObject go = Instantiate(data.prefab, BuildGridUtils.CellToWorld(this, cell), Quaternion.Euler(0, 0, rotation));
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
    #endregion
    #region Selection Logic

    public void HandlePasteMode()
    {
        Vector3Int currentCenter = BuildGridUtils.WorldToCell(this, BuildGridUtils.GetMouseWorld(this));
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
                History.ApplyChanges();
            }
        }
        if (inputActions.Factory.Delete.WasPressedThisFrame() || inputActions.Factory.Paste.WasPressedThisFrame())
        {
            ClipboardManager.Instance.isPasteMode = false;
            BuildGhostManager.Instance.ClearPasteGhosts();
        }
    }
    public void ToggleCell(Vector3Int cell, bool deleteMode)
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
    public void PaintCell(Vector3Int cell, bool deleteMode)
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
                plannedBuilds[cell] = new PlannedBuild(selectedBuilding, Interaction.selectedBuildingTempRotation);
                BuildGhostManager.Instance.CreateBuildGhost(cell, selectedBuilding, Interaction.selectedBuildingTempRotation);
            }
            else
            {
                plannedBuilds[cell].type = selectedBuilding;
                plannedBuilds[cell].rotation = Interaction.selectedBuildingTempRotation;
                BuildGhostManager.Instance.RemoveBuildGhost(cell);
                BuildGhostManager.Instance.CreateBuildGhost(cell, selectedBuilding, Interaction.selectedBuildingTempRotation);
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
    #endregion
    #region Operations
    public void RotateSelected(int angle)
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
    #endregion
    #region Visuals & Ghosts
    
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
