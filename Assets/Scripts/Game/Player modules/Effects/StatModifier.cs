public enum PlayerStatType
{
    Mass,
    EngineForce,
    MaxSpeed,
    RotationSpeed,
    MaxHP,
    MinSpeed,
    Damage,
    AttackCoolDown
}

public enum ModifierType
{
    Flat,
    Percent
}

public class StatModifier
{
    public float Value { get; private set; }
    public ModifierType Type { get; private set; }
    public PlayerStatType StatType { get; private set; }
    public object Source { get; private set; }

    public StatModifier(float value, ModifierType type, PlayerStatType statType, object source)
    {
        Value = value;
        Type = type;
        StatType = statType;
        Source = source;
    }
}
