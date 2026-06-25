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

    private InputSystem_Actions inputActions;

    private void Awake()
    {
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

        if (inputActions.Factory.ToggleBuildMode.WasPressedThisFrame())
        {
            buildMode = !buildMode;
            ClearAll();
            
            if (buildMode && StorageWindow.Instance != null)
            {
                StorageWindow.Instance.Close();
            }

            Debug.Log($"Build mode: {buildMode}");
        }

        if (!buildMode)
            return;

        HandleBuildingSelection();
        HandleSelection();
        HandleRotation();

        if (inputActions.Factory.Apply.WasPressedThisFrame())
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

    void HandleBuildingSelection()
    {
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

    private bool deletingRectangle = false;

    void HandleSelection()
    {
        if (isPasteMode)
        {
            HandlePasteMode();
            return;
        }

        // 1. Постройка (Левая кнопка мыши)
        if (inputActions.Factory.Select.WasPressedThisFrame() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            anchorA = WorldToCell(GetMouseWorld());
            ToggleCell(anchorA, false); // false = не удалять, а строить
        }
        else if (inputActions.Factory.Select.IsPressed() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            // Непрерывная постройка при зажатии (кисть)
            Vector3Int currentCell = WorldToCell(GetMouseWorld());
            if (!selectedCells.Contains(currentCell))
            {
                ToggleCell(currentCell, false);
            }
        }
        else if (inputActions.Factory.Select.WasPressedThisFrame() && inputActions.Factory.MultiBuild.IsPressed())
        {
            anchorA = WorldToCell(GetMouseWorld());
            selectingRectangle = true;
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
            anchorA = WorldToCell(GetMouseWorld());
            ToggleCell(anchorA, true); // true = режим удаления
        }
        else if (inputActions.Factory.Delete.IsPressed() && !inputActions.Factory.MultiBuild.IsPressed())
        {
            // Непрерывное удаление при зажатии (кисть)
            Vector3Int currentCell = WorldToCell(GetMouseWorld());
            if (!selectedCells.Contains(currentCell))
            {
                ToggleCell(currentCell, true);
            }
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
            foreach (var item in clipboard)
            {
                Vector3Int targetCell = currentCenter + item.offset;
                if (!GridManager.Instance.IsOccupied(targetCell) && !plannedBuilds.ContainsKey(targetCell))
                {
                    plannedBuilds[targetCell] = new PlannedBuild(item.type, item.rotation);
                    CreateBuildGhost(targetCell, item.type, item.rotation);
                }
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

        selectedCells.Add(cell);

        if (deleteMode)
        {
            plannedDeletes.Add(cell);
            CreateDeleteMarker(cell);
            return;
        }

        if (!GridManager.Instance.IsOccupied(cell))
        {
            plannedBuilds[cell] =
                new PlannedBuild(
                    selectedBuilding,
                    0
                );

            CreateBuildGhost(
                cell,
                selectedBuilding,
                0
            );
        }
        else
        {
            // Подсветка уже существующих зданий зелёным при выделении
            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
            if (building != null)
            {
                if (!rotationGhosts.TryGetValue(cell, out var ghost))
                {
                    ghost = new GameObject($"GhostRotation_{building.name}");
                    ghost.transform.position = building.transform.position; // Использовать позицию самого здания
                    CopySprites(building.gameObject, ghost);
                    rotationGhosts[cell] = ghost;
                }
                ghost.transform.rotation = Quaternion.Euler(0, 0, Mathf.RoundToInt(building.transform.eulerAngles.z));
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
            else
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

            copy.color = new Color(0, 1, 0, 0.5f);
        }
    }
    #endregion
}



