/// <summary>
/// Central configuration. Set values here or wire them to a ScriptableObject
/// if you want per-build overrides without code changes.
/// </summary>
public static class KioskConfig
{
    // ── API ──────────────────────────────────────────────────────────────
    public const string BaseApiUrl        = "https://treasure-hunt-backend.vercel.app/api";
    public const string RegisterEndpoint  = "/auth/register";   // POST

    // ── AR deep-link ─────────────────────────────────────────────────────
    public const string ArBaseUrl         = "https://hunt.yourdomain.com";

    // ── Kiosk UX ─────────────────────────────────────────────────────────
    /// Seconds of inactivity before the kiosk resets to the attract screen.
    public const float  IdleTimeoutSeconds   = 60f;

    /// How long the QR code screen stays visible before auto-reset.
    public const float  QrScreenDurationSeconds = 90f;

    /// Minimum name length accepted by the registration form.
    public const int    MinNameLength = 2;

    // ── Mini-game ────────────────────────────────────────────────────────
    /// Total time given to complete the mini-game (seconds).
    public const float  MiniGameDuration = 60f;

    // ── Scene names (must match Build Settings) ───────────────────────────
    public const string SceneRegistration = "Registration";
    public const string SceneMiniGame     = "MiniGame";
    public const string SceneQRCode       = "QRCode";
}
