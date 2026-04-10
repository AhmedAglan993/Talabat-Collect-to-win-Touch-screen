using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone UI spawner that generates slot positions automatically,
/// instantiates a prefab Image in each slot, and assigns sprites randomly.
/// Intended for quick testing independent from the kiosk game flow.
/// </summary>
public class ShuffleItemSpawner : MonoBehaviour
{
    [Header("Where to spawn (RectTransform area)")]
    [SerializeField] private RectTransform spawnArea;

    [Header("What to spawn")]
    [SerializeField] private Image itemPrefab;
    [SerializeField] private List<Sprite> availableSprites = new List<Sprite>();

    [Header("How many")]
    [SerializeField] private int slotsToSpawn = 24;

    [Header("Layout constraints (pixels in spawnArea local space)")]
    [SerializeField] private float safePaddingPixels = 40f;
    [SerializeField] private float minDistancePixels = 90f;
    [SerializeField] private int maxPlacementAttemptsPerSlot = 80;

    [Header("Sprite assignment")]
    [SerializeField] private bool shuffleOnStart = true;
    [SerializeField] private bool allowRepeatsIfNotEnoughSprites = true;
    [SerializeField] private bool setNativeSizeFromSprite = true;

    [Header("Optional sizing")]
    [SerializeField] private bool forcePrefabSize = false;
    [SerializeField] private Vector2 forcedSizePixels = new Vector2(120f, 120f);

    private readonly List<Image> _spawned = new List<Image>(64);

    /// <summary>
    /// Returns a copy of the available sprite pool used for shuffling.
    /// </summary>
    public List<Sprite> GetAvailableSprites()
    {
        return availableSprites == null ? new List<Sprite>() : new List<Sprite>(availableSprites);
    }

    /// <summary>
    /// Ensures each spawned item has a CollectibleItem component that forwards taps to the manager.
    /// Call after SpawnNow().
    /// </summary>
    public void AttachCollectibles(OrderHuntManager manager)
    {
        if (manager == null) return;
        for (int i = 0; i < _spawned.Count; i++)
        {

            Image img = _spawned[i];
            if (img == null) continue;

            img.raycastTarget = true;

            CollectibleItem c = img.GetComponent<CollectibleItem>();
            if (c == null)
            {
                c = img.gameObject.AddComponent<CollectibleItem>();
            print("Manager");

            }
            c.Init(manager);
        }
    }

    private void Reset()
    {
        if (spawnArea == null) spawnArea = transform as RectTransform;
    }

    private void Awake()
    {
        if (spawnArea == null)
        {
            spawnArea = transform as RectTransform;
        }
    }

   

    /// <summary>
    /// Clears current spawned items (if any) and spawns a new shuffled layout.
    /// </summary>
    public void SpawnNow()
    {
        if (spawnArea == null || itemPrefab == null)
        {
            enabled = false;
            return;
        }

        Clear();

        int slotCount = Mathf.Max(0, slotsToSpawn);
        if (slotCount == 0) return;

        List<Vector2> positions = GeneratePositions(slotCount);

        List<Sprite> spriteOrder = BuildSpriteOrder(slotCount);

        for (int i = 0; i < positions.Count; i++)
        {
            Image img = Instantiate(itemPrefab, spawnArea);
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = positions[i];

            if (forcePrefabSize)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, forcedSizePixels.x);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, forcedSizePixels.y);
            }

            if (i < spriteOrder.Count)
            {
                img.sprite = spriteOrder[i];
            }

            img.enabled = img.sprite != null;
            if (setNativeSizeFromSprite && img.sprite != null && !forcePrefabSize)
            {
                img.SetNativeSize();
            }
            _spawned.Add(img);
        }
    }

    /// <summary>
    /// Re-shuffles sprites on the existing spawned items without changing positions.
    /// </summary>
    public void ReshuffleSpritesOnly()
    {
        if (_spawned.Count == 0) return;

        List<Sprite> spriteOrder = BuildSpriteOrder(_spawned.Count);
        for (int i = 0; i < _spawned.Count; i++)
        {
            Image img = _spawned[i];
            if (img == null) continue;
            img.sprite = i < spriteOrder.Count ? spriteOrder[i] : null;
            img.enabled = img.sprite != null;
            if (setNativeSizeFromSprite && img.sprite != null && !forcePrefabSize)
            {
                img.SetNativeSize();
            }
        }
    }

    /// <summary>
    /// Destroys all spawned items created by this component.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                Destroy(_spawned[i].gameObject);
            }
        }
        _spawned.Clear();
    }

    [ContextMenu("Spawn Now")]
    private void ContextSpawnNow() => SpawnNow();

    [ContextMenu("Clear Spawned")]
    private void ContextClear() => Clear();

    [ContextMenu("Reshuffle Sprites Only")]
    private void ContextReshuffleOnly() => ReshuffleSpritesOnly();

    private List<Sprite> BuildSpriteOrder(int count)
    {
        List<Sprite> sprites = new List<Sprite>();
        if (availableSprites == null || availableSprites.Count == 0) return sprites;

        List<Sprite> pool = new List<Sprite>(availableSprites);
        Shuffle(pool);

        for (int i = 0; i < count; i++)
        {
            if (i < pool.Count)
            {
                sprites.Add(pool[i]);
            }
            else
            {
                if (!allowRepeatsIfNotEnoughSprites) break;
                sprites.Add(availableSprites[Random.Range(0, availableSprites.Count)]);
            }
        }

        return sprites;
    }

    private List<Vector2> GeneratePositions(int count)
    {
        Rect rect = spawnArea.rect;

        float minX = rect.xMin + safePaddingPixels;
        float maxX = rect.xMax - safePaddingPixels;
        float minY = rect.yMin + safePaddingPixels;
        float maxY = rect.yMax - safePaddingPixels;

        // Fallback: if padding collapses area, spawn in center.
        if (minX >= maxX || minY >= maxY)
        {
            List<Vector2> center = new List<Vector2>(count);
            for (int i = 0; i < count; i++) center.Add(Vector2.zero);
            return center;
        }

        float minDistSqr = Mathf.Max(0f, minDistancePixels) * Mathf.Max(0f, minDistancePixels);
        List<Vector2> placed = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            bool ok = TryPlaceOne(minX, maxX, minY, maxY, minDistSqr, placed, out Vector2 p);
            if (!ok)
            {
                // If we can't place with min distance (too many items),
                // reduce distance slightly and retry once.
                float relaxed = minDistSqr * 0.6f;
                ok = TryPlaceOne(minX, maxX, minY, maxY, relaxed, placed, out p);
            }

            placed.Add(p);
        }

        return placed;
    }

    private bool TryPlaceOne(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minDistSqr,
        List<Vector2> existing,
        out Vector2 point)
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxPlacementAttemptsPerSlot); attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            bool valid = true;

            for (int j = 0; j < existing.Count; j++)
            {
                if ((existing[j] - candidate).sqrMagnitude < minDistSqr)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                point = candidate;
                return true;
            }
        }

        // Worst-case fallback: just place anywhere (may overlap).
        point = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        return false;
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

