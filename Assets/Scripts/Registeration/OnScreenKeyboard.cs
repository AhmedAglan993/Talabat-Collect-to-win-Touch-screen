using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Custom on-screen keyboard: opens when any <see cref="TMP_InputField"/> is focused,
/// stays open while the user taps keys (re-focuses the field so Unity does not treat it as "done typing"),
/// and closes on Done or when focus truly leaves the field + keyboard area.
/// </summary>
[DefaultExecutionOrder(-50)]
public class OnScreenKeyboard : MonoBehaviour
{
    [Header("Keyboard Root")]
    [SerializeField] private GameObject keyboardPanel;

    [Header("Keys")]
    [SerializeField] private Button[] keyButtons;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button spaceButton;
    [SerializeField] private Button doneButton;

    [Header("Input fields")]
    [Tooltip("Optional explicit fields. If empty, auto-discover is used.")]
    [SerializeField] private List<TMP_InputField> explicitInputs = new List<TMP_InputField>();

    [Tooltip("Find every TMP_InputField in the scene (including on inactive objects).")]
    [SerializeField] private bool autoDiscoverAllInputs = true;

    private TMP_InputField _activeInput;

    /// <summary>
    /// While true, <see cref="OnFieldDeselected"/> does not start a coroutine (e.g. during <see cref="HideKeyboard"/>).
    /// </summary>
    private bool _suppressDeselectHandling;

    private readonly List<TMP_InputField> _tracked = new List<TMP_InputField>();
    private readonly Dictionary<TMP_InputField, UnityAction<string>> _onSelect = new Dictionary<TMP_InputField, UnityAction<string>>();
    private readonly Dictionary<TMP_InputField, UnityAction<string>> _onDeselect = new Dictionary<TMP_InputField, UnityAction<string>>();

    private void Awake()
    {
        if (keyboardPanel != null)
            keyboardPanel.SetActive(false);

        WireKeyButtons();
        RefreshRegisteredInputs();
    }

    private void OnDestroy()
    {
        ClearInputSubscriptions();
    }

    /// <summary>
    /// Call after adding/removing input fields at runtime (e.g. canvas enabled later).
    /// </summary>
    public void RefreshRegisteredInputs()
    {
        ClearInputSubscriptions();

        if (explicitInputs != null)
        {
            foreach (var f in explicitInputs)
                SubscribeField(f);
        }

        if (autoDiscoverAllInputs)
        {
            var found = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var f in found)
            {
                if (f != null && !_tracked.Contains(f))
                    SubscribeField(f);
            }
        }
    }

    private void WireKeyButtons()
    {
        if (keyButtons != null)
        {
            foreach (Button btn in keyButtons)
            {
                if (btn == null) continue;
                string key = btn.GetComponentInChildren<TMP_Text>()?.text ?? string.Empty;
                btn.onClick.AddListener(() => TypeKey(key));
            }
        }

        if (backspaceButton != null) backspaceButton.onClick.AddListener(Backspace);
        if (spaceButton != null) spaceButton.onClick.AddListener(() => TypeKey(" "));
        if (doneButton != null) doneButton.onClick.AddListener(HideKeyboard);
    }

    private void SubscribeField(TMP_InputField f)
    {
        if (f == null || _tracked.Contains(f)) return;

        UnityAction<string> sel = _ => OnFieldSelected(f);
        UnityAction<string> des = _ => OnFieldDeselected(f);

        f.onSelect.AddListener(sel);
        f.onDeselect.AddListener(des);

        _onSelect[f] = sel;
        _onDeselect[f] = des;
        _tracked.Add(f);
    }

    private void ClearInputSubscriptions()
    {
        foreach (var f in _tracked)
        {
            if (f == null) continue;
            if (_onSelect.TryGetValue(f, out var sel))
                f.onSelect.RemoveListener(sel);
            if (_onDeselect.TryGetValue(f, out var des))
                f.onDeselect.RemoveListener(des);
        }

        _tracked.Clear();
        _onSelect.Clear();
        _onDeselect.Clear();
    }

    private void OnFieldSelected(TMP_InputField field)
    {
        _activeInput = field;
        if (keyboardPanel != null)
            keyboardPanel.SetActive(true);
    }

    private void OnFieldDeselected(TMP_InputField field)
    {
        if (_suppressDeselectHandling) return;
        if (!isActiveAndEnabled) return;

        StartCoroutine(DeselectAfterFrame(field));
    }

    /// <summary>
    /// After a key/button click, the input often deselects first; we wait one frame then
    /// either re-focus the same field (if the user tapped the keyboard) or hide the keyboard.
    /// </summary>
    private IEnumerator DeselectAfterFrame(TMP_InputField field)
    {
        yield return null;

        if (!isActiveAndEnabled) yield break;

        var es = EventSystem.current;
        GameObject sel = es != null ? es.currentSelectedGameObject : null;

        if (keyboardPanel != null && sel != null && sel.transform.IsChildOf(keyboardPanel.transform))
        {
            if (_activeInput == null) _activeInput = field;
            RefocusActiveField();
            yield break;
        }

        var otherField = sel != null ? sel.GetComponentInParent<TMP_InputField>() : null;
        if (otherField != null && otherField != field)
        {
            _activeInput = otherField;
            if (keyboardPanel != null) keyboardPanel.SetActive(true);
            yield break;
        }

        if (sel == field.gameObject)
            yield break;

        _activeInput = null;
        if (keyboardPanel != null)
            keyboardPanel.SetActive(false);
    }

    private void TypeKey(string key)
    {
        if (_activeInput == null) return;
        if (string.IsNullOrEmpty(key)) return;

        _activeInput.text += key;
        RefocusActiveField();
    }

    private void Backspace()
    {
        if (_activeInput == null || _activeInput.text.Length == 0) return;
        _activeInput.text = _activeInput.text[..^1];
        RefocusActiveField();
    }

    /// <summary>
    /// Keeps caret in the field so the OS / UI does not treat typing as finished.
    /// </summary>
    private void RefocusActiveField()
    {
        if (_activeInput == null) return;

        _activeInput.ActivateInputField();
        int len = _activeInput.text.Length;
        _activeInput.caretPosition = len;
        _activeInput.selectionAnchorPosition = len;
        _activeInput.selectionFocusPosition = len;
    }

    /// <summary>
    /// Hides the keyboard and clears UI selection (user pressed Done).
    /// Clears selection before deactivating the panel so <see cref="onDeselect"/> does not start a coroutine on an inactive component.
    /// </summary>
    public void HideKeyboard()
    {
        _suppressDeselectHandling = true;
        _activeInput = null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (keyboardPanel != null)
            keyboardPanel.SetActive(false);

        _suppressDeselectHandling = false;
    }
}
