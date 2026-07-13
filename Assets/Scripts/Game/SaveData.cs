using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaveData
{
    [System.Serializable]
    public class PlayerInventoryData
    {
        public Inventory foragingInventory;
        public Inventory factoryInventory;
    }

    [System.Serializable]
    public class UpgradeSaveData
    {
        public string statType;
        public int curLevel;
    }

    [System.Serializable]
    public class PlayerUpgradesData
    {
        public List<UpgradeSaveData> upgrades = new List<UpgradeSaveData>();
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public string blockId;
        public Vector3Int position;
        public int zRotation;
        public string customDataJson;
    }

    [System.Serializable]
    public class FactoryData
    {
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    }

    [System.Serializable]
    public class TechTreeData
    {
        public List<string> unlockedNodeIds = new List<string>();
    }
}
