using Fusion;
using UnityEngine;

public class ToolboxController : NetworkBehaviour
{
    [Header("Handle Detection")]
    [SerializeField] private Transform handleAnchor;
    [SerializeField] private float grabRadius = 0.10f;
    [Tooltip("Seconds hand must stay near handle before door opens.")]
    [SerializeField] private float holdDuration = 1.0f;

    private float _holdTimer = 0f;

    [Header("Door")]
    [SerializeField] private GameObject virtualDoorPanel;

    [Header("Interior")]
    [SerializeField] private GameObject interiorObjects;

    [Header("Hands — drag from scene or auto-found")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Networked, OnChangedRender(nameof(OnDoorOpened))]
    public NetworkBool IsOpen { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        if (interiorObjects != null)
            interiorObjects.SetActive(false);

        if (leftHand == null || rightHand == null)
            AutoFindHands();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || IsOpen) return;

        if (leftHand == null || rightHand == null)
            AutoFindHands();

        if (IsHandNear())
        {
            _holdTimer += Runner.DeltaTime;
            if (_holdTimer >= holdDuration)
                RPC_Open();
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Open(RpcInfo info = default)
    {
        Debug.Log("[Toolbox] Door opened!");
        IsOpen = true;
    }

    private void OnDoorOpened()
    {
        if (virtualDoorPanel != null)
            virtualDoorPanel.SetActive(!IsOpen);

        if (interiorObjects != null)
            interiorObjects.SetActive(IsOpen);

        Debug.Log($"[Toolbox] OnDoorOpened → IsOpen:{IsOpen}");
    }

    private bool IsHandNear()
    {
        if (handleAnchor == null) return false;

        if (leftHand != null && Vector3.Distance(leftHand.position, handleAnchor.position) < grabRadius)
            return true;
        if (rightHand != null && Vector3.Distance(rightHand.position, handleAnchor.position) < grabRadius)
            return true;

        return false;
    }

    private void AutoFindHands()
    {
        // Tries to find hand transforms from NetworkedHandSimple instances in the scene
        var hands = FindObjectsByType<NetworkedHandSimple>(FindObjectsSortMode.None);
        foreach (var hand in hands)
        {
            if (!hand.Object.HasStateAuthority) continue;
            if (hand.IsLeftHand && leftHand == null)
                leftHand = hand.transform;
            else if (!hand.IsLeftHand && rightHand == null)
                rightHand = hand.transform;
        }

        if (leftHand == null || rightHand == null)
            Debug.LogWarning("[Toolbox] Could not auto-find hand transforms — drag them in the Inspector.");
        else
            Debug.Log("[Toolbox] ✓ Hands auto-found.");
    }
}
