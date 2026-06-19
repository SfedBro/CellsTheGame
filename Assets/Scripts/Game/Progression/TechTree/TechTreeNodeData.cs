using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tech Node", menuName = "Factory/Tech Tree Node")]
public class TechTreeNodeData : ScriptableObject
{
    [Header("Node Identity")]
    public string nodeId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Requirements")]
    public List<TechTreeNodeData> dependencies = new List<TechTreeNodeData>();
    public List<ItemStack> cost = new List<ItemStack>();

    [Header("Rewards")]
    public List<TechEffect> effects = new List<TechEffect>();
}
