using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] public float speed = 10f;
    [SerializeField] public float timeToLive = 2f;
    [SerializeField] public int dmg = 1;
    [SerializeField] public EnemyBase enemy;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        Destroy(gameObject, timeToLive);
    }
}
