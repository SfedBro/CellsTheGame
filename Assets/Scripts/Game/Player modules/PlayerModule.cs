using System;
using System.Collections.Generic;
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
    public int inventorySize; // For Body modules
    public int stackCapacity; // For Move modules

    [Header("Conflicts")]
    public ModuleConflict conflictGroup;

    [Header("References")]
    public PlayerModuleController controller;

    [Header("Visual Customization (Optional)")]
    public Sprite overrideWeaponSprite;   // Overrides gun visual
    public Sprite overrideHullSprite;     // Overrides hull visual
    public Vector2[] muzzleOffsets;       // Local muzzle positions relative to gun pivot
    public Sprite muzzleModifierSprite;   // Stacking modifier sprites for muzzles

    [Header("Polymorphic Effects")]
    [SerializeReference]
    public List<IModuleEffect> effects = new List<IModuleEffect>();

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

        if (curCharge < 0 && controller != null)
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