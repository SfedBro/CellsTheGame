using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour
{
    public Camera cam;
    public float movementSpeed = 5f;
    public FactoryMovement mov;
    private void Start()
    {
        cam = Camera.main ? Camera.main : Instantiate(new Camera());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            TryInteract();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            SceneManager.LoadScene("Foraging");
        }
        Move();
    }

    void TryInteract()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
                interactable.Interact(this);
        }
    }
    void Move()
    {
        cam.GetComponent<Transform>().localPosition += FactoryMovement.Movement(movementSpeed) * Time.deltaTime;
    }
}
