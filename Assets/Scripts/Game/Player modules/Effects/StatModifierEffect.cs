using System;
using UnityEngine;

[Serializable]
public class StatModifierEffect : IModuleEffect
{
    [SerializeField] private PlayerStatType statType;
    [SerializeField] private ModifierType modifierType;
    [SerializeField] private float amount;

    private StatModifier modifier;

    public void Apply(PlayerController player)
    {
        modifier = new StatModifier(amount, modifierType, statType, this);
        player.AddModifier(modifier);
    }

    public void Remove(PlayerController player)
    {
        player.RemoveModifiersFromSource(this);
    }
}
