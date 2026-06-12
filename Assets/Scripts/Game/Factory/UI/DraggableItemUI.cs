using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemSlotUI sourceSlot;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Vector2 originalPosition;
    private Transform originalParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        sourceSlot = GetComponentInParent<ItemSlotUI>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (sourceSlot == null || sourceSlot.inventory == null || sourceSlot.inventory.slots[sourceSlot.slotIndex].IsEmpty)
        {
            eventData.pointerDrag = null; // Cancel drag
            return;
        }

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Move to highest UI layer to render above everything else
        if (parentCanvas != null)
        {
            transform.SetParent(parentCanvas.transform);
            transform.SetAsLastSibling();
        }
        
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // Allow events to pass through to slots behind
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
        else
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Return to original parent slot
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}
