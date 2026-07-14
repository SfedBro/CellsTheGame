using UnityEngine;

public class Bullet : MonoBehaviour, IAttack
{
    #region fields
    
    [SerializeField] public float dmg;
    [SerializeField] public float range;
    [SerializeField] public EnemyBase enemy;
    private Vector3 startPosition;

    public Rigidbody2D rb;

    #endregion


    #region initialization

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(AttackData data, Transform parent)
    {
        dmg = data.dmg;
        range = data.range;

        float scale = (dmg + 3f) / 8f;
        transform.localScale = new Vector3(scale, scale, 1);

        transform.Rotate(parent.right);

        Vector3 direction = parent.up;
        rb.linearVelocity = direction * data.speed;
        startPosition = parent.position;

        transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(parent.up.y, parent.up.x) * Mathf.Rad2Deg, Vector3.forward);
    }

    #endregion


    #region lifecycle

    void Update()
    {
        if (Vector3.Distance(transform.position, startPosition) >= range)
        {
            Destroy(gameObject);
        }
    }

    #endregion
}
