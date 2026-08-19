using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders the 8×8 board (CLAUDE.md §7.5): 64 SpriteRenderers created once
/// in <see cref="Awake"/> and never instantiated/destroyed again — only
/// their colour/scale mutate. Owns the live <see cref="BoardState"/> and
/// exposes cell↔world conversions for drag/ghost/snap logic.
/// </summary>
public sealed class GridManager : MonoBehaviour
{
    // Flash overlays sit between the grid (z=0) and the drag/ghost pieces
    // (z=-0.5/-1, see PieceController) so a flash never occludes the piece
    // the player is actively looking at.
    private const float FlashZOffset = -0.2f;

    private readonly SpriteRenderer[] _cellRenderers = new SpriteRenderer[Constants.GridSize * Constants.GridSize];

    private BoardState _board;
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

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                var cellObject = new GameObject($"Cell_{x}_{y}");
                cellObject.transform.SetParent(transform, false);
                cellObject.transform.localPosition = CellToLocalPosition(x, y);
                cellObject.transform.localScale = Vector3.one * (Constants.CellWorldSize - Constants.CellGap);

                var cellRenderer = cellObject.AddComponent<SpriteRenderer>();
                cellRenderer.sprite = PlaceholderSprite.GetSolid(Color.white);
                _cellRenderers[Index(x, y)] = cellRenderer;
            }
        }

        if (_flashPool == null)
        {
            _flashPool = new ObjectPool<SpriteRenderer>(
                factory: CreateFlashRenderer,
                onGet: r => r.gameObject.SetActive(true),
                onReturn: r => r.gameObject.SetActive(false));
        }

        RefreshAllCells();
    }

    // CLAUDE.md §3.8: cleared cells "flash white (100ms), then dissolve".
    // Simplified from the spec's literal particle-dissolve to a fading
    // white overlay — no final particle art exists yet (Phase 5/8), and
    // this conveys the same beat (flash, then fade away) without a full
    // ParticleSystem. The underlying cell colour is already updated to
    // empty by the time this plays (RefreshCell already ran) — this is a
    // pure visual overlay on top, not a delay of the logical clear.
    public void PlayClearFlash(IEnumerable<Vector2Int> cells)
    {
        foreach (Vector2Int cell in cells)
        {
            SpriteRenderer flash = _flashPool.Get();
            Vector3 localPos = CellToLocalPosition(cell.x, cell.y);
            flash.transform.localPosition = new Vector3(localPos.x, localPos.y, FlashZOffset);
            flash.transform.localScale = Vector3.one * (Constants.CellWorldSize - Constants.CellGap);
            flash.color = Color.white;

            SpriteRenderer capturedFlash = flash;

            // DOTween.ToAlpha directly, not the SpriteRenderer.DOFade
            // extension from DOTweenModuleSprite.cs — that module's
            // extension methods aren't visible from this assembly (a
            // Plugins-folder script outside any asmdef; other DOTween
            // core calls like DOTween.Sequence() work fine, only the
            // Modules/-specific extension methods don't resolve). Calling
            // the same underlying core API DOFade wraps internally
            // sidesteps the issue entirely rather than chasing Unity's
            // assembly resolution further.
            Tween fade = DOTween.ToAlpha(() => capturedFlash.color, c => capturedFlash.color = c, 0f, Constants.ClearFadeDurationSeconds);
            DOTween.Sequence()
                .AppendInterval(Constants.ClearFlashDurationSeconds)
                .Append(fade)
                .OnComplete(() => _flashPool.Return(capturedFlash));
        }
    }

    private SpriteRenderer CreateFlashRenderer()
    {
        var flashObject = new GameObject("ClearFlash");
        flashObject.transform.SetParent(transform, false);
        var flashRenderer = flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = PlaceholderSprite.GetSolid(Color.white);
        return flashRenderer;
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
        SpriteRenderer cellRenderer = _cellRenderers[Index(x, y)];
        bool filled = _board.IsFilled(x, y);

        if (filled)
        {
            int colourId = _board.GetColourId(x, y);
            cellRenderer.sprite = UiPalette.GetBlockSprite(colourId);
            cellRenderer.color = UiPalette.GetBlockColour(colourId);
        }
        else
        {
            cellRenderer.sprite = PlaceholderSprite.GetSolid(Color.white);
            cellRenderer.color = UiPalette.EmptyCellFill;
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
