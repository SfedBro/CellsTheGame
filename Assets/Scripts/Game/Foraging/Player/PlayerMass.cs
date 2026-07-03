using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMass : MonoBehaviour
{
    #region fields

    [Header("Player Mass")]
    [SerializeField] private PlayerController player;
    [SerializeField] private int resourcesInMassUnit = 10;
    private ResourcesManager rm = ResourcesManager.instance;
    private Dictionary<ItemType, int> resources = new();
    private int totalResources = 0;
    private int massAddition;

    #endregion


    #region initialization
    void Awake()
    {
        resourceRecalculation();
    }

    void OnEnable()
    {
        rm.Subscrive(addResource);
    }

    void OnDisable()
    {
        rm.Unsubscrive(addResource);
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
