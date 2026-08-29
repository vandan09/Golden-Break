using UnityEngine;

/// <summary>
/// Displays the current 3-piece hand below the grid (CLAUDE.md §3.3):
/// fixed-width slots (grid-width ÷ 3), each piece centred within its slot
/// (via <see cref="PieceView"/>) at 0.7× scale. Visual styling matches
/// Claude Design: rounded tray background (#1c1c36), rounded slot
/// backgrounds (#1e1e38), gap and padding matching the mockup's pixel
/// values at the grid's world-unit scale.
/// </summary>
public sealed class PieceTrayController : MonoBehaviour
{
    private PieceView[] _slots;

    public PieceView[] Slots => _slots;

    private void Awake()
    {
        BuildSlots();
    }

    public void BuildSlots()
    {
        if (_slots != null)
        {
            return;
        }

        _slots = new PieceView[Constants.PieceHandSize];
        float gridWidth = Constants.GridSize * Constants.CellWorldSize;
        float slotWidth = gridWidth / Constants.PieceHandSize;

        // Tray background: rounded rect spanning the full grid width
        ColorUtility.TryParseHtmlString("#1c1c36", out Color trayBg);
        var trayBgObj = new GameObject("TrayBackground");
        trayBgObj.transform.SetParent(transform, false);
        trayBgObj.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        var trayBgRenderer = trayBgObj.AddComponent<SpriteRenderer>();
        trayBgRenderer.sprite = RoundedRectSprite.Get(50);
        trayBgRenderer.color = trayBg;
        trayBgRenderer.drawMode = SpriteDrawMode.Sliced;
        trayBgRenderer.size = new Vector2(gridWidth + 0.4f, slotWidth * 0.82f);

        for (int i = 0; i < Constants.PieceHandSize; i++)
        {
            var slotObject = new GameObject($"TraySlot_{i}");
            slotObject.transform.SetParent(transform, false);

            float slotCenterX = (i - ((Constants.PieceHandSize - 1) * 0.5f)) * slotWidth;
            slotObject.transform.localPosition = new Vector3(slotCenterX, 0f, 0f);

            // Slot background: rounded rect
            var slotBgObj = new GameObject("SlotBackground");
            slotBgObj.transform.SetParent(slotObject.transform, false);
            slotBgObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            var slotBgRenderer = slotBgObj.AddComponent<SpriteRenderer>();
            slotBgRenderer.sprite = RoundedRectSprite.Get(35);
            slotBgRenderer.color = UiPalette.EmptyCellFill;
            slotBgRenderer.drawMode = SpriteDrawMode.Sliced;
            slotBgRenderer.size = new Vector2(slotWidth * 0.9f, slotWidth * 0.75f);

            _slots[i] = slotObject.AddComponent<PieceView>();
        }
    }

    public void SetHand(PieceDefinition[] hand, int[] colourIds)
    {
        BuildSlots();

        for (int i = 0; i < _slots.Length; i++)
        {
            PieceDefinition piece = i < hand.Length ? hand[i] : null;
            int colourId = i < colourIds.Length ? colourIds[i] : 0;
            _slots[i].SetPiece(piece, colourId, Constants.TrayPieceScale);
        }
    }
}
