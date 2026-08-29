using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders the 8×8 board (CLAUDE.md §7.5). Background layer: 64 permanent
/// <see cref="SpriteRenderer"/>s that always show empty-cell colour and
/// are NEVER mutated after creation. Fill layer: pooled renderers placed on
/// top of a background cell when it becomes filled, returned to the pool
/// when it clears — matching <see cref="PieceView"/>'s proven-working
/// pattern of using fresh renderers for each state change instead of
/// mutating an existing renderer's sprite/colour, which silently fails to
/// refresh on Android's GLES/IL2CPP runtime (see PROGRESS.md OPEN BUG).
/// </summary>
public sealed class GridManager : MonoBehaviour
{
    // Fill overlays sit just in front of the background (z=0) but behind
    // flash overlays and the drag/ghost pieces (z=-0.5/-1, see
    // PieceController) so visual layering is correct at all times.
    private const float FillZOffset = -0.05f;
    private const float FlashZOffset = -0.2f;

    private readonly SpriteRenderer[] _bgRenderers = new SpriteRenderer[Constants.GridSize * Constants.GridSize];
    private readonly SpriteRenderer[] _fillRenderers = new SpriteRenderer[Constants.GridSize * Constants.GridSize];

    private BoardState _board;
    private ObjectPool<SpriteRenderer> _fillPool;
    private ObjectPool<SpriteRenderer> _flashPool;

    public BoardState Board => _board;

    private void Awake()
    {
        BuildGrid();
    }

    // Separated from Awake() rather than relying on it: AddComponent does
    // not reliably invoke Awake() synchronously outside Play Mode (e.g. in
    // EditMode tests), so construction needs to be independently callable.
    public void BuildGrid()
    {
        _board = new BoardState();

        if (_fillPool == null)
        {
            _fillPool = new ObjectPool<SpriteRenderer>(
                factory: () => CreatePooledRenderer("FillBlock"),
                onGet: r => r.gameObject.SetActive(true),
                onReturn: r => r.gameObject.SetActive(false));
        }

        if (_flashPool == null)
        {
            _flashPool = new ObjectPool<SpriteRenderer>(
                factory: () => CreatePooledRenderer("ClearFlash"),
                onGet: r => r.gameObject.SetActive(true),
                onReturn: r => r.gameObject.SetActive(false));
        }

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                int idx = Index(x, y);

                var cellObject = new GameObject($"Cell_{x}_{y}");
                cellObject.transform.SetParent(transform, false);
                cellObject.transform.localPosition = CellToLocalPosition(x, y);
                cellObject.transform.localScale = Vector3.one * (Constants.CellWorldSize - Constants.CellGap);

                var bgRenderer = cellObject.AddComponent<SpriteRenderer>();
                bgRenderer.sprite = BlockCellSprite.GetEmptyCell();
                bgRenderer.color = Color.white;

                _bgRenderers[idx] = bgRenderer;
                _fillRenderers[idx] = null;
            }
        }

        RefreshAllCells();
    }

    public void PlayClearFlash(IEnumerable<Vector2Int> cells)
    {
        Color goldGlow = new Color(0.94f, 0.85f, 0.56f, 0.5f);

        foreach (Vector2Int cell in cells)
        {
            int idx = Index(cell.x, cell.y);
            Transform cellTransform = _bgRenderers[idx].transform;

            SpriteRenderer flash = _flashPool.Get();
            flash.transform.SetParent(cellTransform, false);
            flash.transform.localPosition = new Vector3(0f, 0f, FlashZOffset);
            flash.transform.localScale = Vector3.one;
            flash.sprite = PlaceholderSprite.GetSolid(Color.white);
            flash.color = goldGlow;

            SpriteRenderer capturedFlash = flash;

            DOTween.Sequence()
                .Append(cellTransform.DOScale(
                    Vector3.one * (Constants.CellWorldSize - Constants.CellGap) * 0.85f,
                    Constants.ClearFlashDurationSeconds).SetEase(Ease.OutQuad))
                .Join(DOTween.ToAlpha(
                    () => capturedFlash.color, c => capturedFlash.color = c,
                    0f, Constants.ClearFadeDurationSeconds))
                .OnComplete(() =>
                {
                    _flashPool.Return(capturedFlash);
                    cellTransform.localScale = Vector3.one * (Constants.CellWorldSize - Constants.CellGap);
                });

            SpawnGoldParticle(cellTransform.position);
        }
    }

    private void SpawnGoldParticle(Vector3 worldPosition)
    {
        var particleObj = new GameObject("GoldParticle");
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.position = worldPosition;
        particleObj.transform.localScale = Vector3.one * 0.08f;

        var sr = particleObj.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.GetSolid(Color.white);
        sr.color = new Color(0.94f, 0.85f, 0.56f, 0.9f);
        sr.sortingOrder = 5;

        float riseHeight = 0.6f + Random.Range(0f, 0.3f);
        float drift = Random.Range(-0.15f, 0.15f);
        float duration = 0.8f + Random.Range(0f, 0.4f);

        DOTween.Sequence()
            .Append(particleObj.transform.DOMove(
                worldPosition + new Vector3(drift, riseHeight, 0f), duration).SetEase(Ease.OutQuad))
            .Join(particleObj.transform.DOScale(Vector3.one * 0.03f, duration))
            .Join(DOTween.ToAlpha(() => sr.color, c => sr.color = c, 0f, duration))
            .OnComplete(() => Object.Destroy(particleObj));
    }

    private SpriteRenderer CreatePooledRenderer(string name)
    {
        var obj = new GameObject(name);
        var renderer = obj.AddComponent<SpriteRenderer>();
        obj.SetActive(false);
        return renderer;
    }

    public void RefreshAllCells()
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                RefreshCell(x, y);
            }
        }
    }

    public void RefreshCell(int x, int y)
    {
        int idx = Index(x, y);
        bool filled = _board.IsFilled(x, y);

        if (filled)
        {
            int colourId = _board.GetColourId(x, y);
            Sprite sprite = UiPalette.GetFilledCellSprite(colourId);
            Color colour = UiPalette.GetBlockColour(colourId);

            // If this cell already has a fill renderer, return it to the
            // pool first — we always get a FRESH one so the Android
            // renderer never has its sprite mutated in place.
            if (_fillRenderers[idx] != null)
            {
                _fillPool.Return(_fillRenderers[idx]);
                _fillRenderers[idx] = null;
            }

            SpriteRenderer fill = _fillPool.Get();
            fill.transform.SetParent(_bgRenderers[idx].transform, false);
            fill.transform.localPosition = new Vector3(0f, 0f, FillZOffset);
            fill.transform.localScale = Vector3.one;
            fill.sprite = sprite;
            fill.color = colour;

            _fillRenderers[idx] = fill;
        }
        else
        {
            // Cell is empty — return fill renderer to pool if one exists.
            if (_fillRenderers[idx] != null)
            {
                _fillPool.Return(_fillRenderers[idx]);
                _fillRenderers[idx] = null;
            }
        }
    }

    public static Vector3 CellToLocalPosition(int x, int y)
    {
        return CellToLocalPosition((float)x, (float)y);
    }

    // Continuous overload: used for positioning a piece's bounding-box
    // centre (which generally isn't an integer cell) rather than a single
    // cell.
    public static Vector3 CellToLocalPosition(float x, float y)
    {
        float step = Constants.CellWorldSize;
        float originOffset = (Constants.GridSize - 1) * step * 0.5f;

        // CLAUDE.md §3.3: (0,0) = top-left, x increases right, y increases
        // down. Unity world space is y-up, so grid-y maps to negative
        // world-y.
        float worldX = (x * step) - originOffset;
        float worldY = originOffset - (y * step);
        return new Vector3(worldX, worldY, 0f);
    }

    public Vector2 WorldToContinuousCell(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float step = Constants.CellWorldSize;
        float originOffset = (Constants.GridSize - 1) * step * 0.5f;

        float cellX = (local.x + originOffset) / step;
        float cellY = (originOffset - local.y) / step;
        return new Vector2(cellX, cellY);
    }

    public bool TryWorldToCell(Vector3 worldPosition, out int cellX, out int cellY)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float step = Constants.CellWorldSize;
        float originOffset = (Constants.GridSize - 1) * step * 0.5f;

        cellX = Mathf.RoundToInt((local.x + originOffset) / step);
        cellY = Mathf.RoundToInt((originOffset - local.y) / step);

        return cellX >= 0 && cellX < Constants.GridSize && cellY >= 0 && cellY < Constants.GridSize;
    }

    private static int Index(int x, int y)
    {
        return (y * Constants.GridSize) + x;
    }
}
