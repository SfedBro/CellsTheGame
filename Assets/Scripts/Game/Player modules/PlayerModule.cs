using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewModule", menuName = "Module/Module")]
public class PlayerModule : ScriptableObject
{
    #region fields

    [Header("UI")]
    public Sprite sprite;
    public string title;

    [Header("Type")]
    public ModuleType moduleType;

    [Header("Conflicts")]
    public ModuleConflict conflictGroup;

    [Header("References")]
    public PlayerModuleController controller;

    #endregion


    #region charge

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
            controller.DisableModule(this);
        }
    }

    public void Recharge()
    {
        curCharge = maxCharge;
    }

    #endregion
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
    int ActivateAttack();
    void AttackEnd();
}


public interface IModuleUseE
{
    void Use(PlayerController player);
}


public interface IModuleStat
{
    PlayerStats GetAddChanges();
    PlayerStats GetMultCganges();
}