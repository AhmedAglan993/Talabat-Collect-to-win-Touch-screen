using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Draggable item: free movement while dragging; on release, if overlapping a restricted zone,
/// snaps back to the drag start position. Otherwise the manager handles drop on the collection box.
/// </summary>
[RequireComponent(typeof(Image))]
public class CollectibleItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag")]
    [SerializeField] private float dragVisualScale = 1.08f;

    [Tooltip("If set, item is reparented here while dragging (e.g. full-screen UI under Canvas) so it can move across the whole screen, not only inside the spawn/play Rect.")]
    [SerializeField] private RectTransform freeDragLayer;

    [Header("Drop animation")]
    [SerializeField] private float dropTravelSeconds = 0.32f;
    [SerializeField] private float dropShrinkSeconds = 0.15f;

    [Header("Snap back")]
    [SerializeField] private float snapBackSeconds = 0.22f;

    private Image _image;
    private RectTransform _rt;
    private OrderHuntManager _manager;
    private IReadOnlyList<RectTransform> _restrictedZones;

    private Vector2 _startAnchoredPos;
    private Vector3 _startScale;
    private Vector2 _dragPointerOffset;

    private Transform _savedParent;
    private int _savedSiblingIndex;

    private bool _isResolving;
    private bool _dragging;

    /// <summary>
    /// The sprite currently displayed by this collectible.
    /// </summary>
    public Sprite Sprite => _image != null ? _image.sprite : null;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rt = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Initializes the collectible with optional forbidden rects (checked only when a drag ends).
    /// </summary>
    public void Init(OrderHuntManager manager, IReadOnlyList<RectTransform> restrictedZones)
    {
        _manager = manager;
        _restrictedZones = restrictedZones;
        _isResolving = false;
    }

    /// <summary>
    /// Begins dragging; brings this item to front and scales slightly.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isResolving || _manager == null) return;

        _dragging = true;
        _startAnchoredPos = _rt.anchoredPosition;
        _startScale = _rt.localScale;
        _rt.localScale = _startScale * dragVisualScale;

        if (freeDragLayer != null)
        {
            _savedParent = _rt.parent;
            _savedSiblingIndex = _rt.GetSiblingIndex();
            _rt.SetParent(freeDragLayer, true);
            _rt.SetAsLastSibling();
        }
        else
        {
            _rt.SetAsLastSibling();
        }

        var parent = _rt.parent as RectTransform;
        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            _dragPointerOffset = _rt.anchoredPosition - localPoint;
        }
        else
        {
            _dragPointerOffset = Vector2.zero;
        }
    }

    /// <summary>
    /// Moves with the pointer — no clamping or restriction while dragging.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_isResolving || !_dragging || _manager == null) return;

        var parent = _rt.parent as RectTransform;
        if (parent == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        _rt.anchoredPosition = localPoint + _dragPointerOffset;
    }

    /// <summary>
    /// Ends drag: if the item overlaps a restricted area, snap back; else let the manager handle the drop box.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        _dragging = false;

        if (!_isResolving && _rt != null)
            _rt.localScale = _startScale;

        RestoreParentIfNeeded();

        if (_isResolving || _manager == null) return;

        if (OverlapsAnyRestrictedZone())
        {
            SnapBackToStart();
            return;
        }

        _manager.HandleItemDropFinished(this, eventData);
    }

    private void RestoreParentIfNeeded()
    {
        if (freeDragLayer == null || _savedParent == null) return;
        if (_rt.parent != freeDragLayer) return;

        _rt.SetParent(_savedParent, true);
        int max = _savedParent.childCount - 1;
        if (max >= 0)
            _rt.SetSiblingIndex(Mathf.Clamp(_savedSiblingIndex, 0, max));
        _savedParent = null;
    }

    /// <summary>
    /// True if this item overlaps a restricted rect in <b>screen space</b> (avoids false positives from 3D bounds on flat UI).
    /// </summary>
    private bool OverlapsAnyRestrictedZone()
    {
        if (_restrictedZones == null || _restrictedZones.Count == 0)
            return false;

        Camera cam = GetUiReferenceCamera();
        Rect itemScreen = RectTransformToScreenRect(_rt, cam);

        foreach (RectTransform zone in _restrictedZones)
        {
            if (zone == null) continue;
            Rect zoneScreen = RectTransformToScreenRect(zone, cam);
            if (itemScreen.Overlaps(zoneScreen))
                return true;
        }

        return false;
    }

    private Camera GetUiReferenceCamera()
    {
        Canvas canvas = _rt.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return canvas.worldCamera;
    }

    private static Rect RectTransformToScreenRect(RectTransform rt, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector3 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            if (sp.x < minX) minX = sp.x;
            if (sp.x > maxX) maxX = sp.x;
            if (sp.y < minY) minY = sp.y;
            if (sp.y > maxY) maxY = sp.y;
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Returns the item to its pre-drag position.
    /// </summary>
    public void SnapBackToStart()
    {
        if (_isResolving) return;
        StartCoroutine(SnapBackRoutine());
    }

    private IEnumerator SnapBackRoutine()
    {
        Vector2 from = _rt.anchoredPosition;
        float t = 0f;
        float dur = Mathf.Max(0.01f, snapBackSeconds);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            _rt.anchoredPosition = Vector2.LerpUnclamped(from, _startAnchoredPos, a);
            yield return null;
        }

        _rt.anchoredPosition = _startAnchoredPos;
    }

    /// <summary>
    /// Flies into the collection box then destroys this GameObject.
    /// </summary>
    public void PlayDropIntoBoxThenDestroy(RectTransform dropBoxRect)
    {
        if (_isResolving) return;
        _isResolving = true;
        StartCoroutine(DropIntoBoxRoutine(dropBoxRect));
    }

    private IEnumerator DropIntoBoxRoutine(RectTransform dropBox)
    {
        Vector3 startPos = _rt.position;
        Vector3 endPos = dropBox != null
            ? dropBox.TransformPoint(dropBox.rect.center)
            : startPos;

        float travel = Mathf.Max(0.01f, dropTravelSeconds);
        float t = 0f;
        while (t < travel)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / travel);
            _rt.position = Vector3.LerpUnclamped(startPos, endPos, a);
            yield return null;
        }

        Vector3 sc = _rt.localScale;
        float shrink = Mathf.Max(0.01f, dropShrinkSeconds);
        t = 0f;
        while (t < shrink)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / shrink);
            _rt.localScale = Vector3.LerpUnclamped(sc, Vector3.zero, a);
            yield return null;
        }

        Destroy(gameObject);
    }
}
