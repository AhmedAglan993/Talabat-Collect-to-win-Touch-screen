using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the QR image from <see cref="PlayerSession.qrCodeDataUrl"/> (data:image/png;base64,...)
/// when the QR canvas is shown. Assign a <see cref="RawImage"/> in the Inspector.
/// </summary>
public class QrCodeScreenView : MonoBehaviour
{
    [Header("QR")]
    [SerializeField] private RawImage qrRawImage;

    [Header("Optional")]
    [SerializeField] private TMP_Text urlDebugLabel;
    [SerializeField] private TMP_Text welcomeLabel;

    private Texture2D _runtimeTexture;

    private void OnEnable()
    {
        var session = KioskGameManager.CurrentSession;

        if (welcomeLabel != null && !string.IsNullOrEmpty(session.playerName))
            welcomeLabel.text = $"Well done, {session.playerName}!";

        if (urlDebugLabel != null)
            urlDebugLabel.text = string.IsNullOrEmpty(session.gameLink) ? session.ArDeepLinkUrl : session.gameLink;

        ApplyQrFromSession(session);
    }

    private void OnDestroy()
    {
        ClearTexture();
    }

    /// <summary>
    /// Re-reads session and refreshes the QR texture (e.g. after registration in same session).
    /// </summary>
    public void RefreshFromSession()
    {
        ApplyQrFromSession(KioskGameManager.CurrentSession);
    }

    private void ApplyQrFromSession(PlayerSession session)
    {
        if (qrRawImage == null) return;

        ClearTexture();

        if (TryDecodeDataUrlToTexture(session.qrCodeDataUrl, out Texture2D tex))
        {
            _runtimeTexture = tex;
            qrRawImage.texture = tex;
            qrRawImage.enabled = true;
        }
        else
        {
            qrRawImage.texture = null;
            qrRawImage.enabled = false;
        }
    }

    private void ClearTexture()
    {
        if (qrRawImage != null)
        {
            qrRawImage.texture = null;
        }

        if (_runtimeTexture != null)
        {
            Destroy(_runtimeTexture);
            _runtimeTexture = null;
        }
    }

    /// <summary>
    /// Parses a data URL (e.g. data:image/png;base64,...) into a PNG texture.
    /// </summary>
    private static bool TryDecodeDataUrlToTexture(string dataUrl, out Texture2D texture)
    {
        texture = null;
        if (string.IsNullOrEmpty(dataUrl)) return false;

        const string marker = "base64,";
        int idx = dataUrl.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return false;

        try
        {
            string b64 = dataUrl.Substring(idx + marker.Length).Trim();
            byte[] bytes = Convert.FromBase64String(b64);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                Destroy(tex);
                return false;
            }

            texture = tex;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QrCodeScreenView] Failed to decode qrCodeDataUrl: {ex.Message}");
            return false;
        }
    }
}
