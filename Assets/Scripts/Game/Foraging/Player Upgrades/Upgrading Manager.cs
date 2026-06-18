using System.Collections.Generic;
using UnityEngine;

public class UpgradingManager : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Transform UIParent;
    [SerializeField] private GameObject UIPrefab;
    [SerializeField] private int yOffset = 105;

    [Header("Dependencies")]
    [SerializeField] private PlayerExperienceManager pem;

    private PlayerController player;
    private ResourcesManager rm = ResourcesManager.instance;
    private List<IncrementInterface> increments = new();

    private List<UpgradeData> upgrades;

    void Awake()
    {
        upgrades = PlayerLevelManager.instance.getPlayerUpgrades();
    }

    void OnEnable()
    {
        rm.Subscrive(UpdateCounters);
        pem.Subscribe(UpdateCounters);
    }

     void Start()
    {
        Vector3 down = new Vector3(0, yOffset, 0);
        int i = 0;
        foreach (UpgradeData upgrade in upgrades)
        {
            GameObject upg = Instantiate(UIPrefab);
            upg.transform.SetParent(UIParent, false);
            upg.transform.position += i * down;
            i++;
            IncrementInterface increment = upg.GetComponent<IncrementInterface>();
            increment.upgradeData = upgrade;
            increment.upgrade = tryUpgrade;
            increments.Add(increment);
            increment.UpdateUI();
        }
    }

    void OnDisable()
    {
        rm.Unsubscrive(UpdateCounters);
    }

    public void correctPlayerStats(PlayerController player)
    {
        this.player = player;
        foreach (var i in upgrades)
        {
            player.UpgradeStat(i.GetStatType(), i.getCurLevelValue());
        }
    }

    private bool tryUpgrade(UpgradeData upgrade)
    {
        foreach (ResourceCost cost in upgrade.getCurCost())
        {
            if (cost.resource == ItemType.Default)
            {
                if (pem.GetUpgradePoints() < cost.amount) return false;
                continue;
            }

            if (rm.getResourceAmount(cost.resource) < cost.amount) return false;
        }

        foreach (ResourceCost cost in upgrade.getCurCost())
        {
            if (cost.resource == ItemType.Default)
            {
                pem.RemoveUpgradePoints(cost.amount);
                continue;
            }

            rm.addResourceAmount(cost.resource, -cost.amount);
        }
    
        upgrade.curLevel++;
        player.UpgradeStat(upgrade.GetStatType(), upgrade.getCurLevelValue());
        return true;
    }

    private void UpdateCounters(ItemType type, int amount)
    {
        foreach (IncrementInterface inc in increments)
        {
            inc.UpdateRequirements(type, amount);
        }
    }

    public void onPlayerDeath()
    {
        foreach (UpgradeData data in upgrades)
        {
            data.curLevel = 1;
        }

        foreach (IncrementInterface inc in increments)
        {
            inc.UpdateUI();
        }
    }

    public void ForceUpdateAfterLoad()
    {
        if (player != null)
        {
            // Reset base stats or just re-apply? 
            // Wait, correctPlayerStats re-applies current level.
            // If player stats don't reset automatically, this might add on top.
            // Assuming player starts fresh on load or we just set it.
            // For now, call correctPlayerStats.
            correctPlayerStats(player);
        }

        foreach (IncrementInterface inc in increments)
        {
            inc.UpdateUI();
        }
    }
}
