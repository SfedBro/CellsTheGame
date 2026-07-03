using System.Collections.Generic;
using UnityEngine;


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


    public enum EditInteractionState
    {
        None,
        PaintingSelection,
        PaintingDeletion,
        MovingSelection,
        SelectingRectangle
    }

public class EditAction
{
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, PlannedBuild> builtCells = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, PlannedBuild>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, PlannedBuild> deletedBuildings = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, PlannedBuild>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int> originalRotations = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
    public System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int> newRotations = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
}
