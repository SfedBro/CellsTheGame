using UnityEngine;

public class PlayerAwareZone : MonoBehaviour
{
    [SerializeField] private PlayerConfig player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            player.EnemyFound();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            player.EnemyLost();
        }
    }
}
