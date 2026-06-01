using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;    // higher = faster follow

    private Vector3 velocity = Vector3.zero;            // for smooth damping

    private void LateUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(player.position.x, player.position.y, transform.position.z), ref velocity, 1f / smoothSpeed);
    }
}