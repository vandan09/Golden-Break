using UnityEngine;

/// <summary>
/// Orchestrates the current 3-piece hand: tray display, drag lifecycle
/// (lift/follow/ghost/snap/place/bounce-back), and dealing a new hand once
/// all 3 are placed (CLAUDE.md §3.1, §3.3). Reacts to Begin/Update/EndDrag
/// calls — actual touch/mouse polling lives in <see cref="InputHandler"/>.
///
/// The dragged piece and its ghost are separate <see cref="PieceView"/>
/// instances from the tray slots, not the tray slot's own view repurposed
/// — the tray slot's transform never moves, so "bounce back" is just
/// re-showing its content, with no position to restore.
/// </summary>
public sealed class PieceController : MonoBehaviour
{
    private static readonly Color InvalidGhostColour = new Color(1f, 0.25f, 0.25f, 0.5f);
    private const float ValidGhostAlpha = 0.3f;

    // CLAUDE.md §3.3: "lifts above the grid (z-order change so it renders
    // on top)". Grid cells sit at z=0; smaller (more negative) z is closer
    // to a default orthographic camera at z=-10, which Unity's standard
    // back-to-front transparent sprite sorting renders on top of anything
    // farther away. Ghost sits between the two so the actively-dragged
    // piece is always the topmost thing on screen.
    private const float GhostZOffset = -0.5f;
    private const float DragZOffset = -1f;

    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceSpawner _spawner;

    private PieceDefinition[] _hand;
    private int[] _handColourIds;

    private PieceView _dragView;
    private PieceView _ghostView;
    private int _draggedSlotIndex = -1;
    private Vector2Int _lastGhostOrigin;
    private bool _lastGhostValid;

    public bool IsDragging => _draggedSlotIndex >= 0;
    public PieceDefinition[] Hand => _hand;
    public GridManager Grid => _grid;
    public PieceTrayController Tray => _tray;

    public event System.Action OnGameOver;

    public void Configure(GridManager grid, PieceTrayController tray, PieceSpawner spawner)
    {
        _grid = grid;
        _tray = tray;
        _spawner = spawner;

        if (_dragView == null)
        {
            _dragView = CreateChildPieceView("DraggedPiece");
            _ghostView = CreateChildPieceView("GhostPiece");
        }

        DealNewHand();
    }

    public void DealNewHand()
    {
        _hand = _spawner.DealHand(Constants.PieceHandSize);
        _handColourIds = new int[_hand.Length];
        for (int i = 0; i < _handColourIds.Length; i++)
        {
            // Placeholder colour assignment (cycles the palette) — no
            // gameplay meaning per CLAUDE.md §3.9, purely cosmetic.
            _handColourIds[i] = i % UiPalette.BlockColours.Length;
        }

        _tray.SetHand(_hand, _handColourIds);
        CheckGameOver();
    }

    public bool TryFindSlotAt(Vector3 worldPosition, out int slotIndex)
    {
        float slotWidth = (Constants.GridSize * Constants.CellWorldSize) / Constants.PieceHandSize;
        float slotHeight = Constants.GridSize * Constants.CellWorldSize;

        for (int i = 0; i < _tray.Slots.Length; i++)
        {
            if (_hand[i] == null)
            {
                continue;
            }

            Vector3 slotWorldPos = _tray.Slots[i].transform.position;
            if (Mathf.Abs(worldPosition.x - slotWorldPos.x) <= slotWidth * 0.5f &&
                Mathf.Abs(worldPosition.y - slotWorldPos.y) <= slotHeight * 0.5f)
            {
                slotIndex = i;
                return true;
            }
        }

        slotIndex = -1;
        return false;
    }

    public void BeginDrag(int slotIndex, Vector3 worldPosition)
    {
        if (slotIndex < 0 || slotIndex >= _hand.Length || _hand[slotIndex] == null || IsDragging)
        {
            return;
        }

        _draggedSlotIndex = slotIndex;

        PieceDefinition piece = _hand[slotIndex];
        _tray.Slots[slotIndex].SetPiece(null, 0, Constants.TrayPieceScale);

        _dragView.gameObject.SetActive(true);
        _dragView.SetPiece(piece, _handColourIds[slotIndex], Constants.DragPieceScale);
        _dragView.transform.position = new Vector3(worldPosition.x, worldPosition.y, DragZOffset);

        _ghostView.gameObject.SetActive(true);
        UpdateDrag(worldPosition);
    }

    public void UpdateDrag(Vector3 worldPosition)
    {
        if (!IsDragging)
        {
            return;
        }

        _dragView.transform.position = new Vector3(worldPosition.x, worldPosition.y, DragZOffset);

        PieceDefinition piece = _hand[_draggedSlotIndex];
        Vector2 continuousCell = _grid.WorldToContinuousCell(worldPosition);
        Vector2Int origin = PlacementSnapper.ComputeNearestOrigin(piece, continuousCell);
        bool valid = _grid.Board.CanPlace(piece, origin.x, origin.y);

        _lastGhostOrigin = origin;
        _lastGhostValid = valid;

        Color ghostColour;
        if (valid)
        {
            ghostColour = UiPalette.GetBlockColour(_handColourIds[_draggedSlotIndex]);
            ghostColour.a = ValidGhostAlpha;
        }
        else
        {
            ghostColour = InvalidGhostColour;
        }

        _ghostView.SetPiece(piece, ghostColour, Constants.DragPieceScale);

        Vector2 boundsCenter = PieceView.ComputeCellBoundsCenter(piece.cells);
        Vector3 ghostLocalPos = GridManager.CellToLocalPosition(origin.x + boundsCenter.x, origin.y + boundsCenter.y);
        Vector3 ghostWorldPos = _grid.transform.TransformPoint(ghostLocalPos);
        _ghostView.transform.position = new Vector3(ghostWorldPos.x, ghostWorldPos.y, GhostZOffset);
    }

    public void EndDrag()
    {
        if (!IsDragging)
        {
            return;
        }

        int slotIndex = _draggedSlotIndex;
        PieceDefinition piece = _hand[slotIndex];

        if (_lastGhostValid)
        {
            _grid.Board.Place(piece, _lastGhostOrigin.x, _lastGhostOrigin.y, _handColourIds[slotIndex]);
            foreach (Vector2Int cell in piece.cells)
            {
                _grid.RefreshCell(_lastGhostOrigin.x + cell.x, _lastGhostOrigin.y + cell.y);
            }

            _hand[slotIndex] = null;

            if (AllPiecesPlaced())
            {
                DealNewHand();
            }
            else
            {
                // CLAUDE.md §3.1: "This runs after every placement" — not
                // only once a full new hand is dealt. A hand can already be
                // unplaceable with slots still empty from earlier this
                // round.
                CheckGameOver();
            }
        }
        else
        {
            _tray.Slots[slotIndex].SetPiece(piece, _handColourIds[slotIndex], Constants.TrayPieceScale);
        }

        _dragView.gameObject.SetActive(false);
        _ghostView.gameObject.SetActive(false);
        _draggedSlotIndex = -1;
    }

    private void CheckGameOver()
    {
        if (GameOverDetector.IsGameOver(_grid.Board, _hand))
        {
            OnGameOver?.Invoke();
        }
    }

    public bool AllPiecesPlaced()
    {
        if (_hand == null)
        {
            return false;
        }

        foreach (PieceDefinition piece in _hand)
        {
            if (piece != null)
            {
                return false;
            }
        }

        return true;
    }

    private PieceView CreateChildPieceView(string name)
    {
        var viewObject = new GameObject(name);
        viewObject.transform.SetParent(_grid.transform, false);
        PieceView view = viewObject.AddComponent<PieceView>();
        view.Initialize();
        viewObject.SetActive(false);
        return view;
    }
}
