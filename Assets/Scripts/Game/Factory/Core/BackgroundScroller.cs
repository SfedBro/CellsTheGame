using UnityEngine;

[ExecuteAlways]
public class BackgroundScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mainCamera;

    [Header("Tiling Settings")]
    [Tooltip("Size of one tile in world units. For 64x64 sprite with 32 PPU, this is 2.0 (since 64 / 32 = 2.0).")]
    [SerializeField] private float tileWorldSize = 2f;
    [Tooltip("Number of extra tiles to render beyond screen edges to prevent black borders when moving.")]
    [SerializeField] private int padding = 5;
    [Tooltip("Manual positional offset to visually align the background grid with Unity's Grid system.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Initialize sizing
        UpdateSize();
    }

    private void Update()
    {
        if (mainCamera == null || spriteRenderer == null) return;

        // Snapping position to multiples of tileWorldSize (2.0).
        // Since 2.0 is a multiple of the 1.0 grid size, the background's visual grid lines
        // will stay perfectly aligned with the logical building placement grid.
        Vector3 camPos = mainCamera.transform.position;
        float snappedX = Mathf.Round(camPos.x / tileWorldSize) * tileWorldSize;
        float snappedY = Mathf.Round(camPos.y / tileWorldSize) * tileWorldSize;

        // Calculate offset based on sprite pivot to keep it perfectly centered on the camera
        float pivotX = 0.5f;
        float pivotY = 0.5f;
        if (spriteRenderer.sprite != null)
        {
            Sprite sprite = spriteRenderer.sprite;
            pivotX = sprite.pivot.x / sprite.rect.width;
            pivotY = sprite.pivot.y / sprite.rect.height;
        }

        float offsetX = spriteRenderer.size.x * (0.5f - pivotX);
        float offsetY = spriteRenderer.size.y * (0.5f - pivotY);

        transform.position = new Vector3(snappedX - offsetX + offset.x, snappedY - offsetY + offset.y, transform.position.z);

        if (!Application.isPlaying)
        {
            UpdateSize();
        }
    }

    private void UpdateSize()
    {
        if (spriteRenderer == null || mainCamera == null) return;

        // Calculate current orthographic screen height and width in world units
        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;

        // Expand bounds by padding to prevent edge clipping during camera movement
        float targetWidth = width + tileWorldSize * padding * 2;
        float targetHeight = height + tileWorldSize * padding * 2;

        // Round to nearest multiples of tileWorldSize to keep tiling aligned and seamless
        targetWidth = Mathf.Ceil(targetWidth / tileWorldSize) * tileWorldSize;
        targetHeight = Mathf.Ceil(targetHeight / tileWorldSize) * tileWorldSize;

        // Configure SpriteRenderer for tiled drawing
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(targetWidth, targetHeight);
    }
}
