using UnityEngine;

/// <summary>
/// Renders the 8×8 board (CLAUDE.md §7.5): 64 SpriteRenderers created once
/// in <see cref="Awake"/> and never instantiated/destroyed again — only
/// their colour/scale mutate. Owns the live <see cref="BoardState"/> and
/// exposes cell↔world conversions for drag/ghost/snap logic.
/// </summary>
public sealed class GridManager : MonoBehaviour
{
    private readonly SpriteRenderer[] _cellRenderers = new SpriteRenderer[Constants.GridSize * Constants.GridSize];

    private BoardState _board;

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

        RefreshAllCells();
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
        cellRenderer.color = filled ? UiPalette.GetBlockColour(_board.GetColourId(x, y)) : UiPalette.EmptyCellFill;
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
