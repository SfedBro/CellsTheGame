using System.Collections.Generic;
using UnityEngine;

public class ClipboardManager : MonoBehaviour
{
    public static ClipboardManager Instance { get; private set; }

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

    private List<ClipboardItem> clipboard = new List<ClipboardItem>();
    public bool isPasteMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Copy(HashSet<Vector3Int> selectedCells)
    {
        if (selectedCells.Count == 0) return;

        clipboard.Clear();
        Vector3Int center = GetCenterOfSelection(selectedCells);

        foreach (var cell in selectedCells)
        {
            BuildingData typeToCopy = null;
            int rotToCopy = 0;

            if (BuildManager.Instance.plannedBuilds.TryGetValue(cell, out var build))
            {
                typeToCopy = build.type;
                rotToCopy = build.rotation;
            }
            else
            {
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                if (building is FactoryBlock block)
                {
                    typeToCopy = BuildManager.Instance.availableBuildings.Find(b => b.buildingName == block.blockId);
                    rotToCopy = Mathf.RoundToInt(block.transform.eulerAngles.z);
                    
                    if (BuildManager.Instance.plannedRotations.TryGetValue(cell, out int pRot))
                        rotToCopy = pRot;
                }
            }

            if (typeToCopy != null)
            {
                Vector3Int offset = cell - center;
                clipboard.Add(new ClipboardItem(typeToCopy, rotToCopy, offset));
            }
        }
        
        Debug.Log($"Copied {clipboard.Count} buildings to clipboard.");
    }

    public void Cut(HashSet<Vector3Int> selectedCells)
    {
        Copy(selectedCells);
    }

    public List<ClipboardItem> GetClipboard()
    {
        return clipboard;
    }

    public void RotateClipboard(int angle, bool rotateMatrix)
    {
        for (int i = 0; i < clipboard.Count; i++)
        {
            var item = clipboard[i];
            item.rotation += angle;

            if (rotateMatrix)
            {
                int newX = item.offset.x;
                int newY = item.offset.y;
                if (angle == 90 || angle == -270)
                {
                    newX = -item.offset.y;
                    newY = item.offset.x;
                }
                else if (angle == -90 || angle == 270)
                {
                    newX = item.offset.y;
                    newY = -item.offset.x;
                }
                else if (angle == 180 || angle == -180)
                {
                    newX = -item.offset.x;
                    newY = -item.offset.y;
                }
                item.offset = new Vector3Int(newX, newY, 0);
            }
            
            item.rotation = ((item.rotation % 360) + 360) % 360;
            clipboard[i] = item;
        }
    }

    public Vector3Int GetCenterOfSelection(HashSet<Vector3Int> cells)
    {
        if (cells.Count == 0) return Vector3Int.zero;

        Vector3 sum = Vector3.zero;
        foreach (var cell in cells) sum += cell;
        
        sum /= cells.Count;
        return new Vector3Int(Mathf.RoundToInt(sum.x), Mathf.RoundToInt(sum.y), 0);
    }
}
