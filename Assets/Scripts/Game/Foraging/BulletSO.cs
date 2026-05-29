using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet", menuName = "Scriptable Objects/Bullet")]
public class BulletSO : ScriptableObject
{
    public GameObject prefab;
    public float timeToLive;
    public float speed;

    private Vector2 direction;

    public BulletSO(Vector2 dir)
    {
        direction = dir;
        Instantiate(prefab);
    }
}
