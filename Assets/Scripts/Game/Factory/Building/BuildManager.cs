using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    #region ??????????
    public Camera cam;

    

    [System.Serializable]
    private class PlannedBuild
    {
        public BuildingData type;
        public int rotation;
        public PlannedBuild(BuildingData type, int rotation)
        {
            this.type = type;
            this.rotation = rotation;
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
    private Dictionary<Vector3Int, int> plannedRotations = new();
    private Dictionary<Vector3Int, GameObject> deleteMarkers = new();

    private bool selectingRectangle;
    private Vector3Int anchorA;
    #endregion
    private void Start()
    {
        grid = FindFirstObjectByType<Grid>();

        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            buildMode = !buildMode;
            ClearAll();
            Debug.Log($"Build mode: {buildMode}");
        }

        if (!buildMode)
            return;

        HandleBuildingSelection();
        HandleSelection();
        HandleRotation();

        if (Input.GetKeyDown(KeyCode.C))
            ApplyChanges();
    }
    #region ??????? ?????????????
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

    void HandleSelection()
    {
        bool deleteMode = Input.GetKey(KeyCode.R);

        if (Input.GetMouseButtonDown(0))
        {
            anchorA = WorldToCell(GetMouseWorld());

            if (Input.GetKey(KeyCode.LeftShift))
            {
                selectingRectangle = true;
            }
            else
            {
                ToggleCell(anchorA, deleteMode);
            }
        }

        if (Input.GetMouseButtonUp(0) && selectingRectangle)
        {
            selectingRectangle = false;

            Vector3Int anchorB =
                WorldToCell(GetMouseWorld());

            SelectRectangle(anchorA, anchorB, deleteMode);
        }
    }

    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            RotateSelected(90);

        if (Input.GetKeyDown(KeyCode.E))
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

            GameObject prefab = GetPrefab(pair.Value.type);
            if (prefab == null) continue;

            GameObject go = Instantiate(prefab,
                CellToWorld(pair.Key),
                Quaternion.Euler(0, 0, pair.Value.rotation));
                
            if (go.TryGetComponent<FactoryBlock>(out var block))
            {
                block.OnPlaced();
            }

            changed.Add(pair.Key);
        }

        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }

        ClearAll();
    }
    #endregion

    #region Grid Math
    Vector3Int WorldToCell(Vector3 pos)
    {
        return grid.WorldToCell(pos + new Vector3(0.5f, 0.5f, 0f));
    }

    Vector3 GetMouseWorld()
    {
        Ray ray =
            cam.ScreenPointToRay(
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
    void ToggleCell(Vector3Int cell, bool deleteMode)
    {
        if (selectedCells.Contains(cell))
        {
            selectedCells.Remove(cell);

            plannedBuilds.Remove(cell);
            plannedDeletes.Remove(cell);

            RemoveBuildGhost(cell);
            RemoveDeleteMarker(cell);

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
                        ghost.transform.position = CellToWorld(cell);
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
        return grid.CellToWorld(cell);
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

        plannedBuilds.Clear();
        plannedDeletes.Clear();
        selectedCells.Clear();
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
            child.transform.localPosition = renderer.transform.localPosition;
            child.transform.localRotation = renderer.transform.localRotation;
            child.transform.localScale = renderer.transform.localScale;

            SpriteRenderer copy = child.AddComponent<SpriteRenderer>();
            copy.sprite = renderer.sprite;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;

            copy.color = new Color(0, 1, 0, 0.5f);
        }
    }
    #endregion
}



