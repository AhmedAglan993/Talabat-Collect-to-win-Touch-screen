using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Master state machine for the kiosk experience.
/// Single-scene mode: each "screen" is a Canvas (or root GameObject); transitions hide the current
/// canvas and show the next. Wire all four roots in the Inspector.
///
/// State flow:
///   Registration → order-hunt canvas (MiniGame state) → QRCode
/// </summary>
[DefaultExecutionOrder(-100)]
public class KioskGameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────
    public static KioskGameManager Instance { get; private set; }

    // ── Shared session (passed between states) ────────────────────────────
    public static PlayerSession CurrentSession { get; private set; } = new PlayerSession();

    // ── State ────────────────────────────────────────────────────────────
    public enum KioskState { Registration, MiniGame, QRCode }
    public static KioskState State { get; private set; } = KioskState.Registration;

    // ── Canvases / screen roots (same scene) ───────────────────────────────
    [Header("Screen roots (one scene, toggle active)")]
    [SerializeField] private GameObject registrationCanvas;
    [SerializeField] private GameObject miniGameCanvas;
    [SerializeField] private GameObject qrCodeCanvas;

    // ── Idle watchdog ────────────────────────────────────────────────────
    private float _idleTimer;
    private bool _idleWatchdogActive;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyCanvasVisibility();
    }

    private void Start()
    {
        ApplyCanvasVisibility();
    }

    private void Update()
    {
        if (!_idleWatchdogActive) return;

        if (Input.anyKey || Input.touchCount > 0)
            _idleTimer = 0f;

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= KioskConfig.IdleTimeoutSeconds)
            GoToRegistration();
    }

    // ── State transitions (call from anywhere) ────────────────────────────

    /// <summary>
    /// Shows the registration canvas.
    /// </summary>
    public static void GoToRegistration()
    {
        SetState(KioskState.Registration, idle: true);
    }

    /// <summary>
    /// Call after registration API succeeds. Shows the order-hunt canvas (<see cref="KioskState.MiniGame"/>).
    /// </summary>
    public static void GoToMiniGame()
    {
        SetState(KioskState.MiniGame, idle: false);
    }

    /// <summary>
    /// Shows the QR canvas. Session score/duration should already be set (e.g. by <see cref="OrderHuntManager"/>).
    /// </summary>
    public static void GoToQRCode()
    {
        SetState(KioskState.QRCode, idle: false);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private static void SetState(KioskState next, bool idle)
    {
        State = next;
        if (Instance == null) return;

        Instance._idleWatchdogActive = idle;
        Instance._idleTimer = 0f;
        Instance.ApplyCanvasVisibility();
    }

    private void ApplyCanvasVisibility()
    {
        SetActiveIfAssigned(registrationCanvas, State == KioskState.Registration);
        SetActiveIfAssigned(miniGameCanvas, State == KioskState.MiniGame);
        SetActiveIfAssigned(qrCodeCanvas, State == KioskState.QRCode);
    }
    public void Reload()
    {
        State = KioskState.Registration;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private static void SetActiveIfAssigned(GameObject root, bool visible)
    {
        if (root == null) return;
        root.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
