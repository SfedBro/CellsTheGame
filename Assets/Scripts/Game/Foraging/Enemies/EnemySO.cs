using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newEnemyStat", menuName = "Enemy/Enemy stats")]
public class EnemySO : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private int maxHP;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int DMG;

    [Header("Loot")]
    [SerializeField] private List<ItemType> types;
    [SerializeField] private List<LootTable> tables;

    public int GetHP() => maxHP;
    public float GetSpeed() => moveSpeed;
    public int getDMG() => DMG;

    public List<LootAmount> generateLoot()
    {
        List<LootAmount> result = new();
        for (int i = 0; i < tables.Count; i++)
        {
            result.Add(new LootAmount(types[i], tables[i].GenerateLoot()));
        }

        return result;
    }
}

[System.Serializable]
public class LootTable
{
    [SerializeField] private int defaultAmount;
    [SerializeField] private List<LootChanceAmount> table;
    [SerializeField] private int totalRareness;

    public int GenerateLoot()
    {
        int value = Random.Range(1, totalRareness);
        foreach (LootChanceAmount l in table)
        {
            value -= l.GetRareness();

            if (value <= 0)
            {
                return l.GetAmount();
            }
        }

        return defaultAmount;
    }
}

[System.Serializable]
public class LootChanceAmount
{
    [SerializeField] private int amount;
    [SerializeField] private int rareness;

    public int GetAmount() => amount;
    public int GetRareness() => rareness;
}

[System.Serializable]
public class LootAmount
{
    [SerializeField] private ItemType type;
    [SerializeField] private int amount;

    public ItemType GetItemType() => type;
    public int GetAmount() => amount;

    public LootAmount(ItemType t, int a)
    {
        type = t;
        amount = a;
    }
}
