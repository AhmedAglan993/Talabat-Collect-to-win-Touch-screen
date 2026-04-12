using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIPresetAnimator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum PresetType
    {
        None, FadeIn, FadeInUp, FadeInScale, ScalePop, ScalePulse,
        SweepInLeft, SweepInRight, SweepInUp, SweepInRandom, Shake, FlashAlpha,
        ScaleBounce,
        HighlightPunch,
        GlowPulse,
        FloatWiggle,

        ClickPop,
        ClickFlash,
        ClickRotateShake,
        ClickBounceBack,

        BreathPulse,
        LoopWiggle,
        FloatBounceLoop,
        PulsateAlpha,
        MoveAroundScreen,
        CircleMotion,
        SineWaveMotion,
        DiagonalBounce, 
        LogoIntroToCorner
    }

    public enum TriggerEvent { OnEnable, OnClick, OnHover, OnDisable, OnExit }
    public UnityEvent onComplete;

    [Header("Animation Groups")]
    public List<UIPresetAnimationGroup> animations = new List<UIPresetAnimationGroup>();

    private CanvasGroup canvasGroup;
    private RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable() => PlayFor(TriggerEvent.OnEnable);
    void OnDisable() => PlayFor(TriggerEvent.OnDisable);
    public void OnPointerClick(PointerEventData eventData) => PlayFor(TriggerEvent.OnClick);
    public void OnPointerEnter(PointerEventData eventData) => PlayFor(TriggerEvent.OnHover);
    public void OnPointerExit(PointerEventData eventData) => PlayFor(TriggerEvent.OnExit);

    public void PlayFor(TriggerEvent trigger)
    {
        foreach (var a in animations)
        {
            if (a.triggerEvent != trigger) continue;
            var t = PlayPreset(a.preset, a.duration, a.delay, a.ease);

            if (a.loop && t != null)
                t.SetLoops(a.loopCount, a.loopType);
        }
    }

    Tween PlayPreset(PresetType preset, float duration, float delay, Ease ease)
    {
        switch (preset)
        {
            case PresetType.FadeIn:
                canvasGroup.alpha = 0;
                return canvasGroup.DOFade(1, duration).SetDelay(delay).SetEase(ease);

            case PresetType.FadeInUp:
                var startPos = rect.anchoredPosition - new Vector2(0, 100);
                rect.anchoredPosition = startPos;
                canvasGroup.alpha = 0;
                rect.DOAnchorPos(startPos + new Vector2(0, 100), duration).SetEase(ease).SetDelay(delay);
                return canvasGroup.DOFade(1, duration).SetDelay(delay).SetEase(ease);

            case PresetType.FadeInScale:
                transform.localScale = Vector3.zero;
                canvasGroup.alpha = 0;
                transform.DOScale(1f, duration).SetEase(ease).SetDelay(delay);
                return canvasGroup.DOFade(1, duration).SetEase(ease).SetDelay(delay);

            case PresetType.ScalePop:
                transform.localScale = Vector3.zero;
                return transform.DOScale(1f, duration).SetEase(ease).SetDelay(delay).OnComplete(() =>
                {
                    onComplete.Invoke();
                });

            case PresetType.ScalePulse:
                return transform.DOScale(1.1f, duration / 2f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo).SetDelay(delay);

            case PresetType.SweepInLeft:
                return Sweep(Vector2.left, duration, delay, ease);
            case PresetType.SweepInRight:
                return Sweep(Vector2.right, duration, delay, ease);
            case PresetType.SweepInUp:
                return Sweep(Vector2.up, duration, delay, ease);
            case PresetType.SweepInRandom:
                return Sweep(Random.insideUnitCircle.normalized, duration, delay, ease);

            case PresetType.Shake:
                return rect.DOShakeAnchorPos(duration, 20f, 10).SetEase(ease).SetDelay(delay);

            case PresetType.FlashAlpha:
                canvasGroup.alpha = 1;
                return canvasGroup.DOFade(0.3f, duration / 2f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo).SetDelay(delay).OnComplete(() =>
                {
                    onComplete.Invoke();
                });
            case PresetType.ScaleBounce:
                transform.localScale = Vector3.one * 0.9f;
                return transform.DOScale(1.05f, duration / 2).SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo).SetDelay(delay);
            case PresetType.HighlightPunch:
                return transform.DOPunchScale(Vector3.one * 0.1f, duration, 10, 1).SetDelay(delay);
            case PresetType.GlowPulse:
                canvasGroup.alpha = 1f;
                return canvasGroup.DOFade(0.6f, duration / 2).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo).SetDelay(delay);
            case PresetType.FloatWiggle:
                return rect.DOAnchorPos(rect.anchoredPosition + new Vector2(5, 0), duration / 2)
                           .SetEase(Ease.InOutSine)
                           .SetLoops(2, LoopType.Yoyo)
                           .SetDelay(delay);
            case PresetType.ClickPop:
                return transform.DOScale(0.85f, duration / 2).SetEase(Ease.InQuad)
                    .OnComplete(() => transform.DOScale(1f, duration / 2).SetEase(Ease.OutBack)).SetDelay(delay);
            case PresetType.ClickFlash:
                canvasGroup.alpha = 1;
                return canvasGroup.DOFade(0.2f, duration / 2).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutQuad).SetDelay(delay);
            case PresetType.ClickRotateShake:
                return transform.DORotate(new Vector3(0, 0, 15), duration / 2, RotateMode.Fast)
                    .SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo).SetDelay(delay);
            case PresetType.BreathPulse:
                return transform.DOScale(1.05f, duration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(delay);
            case PresetType.ClickBounceBack:
                var start = rect.anchoredPosition;
                return rect.DOAnchorPos(start - new Vector2(10, 0), duration / 2).SetEase(Ease.InQuad)
                    .OnComplete(() => rect.DOAnchorPos(start, duration / 2).SetEase(Ease.OutBounce)).SetDelay(delay);
            case PresetType.LoopWiggle:
                return rect.DOAnchorPos(rect.anchoredPosition + new Vector2(3f, 0), duration).SetEase(Ease.InOutSine)
                           .SetLoops(-1, LoopType.Yoyo).SetDelay(delay);
            case PresetType.FloatBounceLoop:
                return rect.DOAnchorPos(rect.anchoredPosition + new Vector2(0, 10f), duration).SetEase(Ease.InOutSine)
                           .SetLoops(-1, LoopType.Yoyo).SetDelay(delay);
            case PresetType.PulsateAlpha:
                return canvasGroup.DOFade(0.4f, duration).SetEase(Ease.InOutSine)
                                  .SetLoops(-1, LoopType.Yoyo).SetDelay(delay);
            case PresetType.MoveAroundScreen:
                return MoveImageAroundScreen(rect, duration, ease, delay);
            case PresetType.CircleMotion:
                return CircleMotion(rect, duration, ease);

            case PresetType.SineWaveMotion:
                return SineWaveMotion(rect, duration, ease, amplitude: 30f, horizontal: true);

            case PresetType.DiagonalBounce:
                return DiagonalBounce(rect, duration, ease, range: 80f);
            case PresetType.LogoIntroToCorner:
                return LogoIntroToCurrentPosition(rect, duration, ease, delay);
            default: return null;
        }
    }
    private Tween MoveImageAroundScreen(RectTransform rect, float duration, Ease ease, float delay = 0f)
    {
        Vector2 startPos = rect.anchoredPosition;

        // Define 4 screen-relative waypoints (top-left, top-right, bottom-right, bottom-left)
        List<Vector3> path = new List<Vector3>()
    {
        startPos + new Vector2(-100, 100),
        startPos + new Vector2(100, 100),
        startPos + new Vector2(100, -100),
        startPos + new Vector2(-100, -100),
        startPos
    };

        return rect.DOPath(path.ToArray(), duration, PathType.Linear, PathMode.Sidescroller2D)
                   .SetEase(Ease.Linear)
                   .SetLoops(-1)
                   .SetDelay(delay);
    }
    private Tween SineWaveMotion(RectTransform rect, float duration, Ease ease, float amplitude = 30f, bool horizontal = true)
    {
        Vector2 startPos = rect.anchoredPosition;
        return DOTween.To(() => 0f, x =>
        {
            if (horizontal)
                rect.anchoredPosition = startPos + new Vector2(x, Mathf.Sin(x * 2f) * amplitude);
            else
                rect.anchoredPosition = startPos + new Vector2(Mathf.Sin(x * 2f) * amplitude, x);
        }, 10f, duration)
        .SetEase(ease)
        .SetLoops(-1);
    }
    private Tween CircleMotion(RectTransform rect, float duration, Ease ease, float radius = 5f)
    {
        return DOTween.To(() => 0f, angle =>
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector2 center = rect.anchoredPosition;
            rect.anchoredPosition = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }, 360f, duration)
        .SetEase(Ease.Linear)
        .SetLoops(-1);
    }




    private Tween DiagonalBounce(RectTransform rect, float duration, Ease ease, float range = 100f)
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(range, range);

        return rect.DOAnchorPos(endPos, duration)
                   .SetEase(Ease.InOutSine)
                   .SetLoops(-1, LoopType.Yoyo);
    }

    Tween Sweep(Vector2 dir, float duration, float delay, Ease ease)
    {
        var original = rect.anchoredPosition;
        rect.anchoredPosition = original + dir * 300f;
        canvasGroup.alpha = 0;
        rect.DOAnchorPos(original, duration).SetEase(ease).SetDelay(delay);
        return canvasGroup.DOFade(1, duration).SetEase(ease).SetDelay(delay);
    }
    private Tween LogoIntroToCurrentPosition(RectTransform rect, float duration, Ease ease, float delay = 0f)
    {
        Vector2 targetPos = rect.anchoredPosition;
        Vector3 targetScale = Vector3.one;

        // Start from center and large scale
        rect.anchoredPosition = Vector2.zero;          // Center of the parent canvas
        rect.localScale = Vector3.one * 5f;            // Big size

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOAnchorPos(targetPos, duration).SetEase(ease).SetDelay(delay)).OnComplete(() =>
        {
            onComplete.Invoke();
        });
        seq.Join(rect.DOScale(targetScale, duration).SetEase(ease));

        return seq;
    }

}
