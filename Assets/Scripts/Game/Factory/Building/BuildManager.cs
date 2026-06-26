using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour, IGameService
{
    #region ??????????
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

    public class ClipboardItem
    {
        public BuildingData type;
        public int rotation;
        public Vector3Int offset;

        public ClipboardItem(BuildingData type, int rotation, Vector3Int offset)
        {
            this.type = type;
            this.rotation = rotation;
            this.offset = offset;
        }
    }

    // (List of prefabs removed, use BuildingData directly)

    public GameObject deleteMarkerPrefab;
    [SerializeField]
    private bool buildMode = false;
    public bool IsBuildMode => buildMode;

    [SerializeField]
    private bool editMode = false;
    public bool IsEditMode => editMode;
    private Grid grid;

    public BuildingData selectedBuilding;

    private HashSet<Vector3Int> selectedCells = new();

    private Dictionary<Vector3Int, PlannedBuild> plannedBuilds = new();
    private HashSet<Vector3Int> plannedDeletes = new();

    private Dictionary<Vector3Int, GameObject> buildGhosts = new();
    private Dictionary<Vector3Int, GameObject> rotationGhosts = new();
    private Dictionary<Vector3Int, int> plannedRotations = new Dictionary<Vector3Int, int>();

    private List<ClipboardItem> clipboard = new List<ClipboardItem>();
    private bool isPasteMode = false;
    private Vector3Int lastPasteCenter;
    private Dictionary<Vector3Int, GameObject> pasteGhosts = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> deleteMarkers = new();

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
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            autoSaveTimer = 0f;
            Save();
        }

        bool bPressed = inputActions.Factory.ToggleBuildMode.WasPressedThisFrame();

        if (bPressed)
        {
            editMode = false;

            if (buildMode)
            {
                // Если стройка активна, B отменяет стройку
                buildMode = false;
                ClearAll();
                Debug.Log("Build mode cancelled.");

                // И открывает меню
                if (BuildMenuWindow.Instance != null)
                {
                    BuildMenuWindow.Instance.Open();
                }
            }
            else
            {
                // Если стройка неактивна, B открывает/закрывает меню зданий
                if (BuildMenuWindow.Instance != null)
                {
                    BuildMenuWindow.Instance.ToggleWindow();
                }
            }
        }

        if (!buildMode && !editMode)
            return;

        if (buildMode)
            HandleBuildingSelection();

        if (buildMode)
            HandleSelection();
        else if (editMode)
            HandleEditSelection();

        HandleRotation();

        if (inputActions.Factory.Apply.WasPressedThisFrame() && buildMode)
            ApplyChanges();

        bool ctrl = UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed;

        if (ctrl && UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
            CopySelected();

        if (ctrl && UnityEngine.InputSystem.Keyboard.current.xKey.wasPressedThisFrame)
            CutSelected();

        if (ctrl && UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (clipboard.Count > 0)
            {
                isPasteMode = !isPasteMode;
                if (!isPasteMode) ClearPasteGhosts();
            }
        }

        // Снятие выделения на Escape
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearAll();
        }
    }
    #region Main Functions
    [Header("Available Buildings")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();

    public void OnBuildUIButtonClicked()
    {
        editMode = false;
        
        if (buildMode)
        {
            // Отменяем режим строительства
            buildMode = false;
            ClearAll();
            Debug.Log("Build mode cancelled via UI button.");

            // И открываем меню (как при нажатии B)
            if (BuildMenuWindow.Instance != null)
            {
                BuildMenuWindow.Instance.Open();
            }
        }
        else
        {
            // Открываем/закрываем меню
            if (BuildMenuWindow.Instance != null)
            {
                BuildMenuWindow.Instance.ToggleWindow();
            }
        }
    }

    public void OnEditUIButtonClicked()
    {
        if (editMode)
        {
            editMode = false;
            ClearAll();
            Debug.Log("Edit mode disabled.");
        }
        else
        {
            editMode = true;
            buildMode = false;
            ClearAll();
            if (BuildMenuWindow.Instance != null)
            {
                BuildMenuWindow.Instance.Close();
            }
            Debug.Log("Edit mode enabled.");
        }
    }

    void HandleBuildingSelection()
    {
        if (!buildMode) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha1) && availableBuildings.Count > 0)
        {
            selectedBuilding = availableBuildings[0];
            Debug.Log("Selected: " + selectedBuilding.buildingName);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && availableBuildings.Count > 1)
        {
            selectedBuilding = availableBuildings[1];
            Debug.Log("Selected: " + selectedBuilding.buildingName);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && availableBuildings.Count > 2)
        {
            selectedBuilding = availableBuildings[2];
            Debug.Log("Selected: " + selectedBuilding.buildingName);
        }
    }

    private Vector3Int lastDraggedCell;
    private int selectedBuildingTempRotation = 0;
    private bool deletingRectangle = false;

    void HandleSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isPasteMode)
        {
            if (!pointerOverUI) HandlePasteMode();
            return;
        }

        // 1. Постройка (Левая кнопка мыши)
        if (inputActions.Factory.Select.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;

            anchorA = WorldToCell(GetMouseWorld());
            lastDraggedCell = anchorA;
            selectedBuildingTempRotation = 0;
            ToggleCell(anchorA, false); // false = не удалять, а строить
        }
        else if (inputActions.Factory.Select.IsPressed() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;

            // Непрерывная постройка при зажатии (линия)
            Vector3Int currentCell = WorldToCell(GetMouseWorld());
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
                                // Угол изменился! Заменяем прямой на угловой
                                BuildingData cornerData = DetermineCorner(prevAngle, angle, out int cornerRot);
                                if (cornerData != null)
                                {
                                    lastBuild.type = cornerData;
                                    lastBuild.rotation = cornerRot;
                                    if (buildGhosts.TryGetValue(lastDraggedCell, out var lastGhost))
                                    {
                                        Destroy(lastGhost);
                                        buildGhosts.Remove(lastDraggedCell);
                                        CreateBuildGhost(lastDraggedCell, cornerData, cornerRot);
                                    }
                                }
                                else
                                {
                                    // Fallback if corners missing
                                    lastBuild.rotation = angle;
                                    if (buildGhosts.TryGetValue(lastDraggedCell, out var lastGhost))
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
        // 2. Выделение рамкой (Shift + ЛКМ)
        if (inputActions.Factory.Select.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            selectingRectangle = true;
            anchorA = WorldToCell(GetMouseWorld());
            deletingRectangle = false;
        }

        // 3. Удаление (Правая кнопка мыши)
        if (inputActions.Factory.Delete.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = WorldToCell(GetMouseWorld());
            ToggleCell(anchorA, true); // true = удалять
            deletingRectangle = true;
            selectingRectangle = true; // Запускаем выделение рамкой для удаления
        }

        if (inputActions.Factory.Select.WasReleasedThisFrame() && selectingRectangle)
        {
            selectingRectangle = false;
            Vector3Int anchorB = WorldToCell(GetMouseWorld());
            SelectRectangle(anchorA, anchorB, false);
        }

        // 2. Удаление (Правая кнопка мыши)
        if (inputActions.Factory.Delete.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = WorldToCell(GetMouseWorld());
            ToggleCell(anchorA, true); // Клик - переключает
        }
        else if (inputActions.Factory.Delete.IsPressed() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            // Непрерывное удаление при зажатии (кисть)
            Vector3Int currentCell = WorldToCell(GetMouseWorld());
            PaintCell(currentCell, true); // Зажатие - закрашивает
        }
        else if (inputActions.Factory.Delete.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            anchorA = WorldToCell(GetMouseWorld());
            deletingRectangle = true;
        }

        if (inputActions.Factory.Delete.WasReleasedThisFrame() && deletingRectangle)
        {
            deletingRectangle = false;
            Vector3Int anchorB = WorldToCell(GetMouseWorld());
            SelectRectangle(anchorA, anchorB, true);
        }
    }

    void HandleEditSelection()
    {
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isPasteMode)
        {
            if (!pointerOverUI) HandlePasteMode();
            return;
        }

        // Dragging logic for selected buildings
        if (inputActions.Factory.Select.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            anchorA = WorldToCell(GetMouseWorld());
            
            // Если кликнули на невыделенное здание, сбрасываем выделение и выделяем его
            if (GridManager.Instance.IsOccupied(anchorA) && !selectedCells.Contains(anchorA))
            {
                ClearAll();
                ToggleEditCell(anchorA);
            }
            
            lastDraggedCell = anchorA;
            isDraggingSelection = true;
        }
        else if (inputActions.Factory.Select.IsPressed() && isDraggingSelection)
        {
            Vector3Int currentCell = WorldToCell(GetMouseWorld());
            if (currentCell != lastDraggedCell)
            {
                Vector3Int diff = currentCell - lastDraggedCell;
                if (diff.sqrMagnitude > 0 && selectedCells.Count > 0 && !hasCopiedForDrag)
                {
                    // Сначала копируем выделенные здания и сразу удаляем оригиналы (Cut)
                    CopySelected();
                    
                    // Мы не вызываем CutSelected(), потому что он вызовет ApplyChanges и ClearAll().
                    // Мы вручную удаляем их:
                    foreach (var cell in selectedCells)
                    {
                        plannedDeletes.Add(cell);
                        CreateDeleteMarker(cell);
                    }
                    hasCopiedForDrag = true;
                }
                
                if (hasCopiedForDrag)
                {
                    // Очищаем предыдущие призраки перетаскивания
                    foreach (var cell in new List<Vector3Int>(plannedBuilds.Keys))
                    {
                        plannedBuilds.Remove(cell);
                        RemoveBuildGhost(cell);
                    }
                    
                    // Создаем новые призраки на новой позиции
                    foreach (var item in clipboard)
                    {
                        Vector3Int targetCell = currentCell + item.offset;
                        if (!GridManager.Instance.IsOccupied(targetCell) && !plannedBuilds.ContainsKey(targetCell))
                        {
                            plannedBuilds[targetCell] = new PlannedBuild(item.type, item.rotation);
                            CreateBuildGhost(targetCell, item.type, item.rotation);
                        }
                    }
                }
                lastDraggedCell = currentCell;
            }
        }
        
        if (inputActions.Factory.Select.WasReleasedThisFrame())
        {
            if (isDraggingSelection && hasCopiedForDrag)
            {
                ApplyChanges();
            }
            isDraggingSelection = false;
            hasCopiedForDrag = false;
        }

        // 2. Выделение рамкой (Shift + ЛКМ)
        if (inputActions.Factory.Select.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            if (pointerOverUI) return;
            selectingRectangle = true;
            anchorA = WorldToCell(GetMouseWorld());
        }

        if (inputActions.Factory.Select.WasReleasedThisFrame() && selectingRectangle)
        {
            selectingRectangle = false;
            Vector3Int anchorB = WorldToCell(GetMouseWorld());
            SelectEditRectangle(anchorA, anchorB);
        }

        // 3. Удаление (Правая кнопка мыши)
        if (inputActions.Factory.Delete.WasPressedThisFrame())
        {
            if (pointerOverUI) return;
            Vector3Int clickCell = WorldToCell(GetMouseWorld());
            
            // Если кликнули на здание, удаляем его
            if (GridManager.Instance.IsOccupied(clickCell))
            {
                plannedDeletes.Add(clickCell);
                CreateDeleteMarker(clickCell);
                ApplyChanges(); // Немедленно удаляем
            }
            else if (selectedCells.Count > 0)
            {
                // Удаляем все выделенные
                foreach (var cell in selectedCells)
                {
                    plannedDeletes.Add(cell);
                }
                ApplyChanges();
            }
        }
    }
    
    private bool isDraggingSelection = false;
    private bool hasCopiedForDrag = false;

    void ToggleEditCell(Vector3Int cell)
    {
        if (selectedCells.Contains(cell))
        {
            selectedCells.Remove(cell);
            RemoveBuildGhost(cell);
            return;
        }

        if (GridManager.Instance.IsOccupied(cell))
        {
            selectedCells.Add(cell);
            // Визуализируем выделение (например, призраком или маркером)
            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
            if (building != null && !buildGhosts.ContainsKey(cell))
            {
                GameObject ghost = new GameObject($"EditSelection_{building.name}");
                ghost.transform.position = building.transform.position;
                ghost.transform.rotation = building.transform.rotation;
                CopySprites(building.gameObject, ghost);
                // Make it look selected (e.g., slightly blue or white overlay)
                SpriteRenderer[] srs = ghost.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in srs)
                {
                    sr.color = new Color(0.5f, 0.8f, 1f, 0.7f); // Голубоватый цвет
                    sr.sortingOrder += 10;
                }
                buildGhosts[cell] = ghost;
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

        // LEFT TURNS
        if (prevAngle == 0 && newAngle == 90) { cornerRot = 0; return leftCorner; }
        if (prevAngle == 90 && newAngle == 180) { cornerRot = 90; return leftCorner; }
        if (prevAngle == 180 && newAngle == 270) { cornerRot = 180; return leftCorner; }
        if (prevAngle == 270 && newAngle == 0) { cornerRot = 270; return leftCorner; }

        // RIGHT TURNS
        if (prevAngle == 0 && newAngle == 270) { cornerRot = 0; return rightCorner; }
        if (prevAngle == 270 && newAngle == 180) { cornerRot = 270; return rightCorner; }
        if (prevAngle == 180 && newAngle == 90) { cornerRot = 180; return rightCorner; }
        if (prevAngle == 90 && newAngle == 0) { cornerRot = 90; return rightCorner; }

        return null;
    }

    void HandleRotation()
    {
        if (inputActions.Factory.RotateLeft.WasPressedThisFrame())
            RotateSelected(90);

        if (inputActions.Factory.RotateRight.WasPressedThisFrame())
            RotateSelected(-90);
    }

    void ApplyChanges()
    {
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

            // Check resources before building
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

            // Spend resources
            if (pair.Value.type.cost != null)
            {
                foreach (var stack in pair.Value.type.cost)
                {
                    // addResourceAmount supports subtracting if amount is negative
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
                block.blockId = pair.Value.type.buildingName; // Save ID
                block.Initialize(); // Initialize and register in Grid immediately!
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
        
        // Закрываем окно склада, если оно открыто
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
            block.Initialize(); // Initialize and register in Grid immediately!
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
            if (string.IsNullOrEmpty(block.blockId)) continue; // Can't save blocks without ID

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
            // Destroy existing blocks
            FactoryBlock[] allBlocks = FindObjectsByType<FactoryBlock>(FindObjectsSortMode.None);
            foreach (var block in allBlocks)
            {
                block.OnRemoved();
                Destroy(block.gameObject);
            }

            // Spawn from save
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

        if (currentCenter != lastPasteCenter || pasteGhosts.Count == 0)
        {
            ClearPasteGhosts();
            lastPasteCenter = currentCenter;

            foreach (var item in clipboard)
            {
                Vector3Int targetCell = currentCenter + item.offset;
                GameObject ghost = new GameObject($"PasteGhost_{item.type.buildingName}");
                ghost.transform.position = CellToWorld(targetCell);
                ghost.transform.rotation = Quaternion.Euler(0, 0, item.rotation);

                GameObject prefab = GetPrefab(item.type);
                if (prefab != null) CopySprites(prefab, ghost);
                
                // Сделаем призраки синими, чтобы отличать от обычных
                SpriteRenderer[] renderers = ghost.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers) r.color = new Color(0, 0.5f, 1f, 0.5f);

                pasteGhosts[targetCell] = ghost;
            }
        }

        if (inputActions.Factory.Select.WasPressedThisFrame())
        {
            // Вставляем!
            bool pasted = false;
            foreach (var item in clipboard)
            {
                Vector3Int targetCell = currentCenter + item.offset;
                if (!GridManager.Instance.IsOccupied(targetCell) && !plannedBuilds.ContainsKey(targetCell))
                {
                    plannedBuilds[targetCell] = new PlannedBuild(item.type, item.rotation);
                    CreateBuildGhost(targetCell, item.type, item.rotation);
                    pasted = true;
                }
            }
            
            if (pasted && editMode)
            {
                ApplyChanges();
                // We keep paste mode active to allow pasting again, but the user can right click to cancel
            }
        }

        if (inputActions.Factory.Delete.WasPressedThisFrame() || inputActions.Factory.Paste.WasPressedThisFrame())
        {
            // Отмена режима вставки (правый клик или повторный Paste)
            isPasteMode = false;
            ClearPasteGhosts();
        }
    }

    void CopySelected()
    {
        if (selectedCells.Count == 0) return;

        clipboard.Clear();

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (var cell in selectedCells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }
        
        Vector3Int anchor = new Vector3Int(minX, minY, 0);

        foreach (var cell in selectedCells)
        {
            BuildingData typeToCopy = null;
            int rotToCopy = 0;

            if (plannedBuilds.TryGetValue(cell, out var build))
            {
                typeToCopy = build.type;
                rotToCopy = build.rotation;
            }
            else
            {
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                if (building is FactoryBlock block)
                {
                    typeToCopy = availableBuildings.Find(b => b.buildingName == block.blockId);
                    rotToCopy = Mathf.RoundToInt(block.transform.eulerAngles.z);
                    
                    if (plannedRotations.TryGetValue(cell, out int pRot))
                        rotToCopy = pRot;
                }
            }

            if (typeToCopy != null)
            {
                clipboard.Add(new ClipboardItem(typeToCopy, rotToCopy, cell - anchor));
            }
        }

        Debug.Log($"Copied {clipboard.Count} buildings. Paste mode ready.");
    }

    void CutSelected()
    {
        CopySelected();

        foreach (var cell in selectedCells)
        {
            if (plannedBuilds.ContainsKey(cell))
            {
                plannedBuilds.Remove(cell);
                RemoveBuildGhost(cell);
            }
            else if (GridManager.Instance.IsOccupied(cell))
            {
                if (!plannedDeletes.Contains(cell))
                {
                    plannedDeletes.Add(cell);
                    CreateDeleteMarker(cell);
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

            RemoveBuildGhost(cell);
            RemoveDeleteMarker(cell);

            if (rotationGhosts.TryGetValue(cell, out var rGhost))
            {
                Destroy(rGhost);
                rotationGhosts.Remove(cell);
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
                RemoveBuildGhost(cell);
            }
            if (!plannedDeletes.Contains(cell))
            {
                selectedCells.Add(cell);
                plannedDeletes.Add(cell);
                CreateDeleteMarker(cell);
            }
            return;
        }

        if (selectedBuilding == null) return; // Prevent NullReferenceException

        if (plannedDeletes.Contains(cell))
        {
            plannedDeletes.Remove(cell);
            RemoveDeleteMarker(cell);
        }

        if (!GridManager.Instance.IsOccupied(cell))
        {
            selectedCells.Add(cell);
            
            if (!plannedBuilds.ContainsKey(cell))
            {
                plannedBuilds[cell] = new PlannedBuild(selectedBuilding, selectedBuildingTempRotation);
                CreateBuildGhost(cell, selectedBuilding, selectedBuildingTempRotation);
            }
            else
            {
                plannedBuilds[cell].type = selectedBuilding;
                plannedBuilds[cell].rotation = selectedBuildingTempRotation;
                RemoveBuildGhost(cell);
                CreateBuildGhost(cell, selectedBuilding, selectedBuildingTempRotation);
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
                    if (!rotationGhosts.TryGetValue(cell, out var ghost))
                    {
                        ghost = new GameObject($"GhostRotation_{building.name}");
                        ghost.transform.position = building.transform.position; 
                        CopySprites(building.gameObject, ghost);
                        rotationGhosts[cell] = ghost;
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
                if (buildGhosts.TryGetValue(cell, out var ghost))
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

                    if (!rotationGhosts.TryGetValue(cell, out var ghost))
                    {
                        ghost = new GameObject($"GhostRotation_{building.name}");
                        ghost.transform.position = building.transform.position;
                        CopySprites(building.gameObject, ghost);
                        rotationGhosts[cell] = ghost;
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

    GameObject GetPrefab(BuildingData data)
    {
        if (data != null) return data.prefab;
        return null;
    }

    Vector3 CellToWorld(Vector3Int cell)
    {
        return GetGrid().CellToWorld(cell);
    }

    #endregion

    #region Visuals & Ghosts
    void ClearAll()
    {
        foreach (var ghost in buildGhosts.Values)
            Destroy(ghost);
        buildGhosts.Clear();

        foreach (var ghost in rotationGhosts.Values)
            Destroy(ghost);
        rotationGhosts.Clear();
        plannedRotations.Clear();

        foreach (var marker in deleteMarkers.Values)
            Destroy(marker);
        deleteMarkers.Clear();

        ClearPasteGhosts();
        isPasteMode = false;

        plannedBuilds.Clear();
        plannedDeletes.Clear();
        selectedCells.Clear();
    }

    void ClearPasteGhosts()
    {
        foreach (var ghost in pasteGhosts.Values)
            Destroy(ghost);
        pasteGhosts.Clear();
    }

    void CreateDeleteMarker(Vector3Int cell)
    {
        if (deleteMarkers.ContainsKey(cell))
            return;

        deleteMarkers[cell] = Instantiate(
            deleteMarkerPrefab,
            CellToWorld(cell),
            Quaternion.identity
        );
    }

    void RemoveDeleteMarker(Vector3Int cell)
    {
        if (!deleteMarkers.TryGetValue(
            cell,
            out var marker))
            return;

        Destroy(marker);

        deleteMarkers.Remove(cell);
    }

    void CreateBuildGhost(Vector3Int cell, BuildingData type, int rotation)
    {
        if (buildGhosts.ContainsKey(cell))
            return;

        GameObject prefab = GetPrefab(type);

        if (prefab == null)
            return;

        GameObject ghost = new GameObject($"Ghost_{type}");
        ghost.transform.position = CellToWorld(cell);
        ghost.transform.rotation = Quaternion.Euler(0, 0, rotation);

        CopySprites(prefab, ghost);
        buildGhosts[cell] = ghost;
    }

    void RemoveBuildGhost(Vector3Int cell)
    {
        if (!buildGhosts.TryGetValue(
            cell,
            out var ghost))
            return;

        Destroy(ghost);

        buildGhosts.Remove(cell);
    }

    void CopySprites(GameObject source, GameObject ghost)
    {
        SpriteRenderer[] renderers = source.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            GameObject child = new GameObject(renderer.name);
            child.transform.SetParent(ghost.transform, false);

            if (renderer.gameObject == source)
            {
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
            }
            else
            {
                // Правильный расчет относительной позиции для ЛЮБОГО уровня вложенности!
                child.transform.localPosition = source.transform.InverseTransformPoint(renderer.transform.position);
                child.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * renderer.transform.rotation;
                child.transform.localScale = renderer.transform.localScale; 
            }

            SpriteRenderer copy = child.AddComponent<SpriteRenderer>();
            copy.sprite = renderer.sprite;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;
            copy.flipX = renderer.flipX;
            copy.flipY = renderer.flipY;

            copy.color = new Color(0, 1, 0, 0.5f);
        }
    }
    #endregion
}



