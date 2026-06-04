using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    public EnemyBase self;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            self.OnPlayerFound();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            self.OnPlayerLost();
        }
    }
}
