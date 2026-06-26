using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour
{
    #region Configuration
    [SerializeField] private Camera cam;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private FactoryMovement mov;
    #endregion

    #region Lifecycle
    private void Start()
    {
    }

    private void Update()
    {
        HandleInput();
        Move();
    }
    #endregion

    #region Input & Logic
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            TryInteract();
        }
        if (Input.GetKeyDown(KeyCode.X) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            SceneManager.LoadScene("Foraging");
        }
    }

    private Camera GetCamera()
    {
        if (cam == null) cam = Camera.main;
        return cam;
    }

    private void TryInteract()
    {
        BuildManager buildManager = FindFirstObjectByType<BuildManager>();
        if (buildManager != null && (buildManager.IsBuildMode || buildManager.IsEditMode))
        {
            return; // Don't allow interaction in build/edit mode
        }

        // Debug.Log("[PlayerInteractor] TryInteract called!");
        Vector2 mousePos = GetCamera().ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit && hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Debug.Log($"[PlayerInteractor] Hit collider with IInteractable: {hit.collider.name}");
                interactable.Interact(this);
                return;
            }
        }

        if (GridManager.Instance != null)
        {
            // Debug.Log("[PlayerInteractor] GridManager.Instance is NOT null.");
            Grid grid = FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                // Debug.Log("[PlayerInteractor] Grid found in scene.");
                Vector3Int cell = grid.WorldToCell(new Vector3(mousePos.x + 0.5f, mousePos.y + 0.5f, 0f));
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                
                if (building != null)
                {
                    // Debug.Log($"[PlayerInteractor] Found building at cell {cell}: {building.name}");
                    if (building is IInteractable gridInteractable)
                    {
                        // Debug.Log("[PlayerInteractor] Calling Interact on Grid building");
                        gridInteractable.Interact(this);
                    }
                }
                else
                {
                    // Debug.Log($"[PlayerInteractor] No building found at cell {cell}");
                }
            }
            else
            {
                // Debug.LogWarning("[PlayerInteractor] Grid is NULL in scene!");
            }
        }
        else
        {
            // Debug.LogWarning("[PlayerInteractor] GridManager.Instance is NULL!");
        }
    }
    #endregion

    #region Movement
    private void Move()
    {
        GetCamera().GetComponent<Transform>().localPosition += FactoryMovement.Movement(movementSpeed) * Time.deltaTime;
    }
    #endregion
}
