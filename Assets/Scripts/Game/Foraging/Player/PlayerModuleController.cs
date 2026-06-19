using System.Collections.Generic;
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
    private List<ModuleConflict> moduleConflicts = new();
    private List<PlayerModule> rechargeWait = new();
    private int recharges;

    #endregion


    #region cheats

    #region doubleCannon

    [SerializeField] private PlayerModule doubleCannon;
    private bool active = false;
    public void ActivateDouble()
    {
        doubleCannon.Recharge();

        if (active)
        {
            print("unequiped double cannon");
            UnequipModule(doubleCannon);
            active = false;
        }
        else
        {
            EquipModule(doubleCannon, true);
            active = true;
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

        // Get initial equiped modules and charges
        equipedModules = new();
        recharges = 0;

        // Get modules' conflicts
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
            m.controller = this;

            if (m.isChargable && m.curCharge < 0)
            {
                rechargeWait.Add(m);
            }

            if (m.conflictGroup != ModuleConflict.None)
            {
                moduleConflicts.Add(m.conflictGroup);
            }
        }
    }

    #endregion


    #region modules
    public void InitializeModules()
    {
        // Apply basic attack module
        EquipModule(basicCannonModule, false);
        player.attackPrefab = basicAttack;


        foreach (PlayerModule m in equipedModules)
        {
            EquipModule(m, false);
        }

        // Update player attack
        player.UpdateAttackData();
    }

    public bool EquipModule(PlayerModule module, bool add)
    {
        // Check conflicts
        if (module.conflictGroup != ModuleConflict.None) {
            foreach(ModuleConflict mt in moduleConflicts)
            {
                if (mt == module.conflictGroup) return false;
            }
        }

        // Equip
        if (add) equipedModules.Add(module);

        // Apply
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

        // Add weight
        player.AddMass(module.mass);

        return true;
    }

    public bool UnequipModule(PlayerModule module)
    {
        // Check equiped
        bool eqiped = false;
        foreach (PlayerModule m in equipedModules)
        {
            if (m == module)
            {
                eqiped = true;
                break;
            }
        }
        if (!eqiped) return eqiped;

        // Remove conflict
        moduleConflicts.Remove(module.conflictGroup);

        // Remove weight
        player.AddMass(-module.mass);

        // Disable
        module.Disable();

        // Remove
        equipedModules.Remove(module);

        return module;
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
