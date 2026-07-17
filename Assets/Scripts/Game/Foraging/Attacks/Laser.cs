using System;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour, IAttack
{
    #region fields

    [Header("Attack settings")]
    [SerializeField] private float dmg;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private bool isPiercing = false;

    private List<EnemyBase> currentTargets = new List<EnemyBase>();
    private float currentLength;

    #endregion


    #region initialization
    
    public void Initialize(AttackData data, Transform parent)
    {
        dmg = data.dmg;
        maxRange = data.range > 0 ? data.range : 10f;

        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(parent.up.y, parent.up.x) * Mathf.Rad2Deg, Vector3.forward);

        // Range - Scale lenght (Range -> Scale.x): 5 -> 0.75, 20 -> 2.25       | Formula:    x * 0.1f + 0.25;
        // Speed - Scale width  (Speed -> Scale.y): 2.5 -> 0.1, 10 -> 0.3)      | Formula:    (x * 2 + 2,5) / 75
        print($"Range: {data.range} - Scale: {data.range * 0.1 + 0.25f}");
        print($"Speed: {data.speed} - Scale: {(data.speed * -8 + 29.5f) / 55}");
        transform.localScale = new Vector3(data.range * 0.1f + 0.25f, (data.speed * 2f + 2.5f) / 75f, 1);
    }

    #endregion


    #region lifecycle

    void Update()
    {
        currentTargets.Clear();
        
        // 1. Raycast to find obstacles and enemies in the direction the laser is pointing
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, transform.right, maxRange);
        
        float obstacleDistance = maxRange;

        foreach (var hit in hits)
        {
            // Ignore player and trigger colliders
            if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                    if (enemy != null && !currentTargets.Contains(enemy))
                    {
                        currentTargets.Add(enemy);
                    }

                    if (!isPiercing)
                    {
                        // If not piercing, treat the first enemy as a blocking obstacle
                        obstacleDistance = hit.distance;
                        break;
                    }
                }
                else
                {
                    // Hit a solid obstacle (e.g. wall) - block the laser here
                    obstacleDistance = hit.distance;
                    break;
                }
            }
        }

        currentLength = obstacleDistance;

        // 2. Scale the laser GameObject along its X-axis to match the length of the beam
        transform.localScale = new Vector3(currentLength, transform.localScale.y, transform.localScale.z);
    }

    #endregion


    #region attack

    public void ActivateAttack()
    {
        // 3. Deal periodic damage to all registered targets
        for (int i = 0; i < currentTargets.Count; i++)
        {
            if (currentTargets[i] != null)
            {
                currentTargets[i].GetDamage(dmg);
            }
        }
    }

    #endregion
}
