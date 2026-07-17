using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Text;
using DG.Tweening;

public class TechTreeNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public TechTreeNodeData nodeData;

    [Header("UI References")]
    public Image background;
    public Image icon;
    public TMP_Text titleText;
    
    [Header("Hover Info (Optional)")]
    public GameObject costPanel; // Panel to show cost on hover
    public TMP_Text costText;

    [Header("Details Panel (Right Click)")]
    public GameObject detailsPanel;
    public TMP_Text descriptionText;
    public Image detailsImage;
    public CanvasGroup detailsCanvasGroup;

    [Header("Colors")]
    public bool overrideBackgroundColor = true;
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color availableColor = new Color(1f, 1f, 1f, 1f);
    public Color unlockedColor = new Color(1f, 0.8f, 0f, 1f);

    private bool isExpanded = false;
    private bool isHovered = false;
    
    // Drag detection
    private Vector2 pointerDownPos;
    private bool isDragging = false;
    private float dragThreshold = 10f;

    private void Start()
    {
        if (nodeData != null)
        {
            Debug.Log($"[TechTreeNode] {gameObject.name} initialized with data: {nodeData.name}. Icon: {(nodeData.icon != null ? nodeData.icon.name : "NULL")}, Display Name: {nodeData.displayName}");
            if (icon != null) icon.sprite = nodeData.icon;
            if (titleText != null) titleText.text = nodeData.displayName;
            
            if (descriptionText != null) descriptionText.text = nodeData.description;
            if (detailsImage != null) detailsImage.sprite = nodeData.icon; // Use the same icon, or you could add a new field in TechTreeNodeData
            if (costText != null) costText.text = GetCostString();
        }
        else
        {
            Debug.LogWarning($"[TechTreeNode] {gameObject.name} has NO NodeData assigned!");
        }

        if (costPanel != null) costPanel.SetActive(false);
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (detailsCanvasGroup != null) detailsCanvasGroup.alpha = 0f;
    }

    private string GetCostString()
    {
        if (nodeData == null || nodeData.cost == null || nodeData.cost.Count == 0) return "Free";
        StringBuilder sb = new StringBuilder();
        foreach (var req in nodeData.cost)
        {
            sb.AppendLine($"{req.type}: {req.amount}");
        }
        return sb.ToString().TrimEnd();
    }

    public void Refresh()
    {
        if (nodeData == null || TechTreeManager.Instance == null) return;

        bool isUnlocked = TechTreeManager.Instance.IsNodeUnlocked(nodeData.nodeId);
        bool depsMet = TechTreeManager.Instance.AreDependenciesMet(nodeData);

        if (isUnlocked)
        {
            if (background != null && overrideBackgroundColor) background.color = unlockedColor;
            if (icon != null) icon.color = Color.white;
        }
        else if (depsMet)
        {
            if (background != null && overrideBackgroundColor) background.color = availableColor;
            if (icon != null) icon.color = Color.white;
        }
        else
        {
            if (background != null && overrideBackgroundColor) background.color = lockedColor;
            if (icon != null) icon.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Darker icon
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (costPanel != null) costPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (costPanel != null) costPanel.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        isDragging = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Handled by click if not dragged
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Vector2.Distance(eventData.position, pointerDownPos) > dragThreshold)
        {
            isDragging = true;
        }

        if (isDragging || nodeData == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (TechTreeUI.Instance != null && TechTreeUI.Instance.globalDetailsPanel != null)
            {
                TechTreeUI.Instance.ShowNodeDetails(nodeData);
            }
            else
            {
                ToggleExpandedState();
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            TryPurchase();
        }
    }

    private void TryPurchase()
    {
        bool success = TechTreeManager.Instance.TryPurchaseNode(nodeData);
        if (success)
        {
            Debug.Log($"Purchased node {nodeData.displayName}!");
            TechTreeUI.Instance?.RefreshTree();
        }
        else
        {
            Debug.Log($"Cannot purchase {nodeData.displayName} (Locked, no funds, or already owned).");
        }
    }

    private void ToggleExpandedState()
    {
        isExpanded = !isExpanded;
        float duration = 0.2f;
        
        transform.DOKill();
        if (detailsCanvasGroup != null) detailsCanvasGroup.DOKill();
        
        if (isExpanded)
        {
            transform.SetAsLastSibling(); // Bring to front
            if (detailsPanel != null) detailsPanel.SetActive(true);
            
            transform.DOScale(Vector3.one * 1.5f, duration).SetEase(Ease.OutQuad);
            if (detailsCanvasGroup != null) detailsCanvasGroup.DOFade(1f, duration);
        }
        else
        {
            transform.DOScale(Vector3.one, duration).SetEase(Ease.OutQuad);
            if (detailsCanvasGroup != null)
            {
                detailsCanvasGroup.DOFade(0f, duration).OnComplete(() => {
                    if (detailsPanel != null) detailsPanel.SetActive(false);
                });
            }
            else
            {
                if (detailsPanel != null) detailsPanel.SetActive(false);
            }
        }
    }
}
