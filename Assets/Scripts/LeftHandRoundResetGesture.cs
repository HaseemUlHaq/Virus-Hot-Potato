using Fusion;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// Left-hand index pinch held longer than <see cref="holdDurationSeconds"/> requests a round reset.
/// Right hand stays free for the Meta system menu.
/// </summary>
public class LeftHandRoundResetGesture : MonoBehaviour
{
    [Header("Reset trigger")]
    [Tooltip("Seconds the left index pinch must be held (must be > 2).")]
    [SerializeField] private float holdDurationSeconds = 2.05f;
    [SerializeField] private float resetCooldownSeconds = 3f;

    [Header("References (auto-found if empty)")]
    [SerializeField] private Hand leftHand;
    [SerializeField] private NetworkedTableAnchor networkedTableAnchor;

    private float _pinchHoldTimer;
    private float _cooldownTimer;

    private void OnEnable()
    {
        TryFindLeftHand();
        if (networkedTableAnchor == null)
            networkedTableAnchor = FindFirstObjectByType<NetworkedTableAnchor>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        TryFindLeftHand();
        if (networkedTableAnchor == null || leftHand == null)
        {
            _pinchHoldTimer = 0f;
            return;
        }

        if (!networkedTableAnchor.CanLocalPlayerRequestRoundReset())
        {
            _pinchHoldTimer = 0f;
            return;
        }

        if (IsLocalPlayerHoldingAnyVirus())
        {
            _pinchHoldTimer = 0f;
            return;
        }

        if (!IsLeftIndexPinching())
        {
            _pinchHoldTimer = 0f;
            return;
        }

        _pinchHoldTimer += Time.deltaTime;
        if (_pinchHoldTimer < holdDurationSeconds)
            return;

        _pinchHoldTimer = 0f;
        _cooldownTimer = resetCooldownSeconds;
        networkedTableAnchor.RequestRoundReset();
        Debug.Log($"[LeftHandRoundReset] Round reset requested (held {holdDurationSeconds:F2}s)");
    }

    private void TryFindLeftHand()
    {
        if (leftHand != null) return;

        foreach (Hand hand in FindObjectsByType<Hand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hand.Handedness != Handedness.Left) continue;
            leftHand = hand;
            return;
        }
    }

    private bool IsLeftIndexPinching()
    {
        if (!leftHand.GetRootPose(out _)) return false;
        return leftHand.GetIndexFingerIsPinching();
    }

    private static bool IsLocalPlayerHoldingAnyVirus()
    {
        NetworkRunner runner = FindActiveRunner();
        if (runner == null) return false;

        PlayerRef local = runner.LocalPlayer;
        foreach (NetworkGrabbableVirus virus in FindObjectsByType<NetworkGrabbableVirus>(FindObjectsSortMode.None))
        {
            if (virus == null) continue;
            if (virus.CurrentHolder == local)
                return true;
        }

        return false;
    }

    private static NetworkRunner FindActiveRunner()
    {
        foreach (NetworkRunner runner in FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
        {
            if (runner != null && runner.IsRunning)
                return runner;
        }

        return null;
    }
}
