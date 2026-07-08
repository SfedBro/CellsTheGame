using UnityEngine;

public class Bullet : MonoBehaviour, IAttack
{
    #region fields
    
    [SerializeField] public float speed;
    [SerializeField] public float dmg;
    [SerializeField] public EnemyBase enemy;

    public Rigidbody2D rb;

    #endregion


    #region initialization

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(AttackData data, Transform parent, float rotation)
    {
        transform.SetParent(parent);

        dmg = data.dmg;
        speed = data.speed;

        Destroy(gameObject, data.timeToLive);

        float scale = (dmg + 3f) / 8f;
        transform.localScale = new Vector3(scale, scale, 1);

        transform.Rotate(0, 0, rotation);

        float angleRad = rotation * Mathf.Deg2Rad;
        rb.linearVelocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;
    }

    #endregion
}
