using UnityEngine;

[CreateAssetMenu(fileName = "NewHealer", menuName = "Module/PlayerHeal")]
public class ModuleHeal : PlayerModule, IModuleUseE
{
    [Header("Settings")]
    [SerializeField] private float healAmount;
    [SerializeField] private float coolDown;
    private float nextUse = 0;

    public void Use(PlayerController player)
    {
        if (Time.time < nextUse) return;

        nextUse = Time.time + coolDown;

        player.Heal(healAmount);

        SpendCharges(1);
    } 
}
