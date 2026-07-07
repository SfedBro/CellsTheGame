using UnityEngine;

[CreateAssetMenu(fileName = "NewStatsUp", menuName = "Module/PlayerStatsBooster")]
public class ModuleStatsUp : PlayerModule, IModuleStat
{
    [Header("Stats up")]
    [SerializeField] private PlayerStats addIncrement;
    [SerializeField] private PlayerStats multIncrement;

    public PlayerStats GetAddChanges() => addIncrement;

    public PlayerStats GetMultCganges() => multIncrement;
}

