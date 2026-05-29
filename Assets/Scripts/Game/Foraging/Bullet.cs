using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float timeToLive = 2f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;
        transform.rotation = new Quaternion(0, 0, 0, 0);

        Destroy(gameObject, timeToLive);
    }
}
