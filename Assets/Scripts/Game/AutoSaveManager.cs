using UnityEngine;
using System.Collections;

public class AutoSaveManager : MonoBehaviour, IGameService
{
    public static AutoSaveManager Instance;
    
    [SerializeField] private float autoSaveIntervalSeconds = 60f;
    private bool isSaving = false;
    private bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("AutoSaveManager");
        var instance = go.AddComponent<AutoSaveManager>();
        instance.InitializeService();
        instance.StartService();
    }

    private void Awake()
    {
        if (Instance == null)
            InitializeService();
    }

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        isInitialized = true;
    }

    public void StartService()
    {
        if (Instance != this) return;
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveIntervalSeconds);
            SaveAll();
        }
    }

    private void Update()
    {
        // Debug manual save
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveAll();
        }
    }

    private void OnApplicationQuit()
    {
        if (isInitialized)
        {
            SaveAll();
        }
    }

    public void SaveAll()
    {
        if (isSaving) return;
        isSaving = true;

        Debug.Log("[AutoSaveManager] Starting global save...");
        
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.Save();
            
        if (BuildManager.Instance != null)
            BuildManager.Instance.Save();
            
        if (TechTreeManager.Instance != null)
        {
            // TechTreeManager has no Save() method, it just does it in MainMenuNavigation right now
            // Let's call a Save method if it exists
            var tt = TechTreeManager.Instance;
            var mi = tt.GetType().GetMethod("Save");
            if (mi != null) mi.Invoke(tt, null);
        }

        Debug.Log("[AutoSaveManager] Global save complete.");
        isSaving = false;
    }
}
