using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Cheats : MonoBehaviour
{
    [SerializeField] List<CheatBinding> cheats = new();

    private Dictionary<InputAction, UnityEvent> map = new();

    void Awake()
    {
        foreach (CheatBinding bind in cheats)
        {
            map.Add(bind.action.action, bind.effect);
        }
    }

    private void OnEnable()
    {
        foreach (CheatBinding bind in cheats)
        {
            if (bind.action != null) bind.action.action.performed += OnCheatperfomed;
        }
    }

    private void OnDisable()
    {
        foreach (CheatBinding bind in cheats)
        {
            if (bind.action != null) bind.action.action.performed -= OnCheatperfomed;
        }
    }

    private void OnCheatperfomed(InputAction.CallbackContext context)
    {
        map.TryGetValue(context.action, out UnityEvent action);
        if (action != null) action.Invoke();
    }
}

[System.Serializable]
class CheatBinding
{
    public InputActionReference action;
    public UnityEvent effect;
}