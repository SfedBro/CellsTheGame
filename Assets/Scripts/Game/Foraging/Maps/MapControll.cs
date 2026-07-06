using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapControll : MonoBehaviour, IPausable, IGameService
{
    #region fields

    [Header("References")]
    [SerializeField] private ForagingManager foragingManager;
    [SerializeField] private PauseController pauseController;
    [SerializeField] private GameObject miniMap;
    [SerializeField] private GameObject bigMap;
    private InputSystem_Actions inputActions;


    #endregion


    #region initialization

    public void InitializeService()
    {
        inputActions = foragingManager.GetInputSystem();
        miniMap.SetActive(true);
        bigMap.SetActive(false);
    }

    public void StartService()
    {
        pauseController.Subscribe(this);
    }

    void OnEnable()
    {
        inputActions.UI.OpenHideMap.performed += onMapOpenHide;
    }

    void OnDisable()
    {
        inputActions.UI.OpenHideMap.performed -= onMapOpenHide;
    }

    #endregion


    #region map open hide logic

    private bool mapClosed = true;

    private void onMapOpenHide(InputAction.CallbackContext context)
    {
        mapClosed = !mapClosed;

        miniMap.SetActive(mapClosed);
        bigMap.SetActive(!mapClosed);
    }

    public void SetPause(bool pause)
    {
        if (pause)
        {
            miniMap.SetActive(true);
            bigMap.SetActive(false);
            enabled = false;
        } else
        {
            miniMap.SetActive(mapClosed);
            bigMap.SetActive(!mapClosed);
            enabled = true;
        }
    }

    #endregion
}
