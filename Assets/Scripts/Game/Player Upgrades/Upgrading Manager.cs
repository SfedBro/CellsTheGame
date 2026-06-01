using System.Collections.Generic;
using UnityEngine;

public class UpgradingManager : MonoBehaviour
{
    public static UpgradingManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField] List<UpgradeData> upgrades;

    public void correctPlayerStats(PlayerConfig player)
    {
        foreach (var i in upgrades)
        {
            player.upgradeStat(i.GetStatType(), i.getCurLevelValue());
        }
    }
}
