using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public Camera cam;

    [System.Serializable]
    public class BuildEntry
    {
        public Building type;
        public GameObject prefab;
    }

    [System.Serializable]
    private class PlannedBuild
    {
        public Building type;
        public int rotation;
        public PlannedBuild(Building type, int rotation)
        {
            this.type = type;
            this.rotation = rotation;
        }
    }

    public List<BuildEntry> prefabs = new();

    public GameObject deleteMarkerPrefab;
    [SerializeField]
    private bool buildMode = false;
    private Grid grid;

    private Building selectedBuilding = Building.Conveyor;

    private HashSet<Vector3Int> selectedCells = new();

    private Dictionary<Vector3Int, PlannedBuild> plannedBuilds = new();
    private HashSet<Vector3Int> plannedDeletes = new();

    private Dictionary<Vector3Int, GameObject> buildGhosts = new();
    private Dictionary<Vector3Int, GameObject> deleteMarkers = new();

    private bool selectingRectangle;
    private Vector3Int anchorA;

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

    void HandleBuildingSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedBuilding = Building.Conveyor;
            Debug.Log("Selected: Conveyor");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedBuilding = Building.Factory;
            Debug.Log("Selected: Factory");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedBuilding = Building.Storage;
            Debug.Log("Selected: Storage");
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
        HashSet<Vector3Int> changed =
            new HashSet<Vector3Int>();

        foreach (var cell in plannedDeletes)
        {
            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);

            if (building == null)
                continue;

            GridManager.Instance.Unregister(cell);

            Destroy(building.gameObject);

            changed.Add(cell);
        }

        foreach (var pair in plannedBuilds)
        {
            if (GridManager.Instance.IsOccupied(pair.Key))
                continue;

            GameObject prefab = GetPrefab(pair.Value.type);

            if (prefab == null)
                continue;

            Instantiate(prefab,
                CellToWorld(pair.Key),
                Quaternion.Euler(0, 0, pair.Value.rotation));

            changed.Add(pair.Key);
        }

        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }

        ClearAll();
    }

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

    void ToggleCell(Vector3Int cell, bool deleteMode)
    {
        if (!deleteMode && GridManager.Instance.IsOccupied(cell))
            return;

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
        }
        else
        {
            plannedBuilds[cell] = new PlannedBuild(selectedBuilding, 0);
            CreateBuildGhost(cell, selectedBuilding, 0);
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

            MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
            if (building != null)
            {
                building.transform.Rotate(0, 0, angle);
                changed.Add(cell);
            }
        }

        foreach (var cell in changed)
        {
            GridManager.Instance.NotifyNeighbours(cell);
        }
    }

    GameObject GetPrefab(Building type)
    {
        foreach (var entry in prefabs)
        {
            if (entry.type == type)
                return entry.prefab;
        }

        return null;
    }

    Vector3 CellToWorld(Vector3Int cell)
    {
        return grid.CellToWorld(cell);
    }

    void ClearAll()
    {
        foreach (var ghost in buildGhosts.Values)
            Destroy(ghost);
        buildGhosts.Clear();

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

    void CreateBuildGhost(Vector3Int cell, Building type, int rotation)
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

            Color color = renderer.color;
            color.a = 0.5f;
            copy.color = color;
        }
    }
}

public enum Building
{
    Conveyor,
    Factory,
    Storage
}