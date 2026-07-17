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
            if (newModule.effects == null || newModule.effects.Count == 0)
            {
                newModule.effects = new List<IModuleEffect>(moduleTemplate.effects);
            }
        }
        ownedModules.Add(newModule);
    }

    public void RemoveOwnedModule(PlayerModule module)
    {
        ownedModules.Remove(module);
    }

    public void EnsureWeaponEquipped()
    {
        // 1. Check if any weapon is already equipped
        foreach (var mod in equipedModules)
        {
            if (mod != null && mod.moduleType == ModuleType.Cannon)
            {
                return; // Already has a weapon equipped!
            }
        }

        // 2. Not equipped. Search owned modules for "BasicCannon"
        PlayerModule weaponToEquip = null;
        foreach (var mod in ownedModules)
        {
            if (mod != null && mod.moduleType == ModuleType.Cannon)
            {
                if (mod.name.Contains("Basic") || mod.title.Contains("Basic") || mod.name.Contains("Cannon") || mod.title.Contains("Cannon"))
                {
                    weaponToEquip = mod;
                    break;
                }
            }
        }

        // 3. If not found, just take any owned cannon module
        if (weaponToEquip == null)
        {
            foreach (var mod in ownedModules)
            {
                if (mod != null && mod.moduleType == ModuleType.Cannon)
                {
                    weaponToEquip = mod;
                    break;
                }
            }
        }

        // 4. If still not found, fallback to startingOwnedModules to find the template, clone it, and equip it!
        if (weaponToEquip == null)
        {
            PlayerModule template = null;
            foreach (var mod in startingOwnedModules)
            {
                if (mod != null && mod.moduleType == ModuleType.Cannon)
                {
                    template = mod;
                    break;
                }
            }
            if (template != null)
            {
                weaponToEquip = Instantiate(template);
                if (weaponToEquip.effects == null || weaponToEquip.effects.Count == 0)
                {
                    weaponToEquip.effects = new List<IModuleEffect>(template.effects);
                }
                Debug.Log($"[PlayerModuleManager] Created a new runtime instance of starting weapon template: {template.title}");
            }
        }

        // 5. Equip it!
        if (weaponToEquip != null)
        {
            if (ownedModules.Contains(weaponToEquip))
            {
                ownedModules.Remove(weaponToEquip);
            }
            EquipModule(weaponToEquip);
            Debug.Log($"[PlayerModuleManager] Automatically equipped {weaponToEquip.title} because no weapon was equipped before entering Foraging.");
        }
        else
        {
            Debug.LogWarning("[PlayerModuleManager] No owned or starting weapon module found to automatically equip!");
        }
    }
}
