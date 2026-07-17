using System;

public enum TechEffectType
{
    UnlockBuilding,
    IncreaseMaxLevel,
    UnlockLocation,
    UnlockModule,
    Custom
}

[Serializable]
public class TechEffect
{
    public TechEffectType effectType;
    public string stringParameter;
    public float floatParameter;
}
