using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Registration scene.
///
/// Wire up in Inspector:
///   nameInputField     → TMP_InputField  (player name)
///   phoneInputField    → TMP_InputField  (optional phone / email)
///   submitButton       → Button
///   errorLabel         → TMP_Text        (validation / API errors)
///   loadingOverlay     → GameObject      (spinner panel, disabled by default)
///
/// The scene should also have a UIAnimator for entrance/exit transitions.
/// </summary>
public class RegistrationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text errorLabel;

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        errorLabel.text = string.Empty;
        submitButton.onClick.AddListener(OnSubmitClicked);

        // Auto-open keyboard on kiosk touch screen
        nameInputField.Select();
        nameInputField.ActivateInputField();
    }

    // ── Submit ─────────────────────────────────────────────────────────────

    private void OnSubmitClicked()
    {
        if (!ValidateForm()) return;
        StartCoroutine(RegisterRoutine());
    }

    private IEnumerator RegisterRoutine()
    {
        SetLoading(true);

        var payload = new ApiService.RegisterRequest
        {
            name = nameInputField.text.Trim(),
        };

        bool done = false;

        yield return ApiService.RegisterPlayer(
             payload,
             onSuccess: data =>
             {

                 print("sssssssssssssssssssssssssssss");
                 // Populate shared session from { "data": { username, playToken, gameLink, ... } }
                 var s = KioskGameManager.CurrentSession;
                 s.playerName = !string.IsNullOrEmpty(data.username) ? data.username : payload.name;
                 s.sessionToken = data.playToken ?? string.Empty;
                 s.gameLink = data.gameLink ?? string.Empty;
                 s.qrCodeDataUrl = data.qrCodeDataUrl ?? string.Empty;
                 KioskGameManager.GoToMiniGame();

             },
             onError: err =>
             {
                 // ApiService passes API error.message when JSON matches { "error": { "message", "code" } }
                 ShowError(string.IsNullOrEmpty(err) ? "Could not register, please try again." : err);
                 Debug.LogWarning($"[Registration] {err}");
                 SetLoading(false);
             }
         );

    }

    // ── Validation ─────────────────────────────────────────────────────────

    private bool ValidateForm()
    {
        string name = nameInputField.text.Trim();

        if (name.Length < KioskConfig.MinNameLength)
        {
            ShowError($"Please enter at least {KioskConfig.MinNameLength} characters.");
            return false;
        }

        errorLabel.text = string.Empty;
        return true;
    }

    // ── UI helpers ─────────────────────────────────────────────────────────

    private void ShowError(string msg)
    {
        errorLabel.text = msg;
    }

    private void SetLoading(bool active)
    {
        submitButton.interactable = !active;
        nameInputField.interactable = !active;

    }
}
