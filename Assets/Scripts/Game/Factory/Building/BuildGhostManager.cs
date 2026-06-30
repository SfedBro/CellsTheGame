using System.Collections.Generic;
using UnityEngine;

public class BuildGhostManager : MonoBehaviour, IGameService
{
    public static BuildGhostManager Instance { get; private set; }

    private Dictionary<Vector3Int, GameObject> buildGhosts = new();
    private Dictionary<Vector3Int, GameObject> rotationGhosts = new();
    private Dictionary<Vector3Int, GameObject> deleteMarkers = new();
    private Dictionary<Vector3Int, GameObject> pasteGhosts = new();

    public GameObject deleteMarkerPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeService() {}
    public void StartService() {}

    public void CreateBuildGhost(Vector3Int cell, BuildingData type, int rotation)
    {
        if (buildGhosts.ContainsKey(cell)) RemoveBuildGhost(cell);

        GameObject ghost = new GameObject($"Ghost_{type.buildingName}");
        ghost.transform.position = BuildManager.Instance.CellToWorld(cell);
        ghost.transform.rotation = Quaternion.Euler(0, 0, rotation);

        GameObject prefab = BuildManager.Instance.GetPrefab(type);
        if (prefab != null) CopySprites(prefab, ghost);

        SpriteRenderer[] renderers = ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.color = new Color(0, 1, 0, 0.5f);
        }

        buildGhosts[cell] = ghost;
    }

    public void RemoveBuildGhost(Vector3Int cell)
    {
        if (buildGhosts.TryGetValue(cell, out var ghost))
        {
            Destroy(ghost);
            buildGhosts.Remove(cell);
        }
    }

    public void CreateDeleteMarker(Vector3Int cell)
    {
        if (deleteMarkers.ContainsKey(cell)) return;

        if (deleteMarkerPrefab != null)
        {
            GameObject marker = Instantiate(deleteMarkerPrefab, BuildManager.Instance.CellToWorld(cell), Quaternion.identity);
            deleteMarkers[cell] = marker;
        }
    }

    public void RemoveDeleteMarker(Vector3Int cell)
    {
        if (deleteMarkers.TryGetValue(cell, out var marker))
        {
            Destroy(marker);
            deleteMarkers.Remove(cell);
        }
    }

    public void CreatePasteGhost(Vector3Int cell, BuildingData type, int rotation)
    {
        GameObject ghost = new GameObject($"PasteGhost_{type.buildingName}");
        ghost.transform.position = BuildManager.Instance.CellToWorld(cell);
        ghost.transform.rotation = Quaternion.Euler(0, 0, rotation);

        GameObject prefab = BuildManager.Instance.GetPrefab(type);
        if (prefab != null) CopySprites(prefab, ghost);

        SpriteRenderer[] renderers = ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.color = new Color(0, 0.5f, 1f, 0.5f);

        pasteGhosts[cell] = ghost;
    }

    public void ClearPasteGhosts()
    {
        foreach (var ghost in pasteGhosts.Values) Destroy(ghost);
        pasteGhosts.Clear();
    }

    public void CopySprites(GameObject source, GameObject target)
    {
        SpriteRenderer[] sourceRenderers = source.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sourceRenderers)
        {
            GameObject child = new GameObject(sr.gameObject.name);
            child.transform.SetParent(target.transform);
            child.transform.localPosition = sr.transform.localPosition;
            child.transform.localRotation = sr.transform.localRotation;
            child.transform.localScale = sr.transform.localScale;

            SpriteRenderer newSr = child.AddComponent<SpriteRenderer>();
            newSr.sprite = sr.sprite;
            newSr.color = new Color(1, 1, 1, 0.5f);
            newSr.sortingLayerID = sr.sortingLayerID;
            newSr.sortingOrder = sr.sortingOrder + 5;
        }
    }

    public void ClearAllGhosts()
    {
        foreach (var ghost in buildGhosts.Values) Destroy(ghost);
        buildGhosts.Clear();

        foreach (var marker in deleteMarkers.Values) Destroy(marker);
        deleteMarkers.Clear();

        foreach (var ghost in rotationGhosts.Values) Destroy(ghost);
        rotationGhosts.Clear();

        ClearPasteGhosts();
    }

    public void SetRotationGhost(Vector3Int cell, GameObject ghost)
    {
        rotationGhosts[cell] = ghost;
    }
    
    public bool HasRotationGhost(Vector3Int cell) => rotationGhosts.ContainsKey(cell);
    public GameObject GetRotationGhost(Vector3Int cell) => rotationGhosts.GetValueOrDefault(cell);

    public void RemoveRotationGhost(Vector3Int cell)
    {
        if (rotationGhosts.TryGetValue(cell, out var rGhost))
        {
            Destroy(rGhost);
            rotationGhosts.Remove(cell);
        }
    }
    
    public void AddBuildGhost(Vector3Int cell, GameObject ghost)
    {
        buildGhosts[cell] = ghost;
    }
    
    public bool HasBuildGhost(Vector3Int cell) => buildGhosts.ContainsKey(cell);
    public GameObject GetBuildGhost(Vector3Int cell) => buildGhosts.GetValueOrDefault(cell);
}
