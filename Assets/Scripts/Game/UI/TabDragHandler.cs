using UnityEngine;
using UnityEngine.EventSystems;

public class TabDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PanelTabType tabType;
    [HideInInspector] public InventoryPanelUI sourcePanel;
    
    private PlayerInventoryWindow parentWindow;

    public void Initialize(PlayerInventoryWindow window, InventoryPanelUI panel, PanelTabType type)
    {
        parentWindow = window;
        sourcePanel = panel;
        tabType = type;
    }

    private void Start()
    {
        // Try to auto-resolve if not programmatically initialized
        if (sourcePanel == null)
        {
            sourcePanel = GetComponentInParent<InventoryPanelUI>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentWindow == null && sourcePanel != null)
        {
            // Find parent window from source panel
            parentWindow = GetComponentInParent<PlayerInventoryWindow>();
        }

        if (parentWindow != null)
        {
            parentWindow.OnTabDragBegin(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentWindow != null)
        {
            parentWindow.OnTabDragUpdate(eventData.position, tabType, sourcePanel);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentWindow != null)
        {
            parentWindow.OnTabDragEnd(eventData.position, tabType, sourcePanel);
        }
    }
}
