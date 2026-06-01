using UnityEngine;

public class EnemyBody : MonoBehaviour
{
    public Enemy self;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerConfig p = collision.gameObject.GetComponent<PlayerConfig>();
            p.getDMG(1);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            self.GetDamage(1);
            Destroy(collision.gameObject);
        }
    }
}
