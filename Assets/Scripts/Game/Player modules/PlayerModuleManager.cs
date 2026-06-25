using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModuleManager : MonoBehaviour, IGameService
{
    public static PlayerModuleManager Instance;

    [Header("Testing / Default")]
    public List<PlayerModule> startingOwnedModules = new();

    private List<PlayerModule> equipedModules = new();
    private List<PlayerModule> ownedModules = new(); 
    private List<ModuleConflict> moduleConflicts = new();

    public IReadOnlyList<PlayerModule> EquipedModules => equipedModules;
    public IReadOnlyList<PlayerModule> OwnedModules => ownedModules;

    // Fired when a module is equipped (true) or unequipped (false)
    public event Action<PlayerModule, bool> OnModuleEquipStatusChanged;

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            InitializeService();
        }
    }

    public void InitializeService()
    {
        if (isInitialized) return;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load starting testing modules
        foreach (var mod in startingOwnedModules)
        {
            if (mod != null) AddOwnedModule(mod);
        }
        
        isInitialized = true;
    }

    public void StartService()
    {
        if (Instance != this) return;
        Load();
    }

    private void Load()
    {
        // TODO: Load from SaveManager
    }

    public bool EquipModule(PlayerModule module)
    {
        // Check conflicts
        if (module.conflictGroup != ModuleConflict.None)
        {
            foreach (ModuleConflict mt in moduleConflicts)
            {
                if (mt == module.conflictGroup) return false;
            }
        }

        // Add to equipped
        equipedModules.Add(module);

        if (module.conflictGroup != ModuleConflict.None)
        {
            moduleConflicts.Add(module.conflictGroup);
        }

        // Fire event
        OnModuleEquipStatusChanged?.Invoke(module, true);

        return true;
    }

    public bool UnequipModule(PlayerModule module)
    {
        if (!equipedModules.Contains(module)) return false;

        // Remove conflict
        if (module.conflictGroup != ModuleConflict.None)
        {
            moduleConflicts.Remove(module.conflictGroup);
        }

        // Remove from equipped
        equipedModules.Remove(module);

        // Fire event
        OnModuleEquipStatusChanged?.Invoke(module, false);

        return true;
    }

    public void AddOwnedModule(PlayerModule moduleTemplate)
    {
        // Clone to prevent mutating ScriptableObject asset
        PlayerModule newModule = moduleTemplate;
        if (!moduleTemplate.name.Contains("(Clone)"))
        {
            newModule = Instantiate(moduleTemplate);
        }
        ownedModules.Add(newModule);
    }

    public void RemoveOwnedModule(PlayerModule module)
    {
        ownedModules.Remove(module);
    }
}
