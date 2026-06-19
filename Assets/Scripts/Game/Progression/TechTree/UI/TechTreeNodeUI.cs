using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TechTreeNodeUI : MonoBehaviour, IPointerClickHandler
{
    public TechTreeNodeData nodeData;

    [Header("UI References")]
    public Image background;
    public Image icon;
    public TMP_Text titleText;

    [Header("Colors")]
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color availableColor = new Color(1f, 1f, 1f, 1f);
    public Color unlockedColor = new Color(1f, 0.8f, 0f, 1f);

    private void Start()
    {
        if (nodeData != null)
        {
            if (icon != null) icon.sprite = nodeData.icon;
            if (titleText != null) titleText.text = nodeData.displayName;
        }
    }

    public void Refresh()
    {
        if (nodeData == null || TechTreeManager.Instance == null) return;

        bool isUnlocked = TechTreeManager.Instance.IsNodeUnlocked(nodeData.nodeId);
        bool depsMet = TechTreeManager.Instance.AreDependenciesMet(nodeData);

        if (isUnlocked)
        {
            background.color = unlockedColor;
            icon.color = Color.white;
        }
        else if (depsMet)
        {
            background.color = availableColor;
            icon.color = Color.white;
        }
        else
        {
            background.color = lockedColor;
            icon.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Darker icon
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (nodeData == null) return;

        // Try to purchase directly for now.
        // Later, this could open a info panel with a "Purchase" button instead.
        bool success = TechTreeManager.Instance.TryPurchaseNode(nodeData);
        if (success)
        {
            Debug.Log($"Purchased node {nodeData.displayName}!");
            // Refresh entire tree to update colors of dependent nodes
            TechTreeUI.Instance?.RefreshTree();
        }
        else
        {
            Debug.Log($"Cannot purchase {nodeData.displayName} (Locked, no funds, or already owned).");
        }
    }
}
