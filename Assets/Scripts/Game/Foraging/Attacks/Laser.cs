using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour, IAttack
{
    #region fields

    [Header("Attack settings")]
    [SerializeField] private float attackTime = 1;
    [SerializeField] private float dmg;
    private List<EnemyBase> enemiesInRadius = new();
    private float attackTimer = 0f;

    #endregion


    #region initialization
    
    public void Initialize(AttackData data, Transform parent, float rotation)
    {
        dmg = data.dmg;

        transform.SetParent(parent);

        transform.Rotate(new Vector3(0, 0, rotation));
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


    #region lifecycle

    void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer > attackTime)
        {
            attackTimer = 0;

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
    }

    #endregion
}
