using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Polls touch/mouse input each frame and drives
/// <see cref="PieceController"/>'s drag lifecycle (CLAUDE.md §3.3). Unity's
/// legacy Input class transparently maps single-finger touch to mouse
/// events on Android, so this one code path covers both mouse (Editor) and
/// touch (device).
///
/// Input polling in <see cref="Update"/> is the one exception BUILD_PLAN
/// Part 1 carves out of "no logic in Update() that could be event-driven" —
/// everything this method does is delegated straight to
/// <see cref="PieceController"/>, which is the actually-testable layer.
/// </summary>
public sealed class InputHandler : MonoBehaviour
{
    private PieceController _pieceController;
    private Camera _camera;

    // Gameplay drag input is polled directly from the legacy Input class,
    // not routed through uGUI's EventSystem/GraphicRaycaster — a
    // full-screen overlay panel (Home, Gallery, Settings) sitting visibly
    // on top does NOT, by itself, block a drag on the world-space grid
    // underneath, since that's a completely separate input path from
    // Button.onClick. Any screen that should block gameplay while open
    // must explicitly set this false while showing and true again when
    // it hides. Retroactively found and fixed for GalleryScreen too
    // (Phase 3 never wired this — see PROGRESS.md) while adding it for
    // the new Phase 4 overlay screens that need it.
    public bool InputEnabled { get; set; } = true;

    public void Configure(PieceController pieceController, Camera camera)
    {
        _pieceController = pieceController;
        _camera = camera != null ? camera : Camera.main;
    }

    // Regular play and a Daily Challenge attempt are two fully separate
    // PieceController instances (see that class's own doc comment) that
    // are never both visible/active at once — repointing this one
    // MonoBehaviour's target when GameplayController switches which board
    // is showing is just "who does a raw touch/drag currently get routed
    // to," not shared game state. InputHandler itself owns no game state
    // of its own (only this reference and InputEnabled) to bleed between
    // the two.
    public void SetPieceController(PieceController pieceController)
    {
        _pieceController = pieceController;
    }

    // IsPointerOverGameObject() reports the EventSystem's *previous* frame
    // raycast, so on the touch-down frame it is false whenever this
    // Update() runs before the input module's — a script-execution-order
    // race that made undo-button taps fall through and start a drag on the
    // piece the undo had just restored. Raycasting explicitly here is
    // order-independent.
    private static readonly System.Collections.Generic.List<RaycastResult> RaycastResults =
        new System.Collections.Generic.List<RaycastResult>();

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        RaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, RaycastResults);

        // Only an actual interactive control should swallow the touch. uGUI
        // Text defaults to raycastTarget=true, so treating every hit as
        // blocking would let the score/progress/hint labels veto legitimate
        // drags on the tray and grid behind them.
        // Deliberately ignores Selectable.interactable: the undo button is
        // set non-interactable the moment an undo is spent, and a *disabled*
        // button still occupies that spot on screen. Letting the touch fall
        // through to the world behind it is what made a second undo tap grab
        // the piece the first undo had just returned to the tray and fling it
        // to the button.
        for (int i = 0; i < RaycastResults.Count; i++)
        {
            if (RaycastResults[i].gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (_pieceController == null || _camera == null || !InputEnabled)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(Input.mousePosition)) return;
            Vector3 worldPosition = ScreenToWorld(Input.mousePosition);
            if (_pieceController.TryFindSlotAt(worldPosition, out int slotIndex))
            {
                _pieceController.BeginDrag(slotIndex, worldPosition);
            }
        }
        else if (Input.GetMouseButton(0) && _pieceController.IsDragging)
        {
            _pieceController.UpdateDrag(ScreenToWorld(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0) && _pieceController.IsDragging)
        {
            _pieceController.EndDrag();
        }
    }

    private Vector3 ScreenToWorld(Vector3 screenPosition)
    {
        screenPosition.z = -_camera.transform.position.z;
        return _camera.ScreenToWorldPoint(screenPosition);
    }
}
