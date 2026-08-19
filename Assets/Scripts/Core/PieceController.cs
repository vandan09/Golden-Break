using System.Collections.Generic;
using DG.Tweening;
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
///
/// One instance is always exactly one session — regular play and a Daily
/// Challenge attempt (CLAUDE.md §4.2) are each their own separate
/// PieceController (with their own GridManager/PieceTrayController), not
/// one instance switching between modes. A real bug caught on-device:
/// an earlier "swap the active spawner and snapshot/restore state"
/// design let the two modes bleed into each other. Full instance
/// separation makes that structurally impossible instead of relying on
/// careful bookkeeping to prevent it.
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

    // Undo support (CLAUDE.md §4.5): captures exactly what the most recent
    // placement did, so it can be exactly reversed. A struct, not a class —
    // this is a small, short-lived value invalidated on the very next
    // placement or hand deal, no identity semantics needed.
    private readonly struct PlacementRecord
    {
        public readonly int SlotIndex;
        public readonly PieceDefinition Piece;
        public readonly Vector2Int Origin;
        public readonly int ColourId;

        public PlacementRecord(int slotIndex, PieceDefinition piece, Vector2Int origin, int colourId)
        {
            SlotIndex = slotIndex;
            Piece = piece;
            Origin = origin;
            ColourId = colourId;
        }
    }

    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceSpawner _spawner;
    private PieceSpawner _standardSpawnerForContinue;
    private ScoreManager _scoreManager;

    private PieceDefinition[] _hand;
    private int[] _handColourIds;

    private PieceView _dragView;
    private PieceView _ghostView;
    private int _draggedSlotIndex = -1;
    private Vector2Int _lastGhostOrigin;
    private bool _lastGhostValid;

    // CLAUDE.md §4.5 undo/refresh restrictions. Both are coin-agnostic
    // here — PieceController (Core) has no knowledge of CoinManager
    // (Meta), matching the same dependency-direction rule
    // CeramicController's own docstring calls out (Core doesn't reach into
    // Meta; Meta reacts to Core's events/calls Core's public API instead).
    // Coin-cost gating belongs to whatever calls TryUndo/TryRefresh (a
    // future UI button handler), not to the mechanic itself.
    private PlacementRecord? _lastPlacement;
    private bool _lastPlacementClearedLines;
    private bool _undoUsedThisHand;
    private bool _refreshUsedThisHand;

    // CLAUDE.md §5.1: one continue per whole game (not per hand, unlike
    // undo/refresh) — reset only by RestartGame, never by DealNewHand.
    private bool _continueUsedThisGame;

    public bool IsDragging => _draggedSlotIndex >= 0;
    public bool IsGameOver { get; private set; }
    public PieceDefinition[] Hand => _hand;
    public GridManager Grid => _grid;
    public PieceTrayController Tray => _tray;
    public ScoreManager Score => _scoreManager;

    // Once game-over fires, the gameplay HUD's undo/refresh buttons are no
    // longer the active interaction surface (the game-over screen is) — so
    // both gate on !IsGameOver alongside their own documented restrictions.
    public bool CanUndo =>
        _lastPlacement.HasValue &&
        !_lastPlacementClearedLines &&
        !_undoUsedThisHand &&
        !IsDragging &&
        !IsGameOver;

    public bool CanRefresh =>
        !_refreshUsedThisHand &&
        AllPiecesUnplaced() &&
        !IsDragging &&
        !IsGameOver;

    // CLAUDE.md §5.1: only offered once game-over has actually fired, and
    // only once per game.
    public bool CanContinue => IsGameOver && !_continueUsedThisGame;

    public event System.Action OnGameOver;
    public event System.Action<LineClearDetector.ClearResult, int> OnLinesCleared;

    // Fires for the very first game (Configure) and every subsequent
    // RestartGame — the single "a new game just began" signal analytics/
    // UI code can hook instead of each caller needing to know about every
    // entry point.
    public event System.Action OnGameStarted;

    // standardSpawnerForContinue defaults to the same spawner as regular
    // dealing when not supplied — keeps every existing single-spawner
    // call site (tests, anywhere DDA/continue distinction doesn't matter)
    // working unchanged. GameplayController passes a genuinely separate,
    // non-DDA-weighted instance per CLAUDE.md §5.1: "using the standard
    // weighted pool, not the DDA-adjusted pool — the continue should feel
    // like a genuine second chance, not an easy handout."
    public void Configure(GridManager grid, PieceTrayController tray, PieceSpawner spawner, ScoreManager scoreManager, PieceSpawner standardSpawnerForContinue = null)
    {
        _grid = grid;
        _tray = tray;
        _spawner = spawner;
        _standardSpawnerForContinue = standardSpawnerForContinue ?? spawner;
        _scoreManager = scoreManager;
        _scoreManager.OnNewBest += PlayNewBestFeedback;

        if (_dragView == null)
        {
            _dragView = CreateChildPieceView("DraggedPiece");
            _ghostView = CreateChildPieceView("GhostPiece");
        }

        DealNewHand();
        OnGameStarted?.Invoke();
    }

    // spawner/setupBoard are both optional, used only by Daily Challenge's
    // "Play Again" (CLAUDE.md §4.2): replaying the same day must deal the
    // identical sequence from the very start, which means a *new*
    // PieceSpawner (its System.Random would otherwise just continue from
    // wherever the previous attempt left off, not restart) and the
    // pre-filled obstacle cells re-applied before the first hand is dealt
    // and checked for game-over, not after — CheckGameOver must see the
    // real starting board, not a temporarily-empty one. Regular play's
    // Play Again never passes either, so it behaves exactly as before.
    public void RestartGame(PieceSpawner spawner = null, System.Action<GridManager> setupBoard = null)
    {
        if (spawner != null)
        {
            _spawner = spawner;
        }

        _grid.Board.Clear();
        setupBoard?.Invoke(_grid);
        _grid.RefreshAllCells();
        _scoreManager.ResetForNewGame();
        IsGameOver = false;
        _continueUsedThisGame = false;
        DealNewHand();
        OnGameStarted?.Invoke();
    }

    // Starts a genuinely new hand-cycle (game start, after a full hand is
    // placed, or a fresh game): resets the per-hand undo/refresh counters,
    // since CLAUDE.md §4.5's "max 1 per hand" resets only when a real new
    // hand begins — not on every internal piece deal (see TryRefresh,
    // which deals fresh pieces via DealHandCore without resetting these).
    public void DealNewHand()
    {
        _undoUsedThisHand = false;
        _refreshUsedThisHand = false;
        _lastPlacement = null;
        DealHandCore(_spawner);
    }

    private void DealHandCore(PieceSpawner spawner)
    {
        _hand = spawner.DealHand(Constants.PieceHandSize);
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
        if (slotIndex < 0 || slotIndex >= _hand.Length || _hand[slotIndex] == null || IsDragging || IsGameOver)
        {
            return;
        }

        _draggedSlotIndex = slotIndex;

        PieceDefinition piece = _hand[slotIndex];
        _tray.Slots[slotIndex].SetPiece(null, 0, Constants.TrayPieceScale);

        AudioManager.Instance?.PlaySound(SoundEffect.PiecePickup);
        HapticManager.Trigger(HapticPattern.Pickup);

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

            LineClearDetector.ClearResult clearResult = LineClearDetector.DetectAndClear(_grid.Board);

            // Recorded regardless of AllPiecesPlaced() below — if this
            // placement completes the hand, DealNewHand() (called a few
            // lines down) immediately nulls this back out, which is
            // exactly CLAUDE.md §4.5's "once all 3 are placed and new
            // pieces are dealt, undo is no longer available."
            _lastPlacement = new PlacementRecord(slotIndex, piece, _lastGhostOrigin, _handColourIds[slotIndex]);
            _lastPlacementClearedLines = clearResult.AnyCleared;

            if (clearResult.AnyCleared)
            {
                var clearedCells = new HashSet<Vector2Int>();

                foreach (int clearedRow in clearResult.ClearedRows)
                {
                    for (int x = 0; x < Constants.GridSize; x++)
                    {
                        _grid.RefreshCell(x, clearedRow);
                        clearedCells.Add(new Vector2Int(x, clearedRow));
                    }
                }

                foreach (int clearedColumn in clearResult.ClearedColumns)
                {
                    for (int y = 0; y < Constants.GridSize; y++)
                    {
                        _grid.RefreshCell(clearedColumn, y);
                        clearedCells.Add(new Vector2Int(clearedColumn, y));
                    }
                }

                _grid.PlayClearFlash(clearedCells);

                bool isCombo = clearResult.TotalLinesCleared >= 2;
                if (isCombo)
                {
                    Camera.main.transform.DOShakePosition(Constants.ComboScreenShakeDurationSeconds, Constants.ComboScreenShakeStrength);
                    AudioManager.Instance?.PlaySound(SoundEffect.ComboClear);
                    HapticManager.Trigger(HapticPattern.Combo);
                }
                else
                {
                    AudioManager.Instance?.PlaySound(SoundEffect.LineClear);
                    HapticManager.Trigger(HapticPattern.Clear);
                }
            }
            else
            {
                AudioManager.Instance?.PlaySound(SoundEffect.PiecePlace);
                HapticManager.Trigger(HapticPattern.Place);
            }

            int pointsAwarded = _scoreManager.ApplyLineClear(clearResult.TotalLinesCleared);
            OnLinesCleared?.Invoke(clearResult, pointsAwarded);

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
            AudioManager.Instance?.PlaySound(SoundEffect.PieceInvalid);
        }

        _dragView.gameObject.SetActive(false);
        _ghostView.gameObject.SetActive(false);
        _draggedSlotIndex = -1;
    }

    private void PlayNewBestFeedback()
    {
        AudioManager.Instance?.PlaySound(SoundEffect.NewBest);
    }

    private void CheckGameOver()
    {
        if (GameOverDetector.IsGameOver(_grid.Board, _hand))
        {
            IsGameOver = true;
            AudioManager.Instance?.PlaySound(SoundEffect.GameOver);
            HapticManager.Trigger(HapticPattern.GameOver);
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

    private bool AllPiecesUnplaced()
    {
        if (_hand == null)
        {
            return false;
        }

        foreach (PieceDefinition piece in _hand)
        {
            if (piece == null)
            {
                return false;
            }
        }

        return true;
    }

    // CLAUDE.md §4.5: reverts exactly the most recent placement — the piece
    // returns to its original tray slot, the cells it filled empty again.
    // Does NOT restore ScoreManager's streak multiplier: a non-clearing
    // placement (the only kind undo can ever apply to, since a clearing
    // placement blocks undo entirely) only ever resets the streak to ×1,
    // never awards points, so the one real side effect undo doesn't reverse
    // is a streak reset that CLAUDE.md's undo spec never mentions
    // restoring — deliberately out of scope, not an oversight.
    public bool TryUndo()
    {
        if (!CanUndo)
        {
            return false;
        }

        PlacementRecord record = _lastPlacement.Value;
        _grid.Board.RemovePiece(record.Piece, record.Origin.x, record.Origin.y);
        foreach (Vector2Int cell in record.Piece.cells)
        {
            _grid.RefreshCell(record.Origin.x + cell.x, record.Origin.y + cell.y);
        }

        _hand[record.SlotIndex] = record.Piece;
        _tray.Slots[record.SlotIndex].SetPiece(record.Piece, record.ColourId, Constants.TrayPieceScale);

        _undoUsedThisHand = true;
        _lastPlacement = null;

        return true;
    }

    // CLAUDE.md §4.5: discards the current (fully-unplaced) hand and deals
    // a fresh weighted-random 3, without resetting the per-hand
    // undo/refresh counters — those only reset on a genuine new hand-cycle
    // (see DealNewHand), so a player can't chain refreshes against their
    // own refreshed hand.
    public bool TryRefresh()
    {
        if (!CanRefresh)
        {
            return false;
        }

        _refreshUsedThisHand = true;
        _lastPlacement = null;
        DealHandCore(_spawner);

        return true;
    }

    // CLAUDE.md §5.1 continue mechanic: clears the bottom 2 rows (Y=6,
    // Y=7) unconditionally — regardless of what was there, unlike a line
    // clear which only fires on a completely full line — discards the
    // hand that caused game-over, and deals a fresh 3 from the *standard*
    // (non-DDA) pool. No score penalty, no crack un-repair: this only
    // touches the board and the hand. Re-runs game-over detection
    // afterward since clearing 16 cells could still (rarely) fail to open
    // up a valid move for an unlucky new hand.
    public bool TryContinue()
    {
        if (!CanContinue)
        {
            return false;
        }

        _continueUsedThisGame = true;
        ClearBottomTwoRows();

        IsGameOver = false;
        _lastPlacement = null;
        _undoUsedThisHand = false;
        _refreshUsedThisHand = false;

        DealHandCore(_standardSpawnerForContinue);

        return true;
    }

    private void ClearBottomTwoRows()
    {
        int firstRow = Constants.GridSize - 2;
        for (int y = firstRow; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                _grid.Board.ClearCell(x, y);
                _grid.RefreshCell(x, y);
            }
        }
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
