using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActiveAbilityEffect : IModuleEffect
{
    [SerializeField] private float coolDown = 5f;
    [SerializeField] private PlayerModule parentModule;
    
    [SerializeReference]
    public List<IAbilityBehavior> behaviors = new List<IAbilityBehavior>();

    private float nextUse = 0f;

    public void Apply(PlayerController player)
    {
        player.activeAbilityE = Trigger;
    }

    public void Remove(PlayerController player)
    {
        if (player.activeAbilityE == Trigger)
        {
            player.activeAbilityE = null;
        }
    }

    private void Trigger(PlayerController player)
    {
        if (Time.time < nextUse) return;
        nextUse = Time.time + coolDown;

        if (behaviors != null)
        {
            foreach (var behavior in behaviors)
            {
                behavior?.Execute(player);
            }
        }

        if (parentModule != null)
        {
            parentModule.SpendCharges(1);
        }
    }
}
