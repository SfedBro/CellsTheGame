using UnityEngine;

public static class BuildGridUtils
{
    public static Vector3Int WorldToCell(BuildManager bm, Vector3 pos)
    {
        return bm.Grid.WorldToCell(pos + new Vector3(0.5f, 0.5f, 0f));
    }

    public static Vector3 CellToWorld(BuildManager bm, Vector3Int cell)
    {
        return bm.Grid.CellToWorld(cell);
    }

    public static Vector3 GetCellCenterWorld(BuildManager bm, Vector3Int cell)
    {
        return bm.Grid.GetCellCenterWorld(cell);
    }

    public static Vector3 GetMouseWorld(BuildManager bm)
    {
        Ray ray = bm.cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
    
    public static bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}
