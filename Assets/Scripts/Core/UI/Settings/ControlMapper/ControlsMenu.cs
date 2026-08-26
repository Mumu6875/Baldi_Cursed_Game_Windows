using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControlsMenu : MonoBehaviour
{
    private void Awake() => allKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    private void Start()
    {
        inputMan = Singleton<InputManager>.Instance;
        if (inputMan != null)
        {
            UpdateAllButtonTexts();
        }
    }

    private void OnEnable()
    {
        if (inputMan == null)
        {
            inputMan = Singleton<InputManager>.Instance;
        }

        if (inputMan == null)
        {
            Debug.LogError("ControlsMenu: InputManager not found! Is it in the scene?");
            return;
        }

        Debug.Log($"ControlsMenu: InputManager found. Mappings count: {inputMan.KeyboardMapping.Count}");
        UpdateAllButtonTexts();
    }

    private void OnDisable() => inputMan.Save();

    public void UpdateAllButtonTexts()
    {
        for (int i = 0; i < labelTexts.Length; i++)
        {
            if (i < Enum.GetValues(typeof(InputAction)).Length)
            {
                InputAction action = (InputAction)i;
                if (inputMan.KeyboardMapping.TryGetValue(action, out InputBinding binding))
                {
                    buttonTexts[i].text = IsMouseKey(binding.primaryKey) ? FormatMouseText(binding.primaryKey) : FormatKeyText(binding.primaryKey);
                    secondaryButtonTexts[i].text = IsMouseKey(binding.secondaryKey) ? FormatMouseText(binding.secondaryKey) : FormatKeyText(binding.secondaryKey);
                }
            }
        }
    }

    private string FormatKeyText(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Escape: return "ESC";
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

    private string FormatMouseText(KeyCode mouseKey)
    {
        switch (mouseKey)
        {
            case KeyCode.Mouse0: return "Left Mouse Button";
            case KeyCode.Mouse1: return "Right Mouse Button";
            case KeyCode.Mouse2: return "Middle Mouse Button";
            default: return mouseKey.ToString();
        }
    }

    public void StartRebindKeyboard(int actionIndex)
    {
        if (panel.activeSelf || confirmationPanel.activeSelf)
        {
            return;
        }

        if (actionIndex < Enum.GetValues(typeof(InputAction)).Length)
        {
            currentActionIndex = actionIndex;
            isBindingMouseInput = false;

            confirmationPanel.SetActive(true);
            SetButtonsInteractable(false);

            confirmText.text = $"{labelTexts[currentActionIndex].text}\n\n{buttonTexts[currentActionIndex].text}";
        }
    }

    public void StartRebindMouse(int actionIndex)
    {
        if (panel.activeSelf || confirmationPanel.activeSelf)
        {
            return;
        }

        if (actionIndex < Enum.GetValues(typeof(InputAction)).Length)
        {
            currentActionIndex = actionIndex;
            isBindingMouseInput = true;

            confirmationPanel.SetActive(true);
            SetButtonsInteractable(false);

            confirmText.text = $"{labelTexts[currentActionIndex].text}\n\n{secondaryButtonTexts[currentActionIndex].text}";
        }
    }

    public void ShowResetConfirmation()
    {
        resetConfirmationPanel.SetActive(true);
        SetButtonsInteractable(false);
    }

    public void ResetKey()
    {
        if (!isBindingMouseInput)
        {
            inputMan.ClearPrimaryKey((InputAction)currentActionIndex);
        }
        else
        {
            inputMan.ClearSecondaryKey((InputAction)currentActionIndex);
        }

        inputMan.Save();
        UpdateAllButtonTexts();
        CancelRebind();
    }

    public void ConfirmRebind()
    {
        confirmationPanel.SetActive(false);
        panel.SetActive(true);

        if (rebindCoroutine != null)
        {
            StopCoroutine(rebindCoroutine);
        }

        rebindCoroutine = StartCoroutine(RebindCoroutine(5f));
    }

    public void ConfirmReset()
    {
        resetConfirmationPanel.SetActive(false);
        ResetToDefaults();
        SetButtonsInteractable(true);
    }

    public void CancelRebind()
    {
        confirmationPanel.SetActive(false);

        if (rebindCoroutine != null)
        {
            StopCoroutine(rebindCoroutine);
        }

        SetButtonsInteractable(true);
    }

    public void CancelReset()
    {
        resetConfirmationPanel.SetActive(false);
        SetButtonsInteractable(true);
    }

    private IEnumerator RebindCoroutine(float duration)
    {
        string actionName = labelTexts[currentActionIndex].text;
        float remainingTime = duration;
        counterText.text = $"<size=16>{actionName}<size=21>\nPress a key to assign it to {actionName}\n\n\n\n\n\n{Mathf.CeilToInt(remainingTime)}";

        while (Input.anyKey)
        {
            yield return null;
        }

        yield return null;

        while (remainingTime > 0f)
        {
            counterText.text = $"<size=16>{actionName}<size=21>\nPress a key to assign it to {actionName}\n\n\n\n\n\n{Mathf.CeilToInt(remainingTime)}";

            if (GetPressedInput(out KeyCode pressedKey))
            {
                if (isBindingMouseInput && IsMouseKey(pressedKey))
                {
                    inputMan.Modify((InputAction)currentActionIndex, KeyCode.None, pressedKey);
                }
                else if (!isBindingMouseInput && !IsMouseKey(pressedKey))
                {
                    inputMan.Modify((InputAction)currentActionIndex, pressedKey);
                }

                remainingTime = 0.01f;
            }

            remainingTime -= Time.unscaledDeltaTime;
            yield return null;
        }

        panel.SetActive(false);
        rebindCoroutine = null;
        isBindingMouseInput = false;

        SetButtonsInteractable(true);

        inputMan.Save();
        UpdateAllButtonTexts();
    }

    private bool GetPressedInput(out KeyCode pressedKey)
    {
        pressedKey = KeyCode.None;

        if (isBindingMouseInput)
        {
            for (int i = 0; i < 7; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    pressedKey = (KeyCode)((int)KeyCode.Mouse0 + i);
                    return true;
                }
            }
        }
        else if (Input.anyKeyDown)
        {
            foreach (var keyCode in allKeyCodes)
            {
                if (Input.GetKeyDown(keyCode) && !IsMouseKey(keyCode))
                {
                    pressedKey = keyCode;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsMouseKey(KeyCode key)
    {
        return key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2 || key == KeyCode.Mouse3 || key == KeyCode.Mouse4;
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var button in allButtons)
        {
            button.interactable = state;
        }
    }

    public void ResetToDefaults()
    {
        SetButtonsInteractable(true);
        inputMan.SetDefaults();
        UpdateAllButtonTexts();
    }

    [Header("UI Elements")]
    [SerializeField] private Button[] allButtons;
    [SerializeField] private TMP_Text[] buttonTexts, secondaryButtonTexts, labelTexts;

    [Header("UI Panels")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject confirmationPanel, resetConfirmationPanel;

    [Header("Counter Text")]
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text confirmText;

    private int currentActionIndex;
    private Coroutine rebindCoroutine;
    private KeyCode[] allKeyCodes;
    private bool isBindingMouseInput;
    private InputManager inputMan;
}