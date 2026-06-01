using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUB : MonoBehaviour, Interactable
{
    [SerializeField] private float timeToEnter = 2.0f;
    [SerializeField] public static float timeToDisapear = 20f;
    public TMPro.TextMeshProUGUI hint;
    public Action onDes;

    void OnEnable()
    {
        transform.position += new Vector3(0, 0, 1);
        Destroy(gameObject, timeToDisapear);
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
        hint.enabled = false;
    }
}
