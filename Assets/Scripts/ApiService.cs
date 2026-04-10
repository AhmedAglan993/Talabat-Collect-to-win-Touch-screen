using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Stateless HTTP service. Call via static coroutine helpers.
/// All endpoints are defined in KioskConfig.
///
/// Usage:
///   StartCoroutine(ApiService.RegisterPlayer(payload, onSuccess, onError));
/// </summary>
public static class ApiService
{
    // ── Request / Response DTOs ───────────────────────────────────────────

    [Serializable]
    public class RegisterRequest
    {
        public string name;
    }

    /// <summary>
    /// Wrapper for POST /kiosk/register JSON: { "data": { ... } }.
    /// </summary>
    [Serializable]
    public class RegisterApiEnvelope
    {
        public RegisterApiData data;
    }

    /// <summary>
    /// Inner payload from the registration API (inside "data").
    /// </summary>
    [Serializable]
    public class RegisterApiData
    {
        public string username;
        public string playToken;
        public string gameLink;
        public string qrCodeDataUrl;
        public string createdAt;
    }

    [Serializable]
    public class ScoreRequest
    {
        public string sessionId;
        public int    score;
        public float  duration;
    }

    [Serializable]
    public class ApiError
    {
        public string error;
        public int    code;
    }

    /// <summary>
    /// Error body shape: { "error": { "message": "...", "code": "USERNAME_TAKEN" } }.
    /// </summary>
    [Serializable]
    public class RegisterErrorEnvelope
    {
        public RegisterErrorNested error;
    }

    [Serializable]
    public class RegisterErrorNested
    {
        public string message;
        public string code;
    }

    // ── Public coroutines ─────────────────────────────────────────────────

    /// <summary>
    /// POST /kiosk/register — creates player + session; response body uses { "data": { username, playToken, gameLink, ... } }.
    /// </summary>
    public static IEnumerator RegisterPlayer(
        RegisterRequest payload,
        Action<RegisterApiData> onSuccess,
        Action<string> onError)
    {
        string url  = KioskConfig.BaseApiUrl + KioskConfig.RegisterEndpoint;
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept",       "application/json");

        yield return req.SendWebRequest();

        string raw = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

        // 4xx/5xx (e.g. 409 Conflict) often arrive with Result != Success; body still has JSON error.
        if (req.result != UnityWebRequest.Result.Success || req.responseCode >= 400)
        {
            long code = req.responseCode;
            if (TryParseRegisterErrorBody(raw, out string apiMessage, out string apiCode))
            {
                Debug.LogWarning($"[ApiService] POST {url} HTTP {code} ({apiCode}): {apiMessage}");
                onError?.Invoke(apiMessage);
            }
            else
            {
                string transport = string.IsNullOrEmpty(req.error) ? "Request failed" : req.error;
                Debug.LogWarning($"[ApiService] POST {url} HTTP {code}: {transport}\n{raw}");
                onError?.Invoke(RegisterFailureUserMessage(code, raw));
            }
            yield break;
        }

        try
        {
            RegisterApiEnvelope envelope = JsonUtility.FromJson<RegisterApiEnvelope>(raw);
            if (envelope == null || envelope.data == null)
            {
                onError?.Invoke("Invalid response");
                yield break;
            }

            Debug.Log(raw);
            onSuccess?.Invoke(envelope.data);
        }
        catch (Exception ex)
        {
            string msg = $"JSON parse error: {ex.Message}";
            Debug.LogError($"[ApiService] {msg}\nRaw: {raw}");
            onError?.Invoke(msg);
        }
    }

    /// <summary>
    /// PATCH /kiosk/score — submits mini-game result for leaderboard seeding.
    /// Fire-and-forget: errors are logged but don't block the QR screen.
    /// </summary>
  

    // ── Internal HTTP helpers ─────────────────────────────────────────────

    private static IEnumerator Post<T>(string url, string json,
        Action<T> onSuccess, Action<string> onError)
    {
        yield return Send(url, "POST", json, onSuccess, onError);
    }

   

    private static IEnumerator Send<T>(string url, string method, string json,
        Action<T> onSuccess, Action<string> onError)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, method)
        {
            uploadHandler   = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept",       "application/json");
        // Add auth headers here if your API needs them, e.g.:
        // req.SetRequestHeader("X-Kiosk-Key", KioskConfig.KioskApiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string msg = $"[{req.responseCode}] {req.error}";
            Debug.LogError($"[ApiService] {method} {url} failed: {msg}");
            onError?.Invoke(msg);
            yield break;
        }

        try
        {
            T response = JsonUtility.FromJson<T>(req.downloadHandler.text);
            Debug.Log(req.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        catch (Exception ex)
        {
            string msg = $"JSON parse error: {ex.Message}";
            Debug.LogError($"[ApiService] {msg}\nRaw: {req.downloadHandler.text}");
            onError?.Invoke(msg);
        }
    }

    /// <summary>
    /// Parses API JSON error payload for register endpoint.
    /// </summary>
    private static bool TryParseRegisterErrorBody(string raw, out string message, out string code)
    {
        message = null;
        code = null;
        if (string.IsNullOrEmpty(raw)) return false;

        try
        {
            RegisterErrorEnvelope env = JsonUtility.FromJson<RegisterErrorEnvelope>(raw);
            if (env?.error == null || string.IsNullOrEmpty(env.error.message)) return false;
            message = env.error.message;
            code = env.error.code;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fallback user-facing message when the body is not the expected JSON error shape.
    /// </summary>
    private static string RegisterFailureUserMessage(long httpCode, string rawBody)
    {
        if (httpCode == 409) return "This username is already taken.";
        if (!string.IsNullOrEmpty(rawBody) && rawBody.Length < 200) return rawBody;
        return "Could not register, please try again.";
    }
}
