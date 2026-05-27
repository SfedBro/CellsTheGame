using UnityEngine;

public class CollectingResources : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Collectable"))
        {
            collision.transform.parent.SendMessage("onCollected");
            Destroy(collision.gameObject);
        }
    }
}
