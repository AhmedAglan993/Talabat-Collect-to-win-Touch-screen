using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Clickable spawned item that reports its sprite to an OrderHuntManager.
/// </summary>
[RequireComponent(typeof(Image))]
public class CollectibleItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float collectPopDurationSeconds = 0.15f;

    private Image _image;
    private OrderHuntManager _manager;
    private bool _isResolving;

    /// <summary>
    /// The sprite currently displayed by this collectible.
    /// </summary>
    public Sprite Sprite => _image != null ? _image.sprite : null;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    /// <summary>
    /// Initializes the collectible with a manager reference.
    /// </summary>
    public void Init(OrderHuntManager manager)
    {
        _manager = manager;
        _isResolving = false;
    }

    /// <summary>
    /// Unity UI pointer / touch click handler.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isResolving) return;
        if (_manager == null) return;
        if (_image == null || _image.sprite == null) return;

        _manager.TryCollect(this);
    }

    /// <summary>
    /// Plays a pop animation then destroys this item.
    /// </summary>
    public void PlayCollectAndDestroy()
    {
        if (_isResolving) return;
        _isResolving = true;
        StartCoroutine(PopAndDestroy());
    }

    private IEnumerator PopAndDestroy()
    {
        float t = 0f;
        Vector3 start = transform.localScale;
        Vector3 end = Vector3.zero;

        while (t < collectPopDurationSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = collectPopDurationSeconds <= 0f ? 1f : Mathf.Clamp01(t / collectPopDurationSeconds);
            transform.localScale = Vector3.LerpUnclamped(start, end, a);
            yield return null;
        }

        Destroy(gameObject);
    }
}

