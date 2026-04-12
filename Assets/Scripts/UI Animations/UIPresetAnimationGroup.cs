using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[System.Serializable]
public class UIPresetAnimationGroup
{
    public UIPresetAnimator.PresetType preset;
    public UIPresetAnimator.TriggerEvent triggerEvent;

    public float duration = 0.8f;
    public float delay = 0f;
    public Ease ease = Ease.OutQuad;

    public bool loop = false;
    public LoopType loopType = LoopType.Yoyo;
    public int loopCount = -1;
}
