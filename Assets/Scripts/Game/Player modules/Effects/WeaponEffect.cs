using System;
using UnityEngine;

[Serializable]
public class WeaponEffect : IModuleEffect, IWeapon
{
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float shootCoolDownMultiplier = 1f;
    [SerializeField] private int shootCost = 1;
    [SerializeField] private PlayerModule parentModule; // Link to spend charges if needed

    private float nextShootTime = 0f;

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
        if (Time.time < nextShootTime) return;
        nextShootTime = Time.time + player.curPlayerStats.attackCoolDown * shootCoolDownMultiplier;

        PlayerWeaponVisuals visuals = player.GetComponentInChildren<PlayerWeaponVisuals>();
        if (visuals != null && visuals.MuzzleOffsets.Count > 0)
        {
            foreach (Vector3 muzzlePos in visuals.GetMuzzleWorldPositions())
            {
                GameObject bullet = UnityEngine.Object.Instantiate(attackPrefab, muzzlePos, Quaternion.identity);
                IAttack attack = bullet.GetComponent<IAttack>();
                attack.Initialize(player.attackData, player.transform, angle);
            }
        }
        else
        {
            GameObject bullet = UnityEngine.Object.Instantiate(attackPrefab, player.transform.position, Quaternion.identity);
            IAttack attack = bullet.GetComponent<IAttack>();
            attack.Initialize(player.attackData, player.transform, angle);
        }

        if (parentModule != null)
        {
            parentModule.SpendCharges(shootCost);
        }
    }

    public void EndAttack(PlayerController player)
    {
    }
}
