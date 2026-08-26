using System;
using UnityEngine;
using System.Collections.Generic;

public enum InputAction
{
    MoveLeft = 0,
    MoveRight = 1,
    MoveForward = 2,
    MoveBackward = 3,
    Interact = 4,
    UseItem = 5,
    Slot0 = 6,
    Slot1 = 7,
    Slot2 = 8,
    Run = 9,
    LookBehind = 10,
    Jump = 11,
    PauseOrCancel = 12,
    Count
}

[Serializable]
public struct InputBinding
{
    public KeyCode primaryKey, secondaryKey;

    public InputBinding(KeyCode primary, KeyCode secondary = KeyCode.None)
    {
        primaryKey = primary;
        secondaryKey = secondary;
    }
}

[Serializable]
public struct SimulatedKeyState
{
    public bool currentFrame, previousFrame;
}

public class InputManager : Singleton<InputManager>
{
    public Dictionary<InputAction, InputBinding> KeyboardMapping = new Dictionary<InputAction, InputBinding>();

    private Dictionary<KeyCode, SimulatedKeyState> simulatedKeys = new Dictionary<KeyCode, SimulatedKeyState>();

    private bool[] currentKeyStates = new bool[(int)InputAction.Count], previousKeyStates = new bool[(int)InputAction.Count];

    private const string SavePrefix = "InputBinding_";

    public Dictionary<InputAction, InputBinding> Mappings
    {
        get
        {
            if (KeyboardMapping == null || KeyboardMapping.Count == 0)
            {
                Load();
            }
            return KeyboardMapping;
        }
    }

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Update()
    {
        bool[] tempStates = previousKeyStates;
        previousKeyStates = currentKeyStates;
        currentKeyStates = tempStates;

        UpdateSimulatedKeys();

        for (int i = 0; i < (int)InputAction.Count; i++)
        {
            if (!KeyboardMapping.TryGetValue((InputAction)i, out InputBinding binding))
            {
                continue;
            }

            bool keyDown = (binding.primaryKey != KeyCode.None && GetKeyState(binding.primaryKey)) || (binding.secondaryKey != KeyCode.None && GetKeyState(binding.secondaryKey));
            currentKeyStates[i] = keyDown;
        }
    }

    private void UpdateSimulatedKeys()
    {
        if (simulatedKeys.Count == 0)
        {
            return;
        }

        var keys = new List<KeyCode>(simulatedKeys.Keys);
        foreach (var key in keys)
        {
            var state = simulatedKeys[key];
            state.previousFrame = state.currentFrame;
            state.currentFrame = false;
            simulatedKeys[key] = state;

            if (!state.currentFrame && !state.previousFrame)
            {
                simulatedKeys.Remove(key);
            }
        }
    }

    public void SimulateKey(KeyCode key)
    {
        if (!simulatedKeys.TryGetValue(key, out var state))
        {
            state = new SimulatedKeyState();
        }

        state.currentFrame = true;
        simulatedKeys[key] = state;
    }

    private bool GetKeyState(KeyCode key)
    {
        if (simulatedKeys.TryGetValue(key, out var state) && state.currentFrame)
        {
            return true;
        }

        return Input.GetKey(key);
    }

    public bool GetActionKey(InputAction action) => currentKeyStates[(int)action];

    public bool GetActionKeyDown(InputAction action) => currentKeyStates[(int)action] && !previousKeyStates[(int)action];

    public bool GetActionKeyUp(InputAction action) => !currentKeyStates[(int)action] && previousKeyStates[(int)action];

    public void ClearPrimaryKey(InputAction action)
    {
        if (KeyboardMapping.TryGetValue(action, out var binding))
        {
            binding.primaryKey = KeyCode.None;
            KeyboardMapping[action] = binding;
        }
    }

    public void ClearSecondaryKey(InputAction action)
    {
        if (KeyboardMapping.TryGetValue(action, out var binding))
        {
            binding.secondaryKey = KeyCode.None;
            KeyboardMapping[action] = binding;
        }
    }

    public string ConvertInputActionToString(InputAction action)
    {
        if (!KeyboardMapping.TryGetValue(action, out var binding))
        {
            return action.ToString();
        }

        if (binding.primaryKey != KeyCode.None)
        {
            return KeyCodeToDisplayString(binding.primaryKey);
        }

        if (binding.secondaryKey != KeyCode.None)
        {
            return KeyCodeToDisplayString(binding.secondaryKey);
        }

        return action.ToString();
    }

    public void Modify(InputAction action, KeyCode newKey, KeyCode secondaryKey = KeyCode.None)
    {
        if (!KeyboardMapping.TryGetValue(action, out var binding))
        {
            binding = new InputBinding();
        }

        if (newKey != KeyCode.None)
        {
            binding.primaryKey = newKey;
        }

        if (secondaryKey != KeyCode.None)
        {
            binding.secondaryKey = secondaryKey;
        }

        KeyboardMapping[action] = binding;
    }

    public void Save()
    {
        foreach (var pair in KeyboardMapping)
        {
            string key = SavePrefix + pair.Key;
            string value = $"{(int)pair.Value.primaryKey},{(int)pair.Value.secondaryKey}";
            PlayerPrefs.SetString(key, value);
        }
        PlayerPrefs.Save();
    }

    public void Load()
    {
        KeyboardMapping.Clear();

        for (int i = 0; i < (int)InputAction.Count; i++)
        {
            InputAction action = (InputAction)i;
            string key = SavePrefix + action;

            if (PlayerPrefs.HasKey(key))
            {
                string[] parts = PlayerPrefs.GetString(key).Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int primary) && int.TryParse(parts[1], out int secondary))
                {
                    KeyboardMapping[action] = new InputBinding((KeyCode)primary, (KeyCode)secondary);
                    continue;
                }
            }

            KeyboardMapping[action] = GetDefaultBinding(action);
        }
    }

    public void SetDefaults()
    {
        KeyboardMapping.Clear();

        for (int i = 0; i < (int)InputAction.Count; i++)
        {
            var action = (InputAction)i;
            KeyboardMapping[action] = GetDefaultBinding(action);
        }

        Save();
    }

    public string KeyCodeToDisplayString(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Escape: return "ESC";

            case KeyCode.Mouse0: return "Left Mouse Button";
            case KeyCode.Mouse1: return "Right Mouse Button";
            case KeyCode.Mouse2: return "Middle Mouse Button";

            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";

            case KeyCode.None: return "";
            default: return key.ToString();
        }
    }

    private InputBinding GetDefaultBinding(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveLeft: return new InputBinding(KeyCode.A);
            case InputAction.MoveRight: return new InputBinding(KeyCode.D);
            case InputAction.MoveForward: return new InputBinding(KeyCode.W);
            case InputAction.MoveBackward: return new InputBinding(KeyCode.S);
            case InputAction.Interact: return new InputBinding(KeyCode.None, KeyCode.Mouse0);
            case InputAction.UseItem: return new InputBinding(KeyCode.None, KeyCode.Mouse1);
            case InputAction.Slot0: return new InputBinding(KeyCode.Alpha1);
            case InputAction.Slot1: return new InputBinding(KeyCode.Alpha2);
            case InputAction.Slot2: return new InputBinding(KeyCode.Alpha3);
            case InputAction.Run: return new InputBinding(KeyCode.LeftShift);
            case InputAction.LookBehind: return new InputBinding(KeyCode.Space);
            case InputAction.Jump: return new InputBinding(KeyCode.Space, KeyCode.None);
            case InputAction.PauseOrCancel: return new InputBinding(KeyCode.Escape);
            default: return new InputBinding(KeyCode.None);
        }
    }
}
