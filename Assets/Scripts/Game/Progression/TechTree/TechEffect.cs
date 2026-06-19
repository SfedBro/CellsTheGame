using System;

public enum TechEffectType
{
    UnlockBuilding,
    IncreaseMaxLevel,
    UnlockLocation,
    Custom
}

[Serializable]
public class TechEffect
{
    public TechEffectType effectType;
    public string stringParameter;
    public float floatParameter;
}
