using System.Collections.Generic;
using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    public Camera cam;

    [System.Serializable]
    public class BuildEntry
    {
        public BuildType type;
        public GameObject prefab;
    }

    public List<BuildEntry> prefabs = new();

    private BuildType selected;

    private void Update()
    {
        HandleSelection();
        HandlePlacement();
    }

    void HandleSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            selected = BuildType.Conveyor;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            selected = BuildType.Factory;

        if (Input.GetKeyDown(KeyCode.Alpha3))
            selected = BuildType.Storage;
    }

    void HandlePlacement()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 world = GetMouseWorld();
        Vector3Int cell = WorldToCell(world);

        if (GridManager.Instance.GetReceiver(cell) != null)
            return;

        GameObject prefab = GetPrefab(selected);
        if (prefab == null)
            return;

        Instantiate(prefab, CellToWorld(cell), Quaternion.identity);
        GridManager.Instance.NotifyNeighbours(cell);
    }

    GameObject GetPrefab(BuildType type)
    {
        foreach (var p in prefabs)
        {
            if (p.type == type)
                return p.prefab;
        }
        return null;
    }

    Vector3 GetMouseWorld()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist) + new Vector3(0.5f, 0.5f, 0);

        return Vector3.zero;
    }

    Vector3Int WorldToCell(Vector3 pos)
    {
        Grid grid = FindFirstObjectByType<Grid>();
        return grid.WorldToCell(pos);
    }

    Vector3 CellToWorld(Vector3Int cell)
    {
        Grid grid = FindFirstObjectByType<Grid>();
        return grid.CellToWorld(cell);
    }
}

public enum BuildType
{
    Conveyor,
    Factory,
    Storage
}