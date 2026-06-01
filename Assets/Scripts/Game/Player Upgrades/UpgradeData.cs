using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrade System/Upgrade Definition")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private StatType stat;
    [SerializeField] private List<UpgradeLevel> costPerLevel;
    [SerializeField] private List<float> valuePerLevel;
    public int maxLevel;
    public int curLevel;

    public StatType GetStatType() => stat;

    public List<ResourceCost> getCurCost() => costPerLevel[curLevel].costs;

    public float getCurLevelValue() => valuePerLevel[curLevel];
}

public enum StatType { Speed, Health }

[System.Serializable]
public class UpgradeLevel
{
    public List<ResourceCost> costs;
}

[System.Serializable]
public class ResourceCost
{
    public ItemType resource;
    public int amount;
}