using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager instance;
    protected Inventory inventory;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
