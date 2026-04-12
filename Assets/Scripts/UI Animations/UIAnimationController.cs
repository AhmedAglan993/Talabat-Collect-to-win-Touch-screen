using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimationController : MonoBehaviour
{
    public enum AnimationPreset
    {
        None,
        FadeInUp,
        ScalePop,
        SweepInRandom,
        MaterialPulse
    }

    [System.Serializable]
    public enum AnimationType
    {
        Move,
        Scale,
        Fade,
        TopographyLightSweep, // existing
        MaterialOffsetScroll  // 🔥 new!
    }
    [System.Serializable]
    public class UIAnimationItem
    {
        public RectTransform target;
        public CanvasGroup canvasGroup;
        public AnimationType animationType = AnimationType.Fade;
        // Fade
        [Header("Fade Settings")]
        public float fromAlpha = 0f;
        // Move
        [Header("Move Settings")]
        public Vector2 fromPositionOffset = Vector2.zero;
        // Scale
        [Header("Scale Settings")]
        public Vector3 fromScale = Vector3.zero;
        // Topography Sweep
        [Header("Topography Sweep Settings")]
        public float sweepDistance = 800f;
        public Vector2 sweepDirection = Vector2.right;
        public bool useRandomDirection = true;
        public Vector2 distanceRange = new Vector2(500f, 1200f);
        // Material Offset Scroll
        [Header("Material Offset Settings")]
        public Renderer targetRenderer;
        public RawImage targetRawImage;
        public bool useURPShader = false;
        public bool useRandomOffsetDirection = true;
        public Vector2 offsetSpeedRange = new Vector2(0.1f, 0.5f);
        public Vector2 offsetDurationRange = new Vector2(2f, 5f);
        [Header("Looping")]
        public bool loop = false;
        public LoopType loopType = LoopType.Yoyo;
        public float loopValue = 1.05f;
        public float loopDuration = 1f;
        // Shared Animation Properties
        public float delay = 0f;
        public float duration = 1f;
        public Ease ease = Ease.OutQuad;
        public AnimationPreset animationPreset = AnimationPreset.None;
        public bool usePreset = false;
    }

    public List<UIAnimationItem> animations = new List<UIAnimationItem>();
    public bool playOnStart = true;

    void Start()
    {
        if (playOnStart)
            PlayAnimations();
    }


    public void PlayAnimations()
    {
        foreach (var item in animations)
        {
            if (item.target == null) continue;

            if (item.usePreset)
            {
                ApplyPreset(item);
            }
            else
            {
                PlayCustomAnimation(item);
            }
        }
    }
    void ApplyPreset(UIAnimationItem item)
    {
        switch (item.animationPreset)
        {
            case AnimationPreset.FadeInUp:
                item.animationType = AnimationType.Move;
                item.fromPositionOffset = new Vector2(0, -100);
                item.duration = 0.6f;
                item.ease = Ease.OutCubic;
                AnimateFade(item);
                AnimateMove(item);
                break;

            case AnimationPreset.ScalePop:
                item.animationType = AnimationType.Scale;
                item.fromScale = Vector3.zero;
                item.duration = 0.5f;
                item.ease = Ease.OutBack;
                AnimateScale(item);
                break;

            case AnimationPreset.SweepInRandom:
                item.animationType = AnimationType.TopographyLightSweep;
                item.loop = true;
                item.useRandomDirection = true;
                item.duration = 1f;
                item.ease = Ease.Linear;
                AnimateTopographySweep(item);
                break;

            case AnimationPreset.MaterialPulse:
                item.animationType = AnimationType.MaterialOffsetScroll;
                item.loop = true;
                item.offsetSpeedRange = new Vector2(0.05f, 0.2f);
                item.offsetDurationRange = new Vector2(2f, 4f);
                AnimateMaterialOffset(item);
                break;
        }
    }

    void PlayCustomAnimation(UIAnimationItem item)
    {
        switch (item.animationType)
        {
            case AnimationType.Fade: AnimateFade(item); break;
            case AnimationType.Move: AnimateMove(item); break;
            case AnimationType.Scale: AnimateScale(item); break;
            case AnimationType.TopographyLightSweep: AnimateTopographySweep(item); break;
            case AnimationType.MaterialOffsetScroll: AnimateMaterialOffset(item); break;
        }
    }


    void AnimateFade(UIAnimationItem item)
    {
        if (item.canvasGroup == null)
        {
            item.canvasGroup = item.target.GetComponent<CanvasGroup>();
            if (item.canvasGroup == null)
            {
                item.canvasGroup = item.target.gameObject.AddComponent<CanvasGroup>();
            }
        }

        item.canvasGroup.alpha = item.fromAlpha;
        var tween = item.canvasGroup.DOFade(1f, item.duration).SetEase(item.ease).SetDelay(item.delay);

        if (item.loop)
            tween.SetLoops(-1, item.loopType);
    }

    void AnimateMove(UIAnimationItem item)
    {
        Vector2 original = item.target.anchoredPosition;
        item.target.anchoredPosition = original + item.fromPositionOffset;

        var tween = item.target.DOAnchorPos(original, item.duration).SetEase(item.ease).SetDelay(item.delay);

        if (item.loop)
            tween.SetLoops(-1, item.loopType);
    }

    void AnimateScale(UIAnimationItem item)
    {
        item.target.localScale = item.fromScale;

        var tween = item.target.DOScale(Vector3.one, item.duration).SetEase(item.ease).SetDelay(item.delay);

        if (item.loop)
        {
            item.target.localScale = Vector3.one;
            tween = item.target.DOScale(item.loopValue, item.loopDuration)
                                .SetLoops(-1, item.loopType)
                                .SetEase(item.ease)
                                .SetDelay(item.delay);
        }
    }
    void AnimateTopographySweep(UIAnimationItem item)
    {
        StartCoroutine(RunSweepLoop(item));
    }

    IEnumerator RunSweepLoop(UIAnimationItem item)
    {
        while (true)
        {
            Vector2 direction;
            float distance;

            if (item.useRandomDirection)
            {
                float angle = Random.Range(0f, 360f);
                direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
                distance = Random.Range(item.distanceRange.x, item.distanceRange.y);
            }
            else
            {
                direction = item.sweepDirection.normalized;
                distance = item.sweepDistance;
            }

            Vector2 center = item.target.anchoredPosition;
            Vector2 startPos = center - direction * distance;
            Vector2 endPos = center + direction * distance;

            item.target.anchoredPosition = startPos;

            // Animate to end position
            Tween tween = item.target.DOAnchorPos(endPos, item.duration)
                              .SetEase(item.ease)
                              .SetDelay(item.delay);

            yield return tween.WaitForCompletion();

            // Optional: small pause between sweeps
            yield return new WaitForSeconds(0.1f);

            if (!item.loop)
                break;
        }
    }
    void AnimateMaterialOffset(UIAnimationItem item)
    {
        Material runtimeMaterial = null;
        string textureProperty = item.useURPShader ? "_BaseMap" : "_MainTex";

        // Setup material instance
        if (item.targetRenderer != null)
        {
            runtimeMaterial = item.targetRenderer.material;
        }
        else if (item.targetRawImage != null)
        {
            runtimeMaterial = Instantiate(item.targetRawImage.material);
            item.targetRawImage.material = runtimeMaterial;
        }
        else
        {
            Debug.LogWarning("MaterialOffsetScroll: No renderer or raw image assigned.");
            return;
        }

        StartCoroutine(MaterialOffsetLoop(runtimeMaterial, textureProperty, item));
    }
    IEnumerator MaterialOffsetLoop(Material mat, string property, UIAnimationItem item)
    {
        Vector2 offset = Vector2.zero;

        while (true)
        {
            float angle = item.useRandomOffsetDirection
                ? Random.Range(0f, 360f)
                : 0f;

            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float speed = Random.Range(item.offsetSpeedRange.x, item.offsetSpeedRange.y);
            float duration = Random.Range(item.offsetDurationRange.x, item.offsetDurationRange.y);

            Vector2 targetOffset = offset + direction.normalized * speed;

            Tween tween = DOTween.To(
                () => offset,
                val =>
                {
                    offset = val;
                    mat.SetTextureOffset(property, offset);
                },
                targetOffset,
                duration
            ).SetEase(item.ease)
             .SetDelay(item.delay);

            yield return tween.WaitForCompletion();

            if (!item.loop)
                break;

            yield return new WaitForSeconds(0.05f); // Optional rest between sweeps
        }
    }


}
