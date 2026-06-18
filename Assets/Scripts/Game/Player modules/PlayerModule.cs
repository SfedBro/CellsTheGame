using UnityEngine;

[CreateAssetMenu(fileName = "NewModule", menuName = "Module/Module")]
public class PlayerModule : ScriptableObject
{
    [Header("Type")]
    public ModuleType moduleType;

    [Header("Conflicts")]
    public ModuleConflict conflictGroup;

    [Header("References")]
    public PlayerModuleController controller;

    [Header("Charge")]
    public bool isChargable;
    [SerializeField] private int maxCharge;
    public int curCharge;

    public void SpendCharges(int amount)
    {
        if (!isChargable) return;

        curCharge -= amount;

        if (curCharge < 0)
        {
            controller.Recharge(this);
        }
    }

    public void Recharge()
    {
        curCharge = maxCharge;
    }

    public virtual void Disable() {}

    public virtual void Enable() {}
}


public enum ModuleType
{
    Cannon,
    Body,
    Move
}

public enum ModuleConflict
{
    None,
    LBM,
    E
}


public interface IModuleCannon
{
    int AttackStart(GameObject attackPrefab, AttackData data, Transform parent, float rotation);
    float GetNextHit();
    void SetNextHit(float next);
}
