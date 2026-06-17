using UnityEngine;

public class EnemyBody : MonoBehaviour
{
    public EnemyBase self;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController p = collision.gameObject.GetComponent<PlayerController>();
            p.getDMG(self);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet b = collision.gameObject.GetComponent<Bullet>();
            if (b.enemy == null) {
                self.GetDamage(b.dmg);
                Destroy(collision.gameObject);
            }
        }
    }
}
