using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ForagingManager : MonoBehaviour
{
    #region fields

    [Header("Controller")]
    private InputSystem_Actions inputActions;
    
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Player Mass")]
    [SerializeField] private int resourcesInMassUnit = 10;
    private ResourcesManager rm = ResourcesManager.instance;
    private Dictionary<ItemType, int> resources = new();
    private int totalResources = 0;
    private int massAddition;

    #endregion


    #region gettersNsetters

    public InputSystem_Actions GetInputSystem() => inputActions;

    #endregion

    #region initialization

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        resourceRecalculation();
    }

    void OnEnable()
    {
        inputActions.Player.Jump.performed += respawnPlayer;
        inputActions.Enable();
        rm.Subscrive(addResource);
    }

    void OnDisable()
    {
        inputActions.Player.Jump.performed -= respawnPlayer;
        inputActions.Disable();
        rm.Unsubscrive(addResource);
    }

    #endregion


    #region playerDeath

    private bool isPlayerDead = false;

    private void respawnPlayer(InputAction.CallbackContext context)
    {
        if (!isPlayerDead) return;

        isPlayerDead = false;
        player.Respawn();
    }

    public void onPlayerDeath()
    {
        isPlayerDead = true;
    }

    #endregion


    #region playerMass

    private void resourceRecalculation()
    {
        totalResources = 0;
        foreach (ItemType t in Enum.GetValues(typeof(ItemType)))
        {
            resources[t] = rm.getResourceAmount(t);
            totalResources += rm.getResourceAmount(t);
        }

        massAddition = totalResources / resourcesInMassUnit;
        player.AddMass(massAddition);
    }

    private void addResource(ItemType itemType, int newAmount)
    {
        int increment = newAmount - resources[itemType];
        resources[itemType] = newAmount;

        totalResources += increment;
        int massIncrement = (totalResources / resourcesInMassUnit) - massAddition;

        massAddition += massIncrement;
        player.AddMass(massIncrement);
    }

    #endregion
}
