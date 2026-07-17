public interface IWeapon
{
    float CoolDownMultiplier { get; }
    void StartAttack(PlayerController player, float angle);
    void AttackTick(PlayerController player);
    void EndAttack(PlayerController player);
}
