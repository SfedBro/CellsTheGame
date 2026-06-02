using UnityEngine;
using TMPro;

public class InventoryManagement : MonoBehaviour
{
    [Header("Counters")]
    public TextMeshProUGUI res1ctr;
    public TextMeshProUGUI res2ctr;

    private ResourcesManager rm = ResourcesManager.instance;

    public void Update()
    {
        // updateCounters();
    }

    private void updateCounters()
    {
        res1ctr.text = rm.getResourceAmount(ItemType.TestOre).ToString();
        res2ctr.text = rm.getResourceAmount(ItemType.TestPlate).ToString();
    }

    public int getResourceAmount(ItemType t)
    {
        return rm.getResourceAmount(t);
    }

    public void addRes(ItemType t, int amount)
    {
        rm.addResourceAmount(t, amount);
    }

    public void onPlayerDeath()
    {
        int r1 = rm.getResourceAmount(ItemType.TestOre) / -2;
        addRes(ItemType.TestOre, r1);

        int r2 = rm.getResourceAmount(ItemType.TestPlate) / -2;
        addRes(ItemType.TestPlate, r2);
    }
}