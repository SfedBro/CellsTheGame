using System;
using UnityEngine;

[Serializable]
public class LaserWeaponEffect : IModuleEffect, IWeapon
{
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private PlayerModule parentModule;
    [SerializeField] private int spendChargePerTick = 1;

    private GameObject lastAttackObject;
    private Laser lastAttack;

    public float CoolDownMultiplier => 1f;

    public void Apply(PlayerController player)
    {
        player.CurrentWeapon = this;
    }

    public void Remove(PlayerController player)
    {
        if (player.CurrentWeapon == this)
        {
            EndAttack(player);
            player.CurrentWeapon = player.DefaultWeapon;
        }
    }

    public void StartAttack(PlayerController player, float angle)
    {
        if (lastAttackObject != null) return;

        Transform gunTransform = player.GetComponentInChildren<PlayerGun>()?.transform;
        Transform spawnParent = gunTransform != null ? gunTransform : player.transform;

        lastAttackObject = UnityEngine.Object.Instantiate(attackPrefab, spawnParent.position, Quaternion.identity);
        lastAttack = lastAttackObject.GetComponent<Laser>();
        if (lastAttack != null)
        {
            lastAttack.Initialize(player.attackData, spawnParent);
        }
    }

    public void AttackTick(PlayerController player)
    {
        if (lastAttack != null)
        {
            lastAttack.ActivateAttack();

            if (parentModule != null)
            {
                parentModule.SpendCharges(spendChargePerTick);
            }
        }
    }

    public void EndAttack(PlayerController player)
    {
        if (lastAttackObject != null)
        {
            UnityEngine.Object.Destroy(lastAttackObject);
            lastAttackObject = null;
            lastAttack = null;
        }
    }
}
