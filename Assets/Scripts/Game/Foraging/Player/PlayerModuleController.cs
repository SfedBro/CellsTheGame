using UnityEngine;

public class PlayerModuleController : MonoBehaviour
{
    #region fields

    [Header("References")]
    private PlayerController player;
    #endregion


    #region initialization

    public void InitializeModules()
    {
        if (PlayerModuleManager.Instance != null)
        {
            foreach (PlayerModule m in PlayerModuleManager.Instance.EquipedModules)
            {
                initializeModule(m);
            }
        }
    }

    #endregion


    #region modules

    private void initializeModule(PlayerModule module)
    {
        module.controller = this;
        
        if (!module.isChargable || module.curCharge > 0)
        {
            EnableModule(module);
        }
    }

    public void EnableModule(PlayerModule module)
    {
        // Apply specific logics
        switch (module)
        {
            case IModuleCannon cannon:
                player.curCannon = cannon;
            break;
            case IModuleUseE usable:
                player.activeAbilityE = usable.Use;
            break;
            case IModuleStat stat:
                player.AddAddIncrements(stat.GetAddChanges());
                player.AddMultIncrements(stat.GetMultCganges());
            break;
        }
    }

    public void DisableModule(PlayerModule module)
    {
        switch (module)
        {
            case IModuleCannon cannon:
                player.curCannon = player.baseCannon;
            break;
            case IModuleUseE usable:
                player.activeAbilityE = null;
            break;
            case IModuleStat stat:
                player.AddAddIncrements(stat.GetAddChanges() * -1);
                player.AddMultIncrements(stat.GetMultCganges() * -1);
            break;
        }
    }

    #endregion
}
