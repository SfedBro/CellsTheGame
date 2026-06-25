using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerModuleController : MonoBehaviour
{
    #region fields

    [Header("References")]
    private PlayerController player;

    [Header("Modules")]
    [SerializeField] private PlayerModule basicCannonModule;
    [SerializeField] private GameObject basicAttack;
    private IModuleCannon basicCannon;
    private List<PlayerModule> equipedModules;
    private List<PlayerModule> rechargeWait = new();
    private int recharges;

    public IReadOnlyList<PlayerModule> EquipedModules => PlayerModuleManager.Instance?.EquipedModules;
    public IReadOnlyList<PlayerModule> OwnedModules => PlayerModuleManager.Instance?.OwnedModules;

    #endregion


    #region cheats

    #region doubleCannon

    [SerializeField] private PlayerModule doubleCannon;
    private bool active = false;
    public void ActivateDouble()
    {
        if (PlayerModuleManager.Instance == null) return;
        
        doubleCannon.Recharge();

        if (active)
        {
            print("unequiped double cannon");
            PlayerModuleManager.Instance.UnequipModule(doubleCannon);
            active = false;
        }
        else
        {
            PlayerModuleManager.Instance.EquipModule(doubleCannon);
            active = false; // Actually in testing you probably want it to be true
            print("equiped double cannon");
        }
    }
    
    #endregion

    #region healer

    [SerializeField] private PlayerModule healer;
    private bool active2 = false;
    public void ActivateHealer()
    {
        healer.Recharge();

        if (active2)
        {
            print("unequiped healer - use E");
            UnequipModule(healer);
            active2 = false;
        }
        else
        {
            EquipModule(healer, true);
            active2 = true;
            print("equiped healer - use E");
        }
    }

    #endregion

    #region AllStatsUp

    [SerializeField] private PlayerModule statsUp;
    private bool active3 = false;
    public void ActivateStatsUp()
    {
        if (active3)
        {
            print("unequiped All Stats Up");
            UnequipModule(statsUp);
            active3 = false;
        }
        else
        {
            EquipModule(statsUp, true);
            active3 = true;
            print("equiped All Stats Up");
        }
    }

    #endregion

    #endregion


    #region initialization

    void Awake()
    {
        player = GetComponent<PlayerController>();

        recharges = 0;

        // Initialize basic attack module safely
        basicCannonModule = Instantiate(basicCannonModule);
        basicCannonModule.controller = this;
        basicCannon = (IModuleCannon)basicCannonModule;

        // Cheats
        doubleCannon = Instantiate(doubleCannon);
        healer = Instantiate(healer);
        statsUp = Instantiate(statsUp);
        doubleCannon.controller = this;
        healer.controller = this;
        statsUp.controller = this;

        foreach (PlayerModule m in equipedModules)
        {
            PlayerModuleManager.Instance.AddOwnedModule(doubleCannon);
        }

        // Subscribe to global equip events
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged += HandleModuleEquipped;
        }
    }

    private void OnDestroy()
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged -= HandleModuleEquipped;
        }
    }

    #endregion


    #region modules
    public void InitializeModules()
    {
        // Apply basic attack module
        ApplyModulePhysics(basicCannonModule, true);
        player.attackPrefab = basicAttack;

        if (PlayerModuleManager.Instance != null)
        {
            foreach (PlayerModule m in PlayerModuleManager.Instance.EquipedModules)
            {
                ApplyModulePhysics(m, true);
            }
        }

        // Update player attack
        player.UpdateAttackData();
    }

    private void HandleModuleEquipped(PlayerModule module, bool isEquipped)
    {
        ApplyModulePhysics(module, isEquipped);
    }

    private void ApplyModulePhysics(PlayerModule module, bool isEquipped)
    {
        if (isEquipped)
        {
            module.controller = this;
            
            if (module.isChargable && module.curCharge < 0)
            {
                if (!rechargeWait.Contains(module)) rechargeWait.Add(module);
            }

            // Apply specific logics
            switch (module)
            {
                case IModuleCannon cannon:
                    player.attackStart = cannon.AttackStart;
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
        else
        {
            // Remove weight
            player.AddMass(-module.mass);

            // Disable
            module.Disable();
            
            // Revert attack if it was a cannon
            if (module is IModuleCannon)
            {
                player.attackStart = basicCannon.AttackStart;
            }
        }
    }

    public bool EquipModule(PlayerModule module, bool add)
    {
        if (PlayerModuleManager.Instance != null)
        {
            return PlayerModuleManager.Instance.EquipModule(module);
        }
        return false;
    }

    public bool UnequipModule(PlayerModule module)
    {
        if (PlayerModuleManager.Instance != null)
        {
            return PlayerModuleManager.Instance.UnequipModule(module);
        }
        return false;
    }

    public void AddOwnedModule(PlayerModule moduleTemplate)
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.AddOwnedModule(moduleTemplate);
        }
    }

    public void RemoveOwnedModule(PlayerModule module)
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.RemoveOwnedModule(module);
        }
    }

    #endregion


    #region charges

    public void AddCharges(int amount)
    {
        foreach (PlayerModule m in rechargeWait)
        {
            m.Recharge();
            m.Enable();
            amount--;

            if (amount == 0) return;
        }

        recharges += amount;
    }

    public void Recharge(PlayerModule module)
    {
        if (recharges > 0)
        {
            recharges--;
            module.Recharge();
        }
        else
        {
            rechargeWait.Add(module);
            module.Disable();
        }
    }

    public void Disable(IModuleCannon m)
    {
        player.attackStart = basicCannon.AttackStart;
        basicCannon.SetNextHit(m.GetNextHit());
    }

    public void Disable(IModuleUseE m)
    {
        player.activeAbilityE = null;
    }

    public void Disable(IModuleStat m)
    {
        player.AddAddIncrements(m.GetAddChanges() * -1);
        player.AddMultIncrements(m.GetMultCganges() * -1);
    }

    public void Enable(IModuleCannon m)
    {
        player.attackStart = m.AttackStart;
        m.SetNextHit(basicCannon.GetNextHit());
    }

    #endregion
}
