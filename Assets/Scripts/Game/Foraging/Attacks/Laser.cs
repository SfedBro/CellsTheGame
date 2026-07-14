using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour, IAttack
{
    #region fields

    [Header("Attack settings")]
    [SerializeField] private float dmg;
    private List<EnemyBase> enemiesInRadius = new();

    #endregion


    #region initialization
    
    public void Initialize(AttackData data, Transform parent)
    {
        dmg = data.dmg;

        transform.SetParent(parent);

        transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(parent.up.y, parent.up.x) * Mathf.Rad2Deg, Vector3.forward);
    }

    #endregion


    #region events

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            enemiesInRadius.Add(collision.GetComponent<EnemyBase>());
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            enemiesInRadius.Remove(collision.GetComponent<EnemyBase>());
        }
    }

    #endregion


    #region attack

    public void  ActivateAttack()
    {
        int i = 0;
        while (i < enemiesInRadius.Count)
        {
            if (enemiesInRadius[i] == null)
            {
                enemiesInRadius.RemoveAt(i);
            }
            else
            {
                enemiesInRadius[i].GetDamage(dmg);
            }
        }
    }

    #endregion
}
