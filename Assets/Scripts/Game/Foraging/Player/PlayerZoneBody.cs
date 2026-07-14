using UnityEngine;

public class PlayerZoneBody : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet b = collision.gameObject.GetComponent<Bullet>();
            if (b.enemy != null) {
                player.getDMG(b.enemy);
                Destroy(collision.gameObject);
            }
            return;
        }
    }
}
