using UnityEngine;

[CreateAssetMenu(fileName = "New Building Data", menuName = "Factory/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public Sprite icon;
    public GameObject prefab;
    public System.Collections.Generic.List<ItemStack> cost = new();
}
