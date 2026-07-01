using UnityEngine;

public class FactoryStateManager : MonoBehaviour, IGameService
{
    public static FactoryStateManager Instance { get; private set; }

    public enum FactoryState
    {
        None,
        BuildMode,
        EditMode
    }

    private FactoryState currentState = FactoryState.None;
    public FactoryState CurrentState => currentState;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void InitializeService()
    {
    }

    public void StartService()
    {
    }

    private void Update()
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused) return;

        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentState != FactoryState.None)
            {
                bool cleared = BuildManager.Instance.ClearAll();
                
                SetState(FactoryState.None);
            }
            else
            {
                bool cleared = BuildManager.Instance.ClearAll();
                if (!cleared && PauseMenu.Instance != null)
                {
                    PauseMenu.Instance.Toggle();
                }
            }
        }

        if (inputActions.Factory.ToggleBuildMode.WasPressedThisFrame())
        {
            if (currentState == FactoryState.BuildMode)
            {
                SetState(FactoryState.None);
            }
            else
            {
                SetState(FactoryState.BuildMode);
            }
        }
    }

    public void SetState(FactoryState newState)
    {
        if (currentState == newState) return;

        if (currentState == FactoryState.EditMode)
        {
            BuildManager.Instance.ClearHistory();
        }

        currentState = newState;
        Debug.Log($"Factory State changed to: {currentState}");

        BuildManager.Instance.ClearAll();

        if (currentState == FactoryState.BuildMode)
        {
            if (BuildMenuWindow.Instance != null) BuildMenuWindow.Instance.Open();
        }
        else
        {
            if (BuildMenuWindow.Instance != null) BuildMenuWindow.Instance.Close();
        }
    }

    public void OnBuildUIButtonClicked()
    {
        if (currentState == FactoryState.BuildMode) SetState(FactoryState.None);
        else SetState(FactoryState.BuildMode);
    }

    public void OnEditUIButtonClicked()
    {
        if (currentState == FactoryState.EditMode) SetState(FactoryState.None);
        else SetState(FactoryState.EditMode);
    }
}
