using System.Collections.Generic;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Services (Execution Order Matters!)")]
    [Tooltip("Drag your manager components here in the order they should be initialized.")]
    public List<MonoBehaviour> services;

    private static GameBootstrapper Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1. Initialize Phase
        foreach (var serviceObj in services)
        {
            if (serviceObj == null) continue;

            if (serviceObj is IGameService service)
            {
                service.InitializeService();
            }
            else
            {
                Debug.LogWarning($"Bootstrapper: Object {serviceObj.name} does not implement IGameService!");
            }
        }
    }

    private void Start()
    {
        // 2. Start Phase
        foreach (var serviceObj in services)
        {
            if (serviceObj == null) continue;

            if (serviceObj is IGameService service)
            {
                service.StartService();
            }
        }
    }
}
