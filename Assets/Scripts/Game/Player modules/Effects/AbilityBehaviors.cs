using System;
using UnityEngine;

[Serializable]
public class HealBehavior : IAbilityBehavior
{
    [SerializeField] private float healAmount = 25f;

    public void Execute(PlayerController player)
    {
        player.Heal(healAmount);
    }
}

[Serializable]
public class DashBehavior : IAbilityBehavior
{
    [SerializeField] private float dashForce = 15f;

    public void Execute(PlayerController player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        PlayerHull hull = player.GetComponentInChildren<PlayerHull>();
        if (rb != null && hull != null)
        {
            rb.AddForce(hull.MoveInput * dashForce, ForceMode2D.Impulse);
        }
    }
}
