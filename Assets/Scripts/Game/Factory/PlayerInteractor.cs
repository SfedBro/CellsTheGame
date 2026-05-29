using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractor : MonoBehaviour
{
    public Camera cam;
    public float speed = 5f;
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
        if (Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene("Foraging");
        }
        Movement();
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
    bool isUp, isDown, isLeft, isRight, isHorizontal, isVertical;
    float movementMultiplier, rightMultiplier, topMultiplier;
    void Movement()
    {
        isUp = isDown = isLeft = isRight = isHorizontal = isVertical = false;
        movementMultiplier = 1f;
        topMultiplier = 0f;
        rightMultiplier = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { isUp = true; }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { isDown = true; }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { isLeft = true; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { isRight = true; }
        if (isUp || isDown) { isVertical = true; }
        if (isLeft || isRight) { isHorizontal = true; }
        if (isVertical || isHorizontal) { movementMultiplier = 0.707f; }
        if (isUp) { topMultiplier += 1; }
        if (isDown) { topMultiplier -= 1; }
        if (isLeft) { rightMultiplier -= 1; }
        if (isRight) { rightMultiplier += 1; }
        cam.GetComponent<Transform>().localPosition += new Vector3(rightMultiplier, topMultiplier, 0) * Time.deltaTime * movementMultiplier * speed;
    }
}
