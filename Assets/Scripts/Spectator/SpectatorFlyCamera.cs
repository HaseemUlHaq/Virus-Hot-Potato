#if VIRUS_SPECTATOR
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Desktop fly camera for the PC spectator client (Input System package).
/// </summary>
public class SpectatorFlyCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float fastMoveMultiplier = 2.5f;
    [SerializeField] private float lookSensitivity = 0.15f;

    private float _pitch;
    private float _yaw;
    private bool _cursorLocked;
    private NetworkedTableAnchor _tableAnchor;

    private void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        _pitch = euler.x;
        _yaw = euler.y;
        LockCursor(true);
    }

    private void Update()
    {
        if (_tableAnchor == null)
            _tableAnchor = FindFirstObjectByType<NetworkedTableAnchor>(FindObjectsInactive.Include);

        HandleCursorLockToggle();
        HandleLook();
        HandleMove();

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            FocusOnTable();
    }

    private void HandleCursorLockToggle()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            LockCursor(true);
    }

    private void LockCursor(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleLook()
    {
        if (!_cursorLocked || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
        _yaw += delta.x;
        _pitch -= delta.y;
        _pitch = Mathf.Clamp(_pitch, -85f, 85f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleMove()
    {
        if (Keyboard.current == null)
            return;

        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= fastMoveMultiplier;

        Vector3 move = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        if (Keyboard.current.eKey.isPressed) move += Vector3.up;
        if (Keyboard.current.qKey.isPressed) move -= Vector3.up;

        if (move.sqrMagnitude > 0.0001f)
            transform.position += move.normalized * speed * Time.deltaTime;
    }

    private void FocusOnTable()
    {
        if (!SpectatorTableAnchorQueries.TryGetPlacedState(
                _tableAnchor, out _, out _, out Vector3 surfacePosition))
            return;

        Vector3 focusPoint = surfacePosition + Vector3.up * 0.35f;
        transform.position = focusPoint + new Vector3(0f, 1.2f, -1.8f);
        Vector3 lookDir = focusPoint - transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        Vector3 euler = transform.rotation.eulerAngles;
        _pitch = euler.x;
        _yaw = euler.y;
    }
}
#endif
