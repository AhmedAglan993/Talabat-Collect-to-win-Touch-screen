using System.Collections;
using UnityEngine;

/// <summary>
/// Simple UI shake for a RectTransform (recommended to shake the UI root).
/// </summary>
public class UIShake : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float durationSeconds = 0.25f;
    [SerializeField] private float magnitudePixels = 18f;
    [SerializeField] private int vibrato = 18;

    private Coroutine _routine;
    private Vector2 _originalAnchoredPos;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
        if (target != null) _originalAnchoredPos = target.anchoredPosition;
    }

    /// <summary>
    /// Plays a shake animation. Safe to call repeatedly.
    /// </summary>
    public void Shake()
    {
        if (target == null) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        _originalAnchoredPos = target.anchoredPosition;

        float t = 0f;
        int steps = Mathf.Max(1, vibrato);
        float step = durationSeconds <= 0f ? 0.01f : (durationSeconds / steps);

        while (t < durationSeconds)
        {
            float x = Random.Range(-1f, 1f) * magnitudePixels;
            float y = Random.Range(-1f, 1f) * magnitudePixels;
            target.anchoredPosition = _originalAnchoredPos + new Vector2(x, y);

            yield return new WaitForSecondsRealtime(step);
            t += step;
        }

        target.anchoredPosition = _originalAnchoredPos;
        _routine = null;
    }
}

