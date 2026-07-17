using System;
using UnityEngine;

[Serializable]
public class WeaponEffect : IModuleEffect, IWeapon
{
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float shootCoolDownMultiplier = 1f;
    [SerializeField] private int shootCost = 1;
    [SerializeField] private PlayerModule parentModule; // Link to spend charges if needed

    public float CoolDownMultiplier => shootCoolDownMultiplier;

    public void Apply(PlayerController player)
    {
        player.CurrentWeapon = this;
    }

    public void Remove(PlayerController player)
    {
        if (player.CurrentWeapon == this)
        {
            player.CurrentWeapon = player.DefaultWeapon;
        }
    }

    public void StartAttack(PlayerController player, float angle)
    {
    }

    public void AttackTick(PlayerController player)
    {
        Fire(player);
    }

    public void EndAttack(PlayerController player)
    {
    }

    private void Fire(PlayerController player)
    {
        Transform gunTransform = player.GetComponentInChildren<PlayerGun>()?.transform;
        Transform parentTransform = gunTransform != null ? gunTransform : player.transform;
        Quaternion rotation = parentTransform.rotation;

        PlayerWeaponVisuals visuals = player.GetComponentInChildren<PlayerWeaponVisuals>();
        if (visuals != null && visuals.MuzzleOffsets.Count > 0)
        {
            foreach (Vector3 muzzlePos in visuals.GetMuzzleWorldPositions())
            {
                GameObject bullet = UnityEngine.Object.Instantiate(attackPrefab, muzzlePos, rotation);
                IAttack attack = bullet.GetComponent<IAttack>();
                if (attack != null)
                {
                    attack.Initialize(player.attackData, parentTransform);
                }
            }
        }
        else
        {
            GameObject bullet = UnityEngine.Object.Instantiate(attackPrefab, player.transform.position, rotation);
            IAttack attack = bullet.GetComponent<IAttack>();
            if (attack != null)
            {
                attack.Initialize(player.attackData, parentTransform);
            }
        }

        if (parentModule != null)
        {
            parentModule.SpendCharges(shootCost);
        }
    }
}
