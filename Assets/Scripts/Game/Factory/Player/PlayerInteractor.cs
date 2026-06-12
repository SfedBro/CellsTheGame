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
        cam = Camera.main ? Camera.main : Instantiate(new Camera());
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
        if (Input.GetKeyDown(KeyCode.X))
        {
            SceneManager.LoadScene("Foraging");
        }
    }

    private void TryInteract()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit && hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }
        }

        if (GridManager.Instance != null)
        {
            Grid grid = FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                // Применяем смещение на 0.5, как в строительном скрипте
                Vector3Int cell = grid.WorldToCell(new Vector3(mousePos.x + 0.5f, mousePos.y + 0.5f, 0f));
                MonoBehaviour building = GridManager.Instance.GetBuilding(cell);
                if (building is IInteractable gridInteractable)
                {
                    gridInteractable.Interact(this);
                }
            }
        }
    }
    #endregion

    #region Movement
    private void Move()
    {
        cam.GetComponent<Transform>().localPosition += FactoryMovement.Movement(movementSpeed) * Time.deltaTime;
    }
    #endregion
}
