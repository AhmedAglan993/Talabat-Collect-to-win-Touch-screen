using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Order-hunt: each round picks <b>one of two</b> predefined sprite orders (not a fully random pick from the pool).
/// Order row length = number of sprites in the chosen scenario. Drops on the collection box fill matching slots (any order among slots).
/// </summary>
public class OrderHuntManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ShuffleItemSpawner shuffleSpawner;

    [Header("Drop zone")]
    [Tooltip("Corner box Image — player must drop matching items here to collect them.")]
    [SerializeField] private RectTransform collectionBox;

    [Header("Drag — forbidden zones")]
    [Tooltip("Items cannot be dragged into these rects (e.g. UI chrome).")]
    [SerializeField] private List<RectTransform> restrictedDragAreas = new List<RectTransform>();

    [Header("Order scenarios (one chosen at random each round)")]
    [Tooltip("Add 2 (or more) entries; each has optional entryIds + orderSprites. One scenario is picked per round.")]
    [SerializeField] private List<SpriteOrderDefinition> orderScenarios = new List<SpriteOrderDefinition>();
    [Header("Order UI")]
    [SerializeField] private RectTransform orderContainer;
    [SerializeField] private Image orderIconPrefab;

    [Header("Feedback")]
    [SerializeField] private UIShake uiShake;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    [Header("Order icon animation")]
    [SerializeField] private float orderCollectFadeSeconds = 0.12f;
    [SerializeField] private float orderCollectScaleSeconds = 0.12f;

    [Header("Completion → QR canvas")]
    [SerializeField] private float delayBeforeQrSeconds = 0.35f;
    [SerializeField] private int scorePerCollectedItem = 10;

    /// <summary>
    /// Optional ids + sprites for one fixed order. Gameplay uses <see cref="orderSprites"/>; ids are optional metadata.
    /// </summary>
    [System.Serializable]
    public class SpriteOrderDefinition
    {
        [Tooltip("Optional parallel ids (same count as Sprites if used).")]
        public string orderType;

        [Tooltip("Sprites required for this order, in display order (length = number of order slots).")]
        public List<Sprite> orderSprites = new List<Sprite>();
    }

    private readonly List<Sprite> _currentOrderSprites = new List<Sprite>();
    private bool[] _slotCollected;
    private readonly List<Image> _orderSlotIcons = new List<Image>();

    private int _initialOrderCount;
    private bool _completionStarted;
    private float _roundStartTime;

    /// <summary>
    /// Regions collectibles may not overlap while dragging (same list passed at spawn).
    /// </summary>
    public IReadOnlyList<RectTransform> RestrictedDragAreas => restrictedDragAreas;

    private void Awake()
    {
        if (uiShake == null) uiShake = GetComponentInChildren<UIShake>(true);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        BuildNewRound();
    }

    /// <summary>
    /// Picks a random entry from <see cref="orderScenarios"/> that has sprites, builds the order row, then spawns the play area.
    /// </summary>
    public void BuildNewRound()
    {
        ClearOrderUI();
        _orderSlotIcons.Clear();
        _currentOrderSprites.Clear();
        _slotCollected = null;
        _completionStarted = false;
        _roundStartTime = Time.realtimeSinceStartup;

        if (shuffleSpawner == null || orderContainer == null || orderIconPrefab == null || collectionBox == null)
        {
            enabled = false;
            return;
        }

        List<Sprite> pool = shuffleSpawner.GetAvailableSprites();
        if (pool.Count == 0)
        {
            enabled = false;
            return;
        }

        SpriteOrderDefinition chosen = PickRandomScenario();
        if (chosen == null || chosen.orderSprites == null || chosen.orderSprites.Count == 0)
        {
            Debug.LogWarning("[OrderHuntManager] No valid order scenario: add at least one entry to orderScenarios with orderSprites.");
            enabled = false;
            return;
        }



        foreach (Sprite s in chosen.orderSprites)
        {
            if (s == null) continue;
            _currentOrderSprites.Add(s);
        }

        if (_currentOrderSprites.Count == 0)
        {
            enabled = false;
            return;
        }

        _initialOrderCount = _currentOrderSprites.Count;
        _slotCollected = new bool[_currentOrderSprites.Count];

        for (int i = 0; i < _currentOrderSprites.Count; i++)
        {
            Sprite s = _currentOrderSprites[i];
            Image icon = Instantiate(orderIconPrefab, orderContainer);
            icon.sprite = s;
            icon.enabled = true;

            _orderSlotIcons.Add(icon);
        }

        shuffleSpawner.SpawnNow();
        shuffleSpawner.AttachCollectibles(this);
    }

    /// <summary>
    /// Returns a random scenario from <see cref="orderScenarios"/> that has at least one non-null sprite.
    /// </summary>
    private SpriteOrderDefinition PickRandomScenario()
    {
        if (orderScenarios == null || orderScenarios.Count == 0)
            return null;

        List<SpriteOrderDefinition> valid = new List<SpriteOrderDefinition>();
        for (int i = 0; i < orderScenarios.Count; i++)
        {
            SpriteOrderDefinition sc = orderScenarios[i];
            if (sc == null || sc.orderSprites == null) continue;
            if (CountNonNullSprites(sc.orderSprites) > 0)
                valid.Add(sc);
        }

        if (valid.Count == 0)
            return null;

        return valid[Random.Range(0, valid.Count)];
    }

    private static int CountNonNullSprites(List<Sprite> list)
    {
        int n = 0;
        if (list == null) return 0;
        foreach (Sprite s in list)
        {
            if (s != null) n++;
        }

        return n;
    }

    /// <summary>
    /// Finds the first uncollected slot whose sprite matches the dropped item (supports duplicate sprites in the order).
    /// </summary>
    private bool TryConsumeMatchingSlot(Sprite sprite, out int slotIndex)
    {
        slotIndex = -1;
        if (sprite == null || _slotCollected == null || _currentOrderSprites == null) return false;

        for (int i = 0; i < _currentOrderSprites.Count; i++)
        {
            if (_slotCollected[i]) continue;
            if (_currentOrderSprites[i] == sprite)
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private int CountSlotsRemaining()
    {
        if (_slotCollected == null) return 0;
        int c = 0;
        for (int i = 0; i < _slotCollected.Length; i++)
        {
            if (!_slotCollected[i]) c++;
        }

        return c;
    }

    /// <summary>
    /// Called when a draggable item is released after a drag. Handles drop-on-box vs snap-back.
    /// </summary>
    public void HandleItemDropFinished(CollectibleItem item, PointerEventData eventData)
    {
        if (item == null || collectionBox == null) return;

        Camera cam = eventData.pressEventCamera;
        if (!RectTransformUtility.RectangleContainsScreenPoint(collectionBox, eventData.position, cam))
        {
            // Not dropped on the collection box — leave the item where it was released (no snap).
            return;
        }

        Sprite sprite = item.Sprite;
        if (sprite == null)
        {
            item.SnapBackToStart();
            return;
        }

        if (TryConsumeMatchingSlot(sprite, out int slotIndex))
        {
            _slotCollected[slotIndex] = true;
            PlayOneShot(correctClip);

            if (slotIndex >= 0 && slotIndex < _orderSlotIcons.Count)
            {
                Image icon = _orderSlotIcons[slotIndex];
                if (icon != null)
                    StartCoroutine(AnimateOrderIconCollected(icon));
            }

            item.PlayDropIntoBoxThenDestroy(collectionBox);

            if (CountSlotsRemaining() == 0 && !_completionStarted)
            {
                _completionStarted = true;
                StartCoroutine(CompleteOrderAndGoToQr());
            }
        }
        else
        {
            PlayOneShot(wrongClip);
            if (uiShake != null) uiShake.Shake();
            item.SnapBackToStart();
        }
    }

    [ContextMenu("Build New Round")]
    private void ContextBuild() => BuildNewRound();

    private IEnumerator CompleteOrderAndGoToQr()
    {
        if (delayBeforeQrSeconds > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeQrSeconds);

        int totalScore = Mathf.Max(0, _initialOrderCount) * Mathf.Max(0, scorePerCollectedItem);
        float duration = Time.realtimeSinceStartup - _roundStartTime;

        var session = KioskGameManager.CurrentSession;
        session.miniGameScore = totalScore;
        session.miniGameDuration = duration;

        if (KioskGameManager.Instance != null)
        {
            KioskGameManager.Instance.GoToLevelUP();

        }
    }

    private IEnumerator AnimateOrderIconCollected(Image icon)
    {
        CanvasGroup cg = icon.GetComponent<CanvasGroup>();
        if (cg == null) cg = icon.gameObject.AddComponent<CanvasGroup>();

        Vector3 startScale = icon.transform.localScale;
        Vector3 endScale = startScale * 0.85f;
        float startAlpha = cg.alpha;

        float t = 0f;
        float dur = Mathf.Max(0.01f, Mathf.Max(orderCollectFadeSeconds, orderCollectScaleSeconds));

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float aFade = orderCollectFadeSeconds <= 0f ? 1f : Mathf.Clamp01(t / orderCollectFadeSeconds);
            float aScale = orderCollectScaleSeconds <= 0f ? 1f : Mathf.Clamp01(t / orderCollectScaleSeconds);

            cg.alpha = Mathf.Lerp(startAlpha, 0.25f, aFade);
            icon.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, aScale);
            yield return null;
        }

        cg.alpha = 0.25f;
        icon.transform.localScale = endScale;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void ClearOrderUI()
    {
        if (orderContainer == null) return;
        for (int i = orderContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(orderContainer.GetChild(i).gameObject);
        }
    }
}
