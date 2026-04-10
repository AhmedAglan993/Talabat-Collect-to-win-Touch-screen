using System;

/// <summary>
/// Shared runtime data model passed across all kiosk states.
/// Populated during Registration, enriched after API response,
/// consumed by QRCodeManager to build the AR deep-link URL.
/// </summary>
[Serializable]
public class PlayerSession
{
    // --- Set during Registration ---
    public string playerName;
    public string playerPhone;      // optional, extend as needed

    // --- Set after API response ---
    public string sessionId;        // optional; not all APIs return this
    public string playerId;         // optional; not all APIs return this
    /// <summary>Alias for backend "playToken" — used when building URLs if gameLink is empty.</summary>
    public string sessionToken;
    /// <summary>Full play URL from API (preferred for QR when set).</summary>
    public string gameLink;
    /// <summary>Optional data URL PNG from API (data:image/png;base64,...).</summary>
    public string qrCodeDataUrl;

    // --- Set after Mini-game ---
    public int    miniGameScore;
    public float  miniGameDuration; // seconds

    // --- Derived ---
    /// <summary>
    /// Full URL for the AR / web game. Uses <see cref="gameLink"/> from the API when present.
    /// </summary>
    public string ArDeepLinkUrl =>
        !string.IsNullOrEmpty(gameLink)
            ? gameLink
            : $"{KioskConfig.ArBaseUrl}?s={Uri.EscapeDataString(sessionToken)}" +
              $"&name={Uri.EscapeDataString(playerName)}" +
              $"&pid={Uri.EscapeDataString(playerId)}";

    public void Reset()
    {
        playerName     = string.Empty;
        playerPhone    = string.Empty;
        sessionId      = string.Empty;
        playerId       = string.Empty;
        sessionToken   = string.Empty;
        gameLink       = string.Empty;
        qrCodeDataUrl  = string.Empty;
        miniGameScore  = 0;
        miniGameDuration = 0f;
    }
}
