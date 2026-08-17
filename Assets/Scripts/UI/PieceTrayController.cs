using UnityEngine;

/// <summary>
/// Displays the current 3-piece hand below the grid (CLAUDE.md §3.3):
/// fixed-width slots (grid-width ÷ 3), each piece centred within its slot
/// (via <see cref="PieceView"/>) at 0.7× scale.
/// </summary>
public sealed class PieceTrayController : MonoBehaviour
{
    private PieceView[] _slots;

    public PieceView[] Slots => _slots;

    private void Awake()
    {
        BuildSlots();
    }

    // Separated from Awake() for the same reason as GridManager.BuildGrid —
    // see PROGRESS.md.
    public void BuildSlots()
    {
        if (_slots != null)
        {
            return;
        }

        _slots = new PieceView[Constants.PieceHandSize];
        float slotWidth = (Constants.GridSize * Constants.CellWorldSize) / Constants.PieceHandSize;

        for (int i = 0; i < Constants.PieceHandSize; i++)
        {
            var slotObject = new GameObject($"TraySlot_{i}");
            slotObject.transform.SetParent(transform, false);

            float slotCenterX = (i - ((Constants.PieceHandSize - 1) * 0.5f)) * slotWidth;
            slotObject.transform.localPosition = new Vector3(slotCenterX, 0f, 0f);

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
