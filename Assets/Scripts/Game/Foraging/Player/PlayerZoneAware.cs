using UnityEngine;

public class PlayerZoneAware : MonoBehaviour
{
    [SerializeField] private HintsController hintsController;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            hintsController.OnPlayerInDanger();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            hintsController.OnPlayerLeaveDanger();
        }
    }
}
