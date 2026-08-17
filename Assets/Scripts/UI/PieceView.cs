using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders one piece instance as a set of pooled block sprites, centred
/// around local (0,0) regardless of the piece's own cell-origin — reused
/// for both the piece tray and the actively-dragged piece so no
/// GameObjects are destroyed once created. Reassigning a new
/// <see cref="PieceDefinition"/> returns the old blocks to the pool and
/// gets fresh ones.
/// </summary>
public sealed class PieceView : MonoBehaviour
{
    private ObjectPool<SpriteRenderer> _blockPool;
    private readonly List<SpriteRenderer> _activeBlocks = new List<SpriteRenderer>();

    public PieceDefinition CurrentPiece { get; private set; }
    public IReadOnlyList<SpriteRenderer> ActiveBlocks => _activeBlocks;

    private void Awake()
    {
        Initialize();
    }

    // Separated from Awake(): AddComponent doesn't reliably invoke Awake()
    // synchronously outside Play Mode (see PROGRESS.md), so construction
    // needs to be independently callable from tests.
    public void Initialize()
    {
        if (_blockPool != null)
        {
            return;
        }

        _blockPool = new ObjectPool<SpriteRenderer>(
            factory: CreateBlockRenderer,
            onGet: block => block.gameObject.SetActive(true),
            onReturn: block => block.gameObject.SetActive(false));
    }

    public void SetPiece(PieceDefinition piece, int colourId, float blockScale)
    {
        SetPiece(piece, UiPalette.GetBlockColour(colourId), blockScale);
    }

    public void SetPiece(PieceDefinition piece, Color colour, float blockScale)
    {
        Initialize();
        ClearBlocks();

        if (piece == null || piece.cells == null || piece.cells.Length == 0)
        {
            CurrentPiece = null;
            return;
        }

        CurrentPiece = piece;
        Vector2 boundsCenter = ComputeCellBoundsCenter(piece.cells);

        foreach (Vector2Int cell in piece.cells)
        {
            SpriteRenderer block = _blockPool.Get();
            block.color = colour;
            block.transform.SetParent(transform, false);

            float localX = (cell.x - boundsCenter.x) * Constants.CellWorldSize * blockScale;
            float localY = -(cell.y - boundsCenter.y) * Constants.CellWorldSize * blockScale;
            block.transform.localPosition = new Vector3(localX, localY, 0f);
            block.transform.localScale = Vector3.one * (Constants.CellWorldSize - Constants.CellGap) * blockScale;

            _activeBlocks.Add(block);
        }
    }

    internal static Vector2 ComputeCellBoundsCenter(Vector2Int[] cells)
    {
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (Vector2Int cell in cells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    private void ClearBlocks()
    {
        foreach (SpriteRenderer block in _activeBlocks)
        {
            _blockPool.Return(block);
        }

        _activeBlocks.Clear();
    }

    private SpriteRenderer CreateBlockRenderer()
    {
        var blockObject = new GameObject("Block");
        var blockRenderer = blockObject.AddComponent<SpriteRenderer>();
        blockRenderer.sprite = PlaceholderSprite.GetSolid(Color.white);
        return blockRenderer;
    }
}
