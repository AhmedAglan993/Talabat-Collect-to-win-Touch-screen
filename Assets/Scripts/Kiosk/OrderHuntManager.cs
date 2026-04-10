using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Order-hunt mini-game: builds a random order from the shuffle spawner's sprite pool,
/// handles collect / wrong feedback, writes session score and duration, then opens the QR canvas.
/// </summary>
public class OrderHuntManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ShuffleItemSpawner shuffleSpawner;

    [Header("Order UI")]
    [SerializeField] private RectTransform orderContainer;
    [SerializeField] private Image orderIconPrefab;
    [SerializeField] private int orderCount = 8;

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

    private readonly HashSet<Sprite> _remaining = new HashSet<Sprite>();
    private readonly Dictionary<Sprite, Image> _orderIconBySprite = new Dictionary<Sprite, Image>();
    private int _initialOrderCount;
    private bool _completionStarted;
    private float _roundStartTime;

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
    /// Builds a new shuffled play area (optional) and generates a new order list.
    /// </summary>
    public void BuildNewRound()
    {
        ClearOrderUI();
        _remaining.Clear();
        _orderIconBySprite.Clear();
        _completionStarted = false;
        _roundStartTime = Time.realtimeSinceStartup;

        if (shuffleSpawner == null || orderContainer == null || orderIconPrefab == null)
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

        int count = Mathf.Clamp(orderCount, 1, pool.Count);
        Shuffle(pool);

        for (int i = 0; i < count; i++)
        {
            Sprite s = pool[i];
            if (s == null) continue;
            if (_remaining.Contains(s)) continue;

            _remaining.Add(s);
            Image icon = Instantiate(orderIconPrefab, orderContainer);
            icon.sprite = s;
            icon.enabled = true;
            _orderIconBySprite[s] = icon;
        }

        _initialOrderCount = _remaining.Count;

        // Ensure the play area items are present and clickable for this round.
        shuffleSpawner.SpawnNow();
        shuffleSpawner.AttachCollectibles(this);
    }

    /// <summary>
    /// Called by CollectibleItem when player taps an item in the play area.
    /// </summary>
    public void TryCollect(CollectibleItem item)
    {
        if (item == null) return;
        Sprite sprite = item.Sprite;
        if (sprite == null) return;

        if (_remaining.Contains(sprite))
        {
            _remaining.Remove(sprite);
            PlayOneShot(correctClip);

            if (_orderIconBySprite.TryGetValue(sprite, out Image icon) && icon != null)
            {
                StartCoroutine(AnimateOrderIconCollected(icon));
            }

            item.PlayCollectAndDestroy();

            if (_remaining.Count == 0 && !_completionStarted)
            {
                _completionStarted = true;
                StartCoroutine(CompleteOrderAndGoToQr());
            }
        }
        else
        {
            PlayOneShot(wrongClip);
            if (uiShake != null) uiShake.Shake();
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

        KioskGameManager.GoToQRCode();
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

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

