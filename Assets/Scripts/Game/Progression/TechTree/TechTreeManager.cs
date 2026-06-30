using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

public class TechTreeManager : MonoBehaviour, IGameService
{
    public static TechTreeManager Instance;

    private HashSet<string> unlockedNodes = new HashSet<string>();

    private string saveKey = "TechTreeSave";

    private void Awake()
    {
        
        if (Instance == null)
        {
            InitializeService();
        }
    }

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool hasStarted = false;

    public void StartService()
    {
        if (Instance != this || hasStarted) return;
        hasStarted = true;
        Load();
    }

    private void Start()
    {
        StartService();
    }

    public bool IsNodeUnlocked(string nodeId)
    {
        return unlockedNodes.Contains(nodeId);
    }

    public bool AreDependenciesMet(TechTreeNodeData node)
    {
        if (node.dependencies == null || node.dependencies.Count == 0)
            return true;

        foreach (var dep in node.dependencies)
        {
            if (dep != null && !IsNodeUnlocked(dep.nodeId))
            {
                return false;
            }
        }
        return true;
    }

    public bool CanAfford(TechTreeNodeData node)
    {
        if (node.cost == null || node.cost.Count == 0) return true;

        foreach (var c in node.cost)
        {
            if (PlayerInventory.Instance.Inventory.GetAmount(c.type) < c.amount)
            {
                return false;
            }
        }
        return true;
    }

    public bool TryPurchaseNode(TechTreeNodeData node)
    {
        if (IsNodeUnlocked(node.nodeId)) return false;
        if (!AreDependenciesMet(node)) return false;
        if (!CanAfford(node)) return false;

        // Deduct cost
        foreach (var c in node.cost)
        {
            PlayerInventory.Instance.Inventory.RemoveItem(c.type, c.amount);
        }

        unlockedNodes.Add(node.nodeId);
        
        ApplyEffects(node);
        
        Save();

        return true;
    }

    private void ApplyEffects(TechTreeNodeData node)
    {
        if (node.effects == null) return;
        
        foreach (var effect in node.effects)
        {
            switch (effect.effectType)
            {
                case TechEffectType.UnlockBuilding:
                    Debug.Log($"[TechTree] Unlocked Building: {effect.stringParameter}");
                    // Here you can add logic to register the building in BuildManager
                    break;
                case TechEffectType.IncreaseMaxLevel:
                    Debug.Log($"[TechTree] Max Level Increased by {effect.floatParameter}");
                    break;
                case TechEffectType.UnlockLocation:
                    Debug.Log($"[TechTree] Unlocked Location: {effect.stringParameter}");
                    break;
                case TechEffectType.Custom:
                    Debug.Log($"[TechTree] Custom effect applied: {effect.stringParameter}");
                    break;
            }
        }
    }

    public void UnlockNodeDebug(string nodeId)
    {
        unlockedNodes.Add(nodeId);
        Save();
    }

    public void Save()
    {
        var data = new SaveData.TechTreeData();
        data.unlockedNodeIds = unlockedNodes.ToList();
        SaveManager.Save(saveKey, data);
        Debug.Log("Saved Tech Tree");
    }

    public void Load()
    {
        var data = SaveManager.Load<SaveData.TechTreeData>(saveKey);
        if (data != null && data.unlockedNodeIds != null)
        {
            unlockedNodes = new HashSet<string>(data.unlockedNodeIds);
        }
        Debug.Log("Loaded Tech Tree");
    }
}
