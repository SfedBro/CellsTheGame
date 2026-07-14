using UnityEngine;

public class PlayerModuleController : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private PlayerController player;
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
        if (module.effects != null)
        {
            foreach (var effect in module.effects)
            {
                effect.Apply(player);
            }
        }
    }

    public void DisableModule(PlayerModule module)
    {
        if (module.effects != null)
        {
            foreach (var effect in module.effects)
            {
                effect.Remove(player);
            }
        }
    }

    #endregion
}
