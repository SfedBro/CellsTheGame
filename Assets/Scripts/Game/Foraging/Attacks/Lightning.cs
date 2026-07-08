using System.Collections.Generic;
using UnityEngine;

public class Lightning //: MonoBehaviour, IAttack
{
    // #region fields

    // [Header("Attack settings")]
    // [SerializeField] private float nextTargetRadius;
    // [SerializeField] private float dmg;
    // private BoxCollider2D boxCollider2D;
    // private CircleCollider2D circleCollider2D;
    // private bool isFound = false;

    // #endregion


    // #region initialization

    // void Awake()
    // {
    //     boxCollider2D.enabled = false;
    //     circleCollider2D.enabled = false;
    // }

    // public void Initialize(AttackData data, Transform parent, float rotation)
    // {
    //     dmg = data.dmg;

    //     transform.SetParent(parent);

    //     transform.Rotate(new Vector3(0, 0, rotation));
        
    //     boxCollider2D.enabled = true;
    // }

    // #endregion


    // #region events

    // void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (collision.CompareTag("Enemy"))
    //     {
    //         if (boxCollider2D.enabled) {
    //             boxCollider2D.enabled = false;
    //             collision.GetComponent<EnemyBase>().GetDamage(dmg);

    //             circleCollider2D.enabled = true;

    //             Destroy(this, 1);
    //         } else
    //         {
    //             circleCollider2D.enabled = false;

    //             collision.transform.position
    //         }
    //     }
    // }

    // #endregion
}
