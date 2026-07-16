using System;
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

        // Range - Scale lenght (Range -> Scale.x): 5 -> 0.75, 20 -> 2.25       | Formula:    x * 0.1f + 0.25;
        // Speed - Scale width  (Speed -> Scale.y): 2.5 -> 0.1, 10 -> 0.3)      | Formula:    (x * 2 + 2,5) / 75
        print($"Range: {data.range} - Scale: {data.range * 0.1 + 0.25f}");
        print($"Speed: {data.speed} - Scale: {(data.speed * -8 + 29.5f) / 55}");
        transform.localScale = new Vector3(data.range * 0.1f + 0.25f, (data.speed * 2f + 2.5f) / 75f, 1);
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
