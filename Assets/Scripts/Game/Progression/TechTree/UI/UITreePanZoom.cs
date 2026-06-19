using UnityEngine;
using UnityEngine.EventSystems;

public class UITreePanZoom : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Tooltip("The panel containing all nodes and lines that will be moved and scaled.")]
    public RectTransform contentRect;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float maxZoom = 2f;
    public float minZoom = 0.2f;

    private Canvas canvas;

    private void Awake()
    {
        if (contentRect == null) contentRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (contentRect == null || canvas == null) return;

        // Move the content based on mouse drag, accounting for Canvas scaling
        contentRect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (contentRect == null) return;

        float scroll = eventData.scrollDelta.y;
        if (scroll == 0) return;

        // Find the mouse position inside the content rect BEFORE scaling
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosBefore);

        // Calculate new scale
        float scaleMultiplier = 1f + (scroll * zoomSpeed);
        Vector3 newScale = contentRect.localScale * scaleMultiplier;

        // Clamp the scale to prevent zooming in/out too much
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        newScale.z = 1f;

        // Apply the new scale
        contentRect.localScale = newScale;

        // Find the mouse position inside the content rect AFTER scaling
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosAfter);

        // Shift the content rect so it zooms exactly where the mouse pointer is
        Vector2 offset = (localMousePosAfter - localMousePosBefore);
        // Convert local offset to scaled anchored offset
        offset = new Vector2(offset.x * newScale.x, offset.y * newScale.y);
        
        contentRect.anchoredPosition += offset;
    }
}
