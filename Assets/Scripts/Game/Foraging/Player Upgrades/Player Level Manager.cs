using UnityEngine;
using System.Collections.Generic;

public class PlayerLevelManager : MonoBehaviour, IGameService
{
    private static PlayerLevelManager _instance;
    public static PlayerLevelManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerLevelManager>();
                if (_instance != null)
                {
                    _instance.InitializeService();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private List<UpgradeData> upgradeTemplates;
    private List<UpgradeData> playerUpgrades = new();
    public int curPlayerLevel = 0;
    public int curExperience = 0;
    public int curUpgradePoints = 0;

    public void InitializeService()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerUpgrades.Count > 0) return;

        foreach (UpgradeData data in upgradeTemplates)
        {
            UpgradeData copy = ScriptableObject.CreateInstance<UpgradeData>();
            string json = JsonUtility.ToJson(data);
            JsonUtility.FromJsonOverwrite(json, copy);
            playerUpgrades.Add(copy);
        }
    }

    public List<UpgradeData> getPlayerUpgrades() => playerUpgrades;

    public void StartService()
    {
        Load();
    }

    private string saveKey = "PlayerUpgradesSave";

    private SaveData.PlayerUpgradesData GetSaveSnapshot()
    {
        var data = new SaveData.PlayerUpgradesData();
        foreach (var upg in playerUpgrades)
        {
            data.upgrades.Add(new SaveData.UpgradeSaveData
            {
                statType = upg.GetStatType().ToString(),
                curLevel = upg.curLevel
            });
        }
        return data;
    }

    public void Save()
    {
        SaveManager.Save(saveKey, GetSaveSnapshot());
        Debug.Log("Saved Player Upgrades");
    }

    public void Load()
    {
        var data = SaveManager.Load<SaveData.PlayerUpgradesData>(saveKey);
        if (data != null && data.upgrades != null)
        {
            foreach (var upgSave in data.upgrades)
            {
                var match = playerUpgrades.Find(u => u.GetStatType().ToString() == upgSave.statType);
                if (match != null)
                {
                    match.curLevel = upgSave.curLevel;
                }
            }

            UpgradingManager um = FindFirstObjectByType<UpgradingManager>();
            if (um != null)
            {
                um.ForceUpdateAfterLoad();
            }
        }
        Debug.Log("Loaded Player Upgrades");
    }
}
