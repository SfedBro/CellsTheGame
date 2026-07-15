using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAppearence : MonoBehaviour
{
    #region  fields

    [Header("Appearence Settings")]
    [SerializeField] private float spawnDuration = 2f;
    private Color color = Color.white;
    private SpriteRenderer sr;
    private readonly List<MonoBehaviour> behaviours = new List<MonoBehaviour>();
    private readonly List<Collider2D> colliders2D = new List<Collider2D>();
    private Rigidbody2D rb;


    #endregion


    #region initialization

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        behaviours.AddRange(GetComponents<MonoBehaviour>());
        foreach (var mon in behaviours) mon.enabled = false;

        colliders2D.AddRange(GetComponents<Collider2D>());
        foreach (var col in colliders2D) col.enabled = false;

        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;

        color.a = 0f;
        sr.color = color;

        StartCoroutine(AppearRoutine());
    }

    #endregion


    #region lifecycle

    private IEnumerator AppearRoutine()
    {
        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / spawnDuration);
            color.a = alpha;
            sr.color = color;
            yield return null;
        }

        color.a = 1f;
        sr.color = color;

        foreach (var mon in behaviours) mon.enabled = true;
        foreach (var col in colliders2D) col.enabled = true;
        rb.simulated = true;

        Destroy(this);
    }

    #endregion
}   
