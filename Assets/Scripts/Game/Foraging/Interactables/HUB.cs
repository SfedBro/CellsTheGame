using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUB : MonoBehaviour, Interactable
{
    [SerializeField] private float timeToEnter = 2.0f;
    [SerializeField] public static float timeToDisapear = 20f;
    public TMPro.TextMeshProUGUI hint;
    public Action onDes;
    private float timer = 0f;

    void OnEnable()
    {
        transform.position += new Vector3(0, 0, 1);
    }

    void OnDestroy()
    {
        onDes();
    }

    public float getTime() => timeToEnter;

    public void onInteract()
    {
        SceneManager.LoadScene("FactorySampleScene");
    }

    public void onZoneEnter()
    {
        hint.enabled = true;
    }

    public void onZoneExit()
    {
        if (hint != null)
        hint.enabled = false;
    }

    void Update()
    {
        if (!hint.enabled)
        {
            timer += Time.deltaTime;

            if (timer >= timeToDisapear)
            {
                Destroy(gameObject);
            }
        }
    }
}
