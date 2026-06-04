using UnityEngine;
using System.Collections.Generic;

public class PlayerLevelManager : MonoBehaviour
{
    public static PlayerLevelManager instance;
    [SerializeField] private List<UpgradeData> upgradeTemplates;
    private List<UpgradeData> playerUpgrades = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
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
}
