using UnityEngine;

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

    public void Configure(PieceController pieceController, Camera camera)
    {
        _pieceController = pieceController;
        _camera = camera != null ? camera : Camera.main;
    }

    private void Update()
    {
        if (_pieceController == null || _camera == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
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
