using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI transition helper.
/// Attach to the root Canvas CanvasGroup in each scene for fade in/out.
/// Also drives the Attract screen idle animation loop.
///
/// Wire up in Inspector:
///   canvasGroup    → CanvasGroup on the scene root canvas
///   attractLoop    → GameObject  (attract animation root, Attract scene only)
///   startButton    → Button      (CTA on Attract screen, optional)
///
/// Usage from other scripts:
///   UIAnimator.Instance.FadeOut(onComplete: () => { ... });
/// </summary>
public class UIAnimator : MonoBehaviour
{
    public static UIAnimator Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject  attractLoop;   // null in non-attract scenes
    [SerializeField] private Button      startButton;

    [Header("Transition")]
    [SerializeField] private float fadeInDuration  = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        // Start invisible and fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(Fade(0f, 1f, fadeInDuration, null));
        }
    }

    void Start()
    {

        if (attractLoop != null)
            StartCoroutine(AttractPulse());
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void FadeOut(System.Action onComplete = null)
    {
        StartCoroutine(Fade(canvasGroup.alpha, 0f, fadeOutDuration, onComplete));
    }

    // ── Internal ──────────────────────────────────────────────────────────

  
    private IEnumerator Fade(float from, float to, float duration, System.Action onComplete)
    {
        if (canvasGroup == null) { onComplete?.Invoke(); yield break; }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Simple attract loop — pulses the attract animation root to draw attention.
    /// Replace with your actual attract animation/video trigger.
    /// </summary>
    private IEnumerator AttractPulse()
    {
        while (true)
        {
            // TODO: trigger your attract Animator or video player here
            yield return new WaitForSeconds(5f);
        }
    }
}
