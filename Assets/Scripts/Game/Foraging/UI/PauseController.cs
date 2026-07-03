using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private ForagingManager foragingManager;
    private List<IPausable> pausables = new();
    private InputSystem_Actions inputActions;

    private bool isPaused = false;

    #endregion


    #region initialization

    void Awake()
    {
        inputActions = foragingManager.GetInputSystem();
    }

    void OnEnable()
    {
        inputActions.UI.Pause.performed += onPause;
    }

    void OnDisable()
    {
        inputActions.UI.Pause.performed -= onPause;
    }

    #endregion


    #region pause logic

    private void onPause(InputAction.CallbackContext context)
    {
        isPaused = !isPaused;

        Time.timeScale = isPaused? 0f : 1f;

        foreach (IPausable pausable in pausables)
        {
            pausable.SetPause(isPaused);
        }
    }

    public void Resume()
    {
        isPaused = false;

        Time.timeScale = isPaused? 0f : 1f;

        foreach (IPausable pausable in pausables)
        {
            pausable.SetPause(isPaused);
        }
    }

    #endregion


    #region subscribe pattern

    public void Subscribe(IPausable pausable)
    {
        pausables.Add(pausable);
        pausable.SetPause(isPaused);
    }

    public void Unsubscribe(IPausable pausable)
    {
        pausables.Remove(pausable);
    }

    #endregion
}


public interface IPausable
{
    public void SetPause(bool pause);
}