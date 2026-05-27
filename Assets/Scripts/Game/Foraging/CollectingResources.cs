using UnityEngine;

public class CollectingResources : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Collectable"))
        {
            collision.transform.parent.SendMessage("onCollected");
            Destroy(collision.gameObject);
        }
    }
}
