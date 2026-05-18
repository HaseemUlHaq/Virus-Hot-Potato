using Oculus.Interaction.Input;
using Oculus.Interaction.Samples;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Add to the virus prefab root.
/// Injects scene references that can't be serialized in a prefab:
///   - HandRef._hand  (IHand from the scene rig)
///   - LookAtTarget._target  (CenterEyeAnchor camera transform)
///
/// DefaultExecutionOrder(-200) means this Awake() runs before HandRef.Start()
/// and LookAtTarget.Start() fire their assertions. Injection order for HandRefs:
///   handRefs[0] = Left hand, handRefs[1] = Right hand
/// (matches the order the HandRef components appear on the prefab root).
/// </summary>
[DefaultExecutionOrder(-200)]
public class VirusRuntimeConnector : MonoBehaviour
{
    private void Awake()
    {
        InjectHandRefs();
        InjectLookAtCameraTargets();
    }

    private void InjectHandRefs()
    {
        HandRef[] handRefs = GetComponentsInChildren<HandRef>(true);
        if (handRefs.Length == 0) return;

        Hand[] sceneHands = FindObjectsByType<Hand>(FindObjectsSortMode.None);
        Hand leftHand = null, rightHand = null;

        foreach (var hand in sceneHands)
        {
            try
            {
                if (hand.Handedness == Handedness.Left && leftHand == null) leftHand = hand;
                else if (hand.Handedness == Handedness.Right && rightHand == null) rightHand = hand;
            }
            catch { }
        }

        if (handRefs.Length > 0 && leftHand != null)
        {
            handRefs[0].InjectHand(leftHand);
            Debug.Log("[VirusRuntimeConnector] Injected Left hand into HandRef[0]");
        }
        if (handRefs.Length > 1 && rightHand != null)
        {
            handRefs[1].InjectHand(rightHand);
            Debug.Log("[VirusRuntimeConnector] Injected Right hand into HandRef[1]");
        }

        if (leftHand == null) Debug.LogWarning("[VirusRuntimeConnector] Left Hand not found in scene!");
        if (rightHand == null) Debug.LogWarning("[VirusRuntimeConnector] Right Hand not found in scene!");
    }

    private void InjectLookAtCameraTargets()
    {
        LookAtTarget[] targets = GetComponentsInChildren<LookAtTarget>(true);
        if (targets.Length == 0) return;

        Transform cam = Camera.main?.transform;
        if (cam == null)
        {
            Debug.LogWarning("[VirusRuntimeConnector] Camera.main not found — LookAtTarget._target not injected.");
            return;
        }

        var field = typeof(LookAtTarget).GetField("_target",
            BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var t in targets)
            field?.SetValue(t, cam);

        Debug.Log($"[VirusRuntimeConnector] Injected camera into {targets.Length} LookAtTarget(s)");
    }
}
