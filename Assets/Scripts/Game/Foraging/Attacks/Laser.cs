using UnityEngine;

public class Laser : MonoBehaviour, IAttack
{
    #region fields

    [SerializeField] private float dmg;
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
        dmg = data.dmg;

        // Destroy(gameObject, data.timeToLive);

        // float scale = (dmg + 3f) / 8f;
        // transform.localScale = new Vector3(scale, scale, 1);

        transform.SetParent(parent);

        transform.Rotate(new Vector3(0, 0, rotation));
    }

    #endregion

    #region attack

    public void Activate()
    {
        throw new System.NotImplementedException();
    }

    #endregion
}
