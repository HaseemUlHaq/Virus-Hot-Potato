using UnityEngine;
using Oculus.Interaction.Input;
using System.Linq;

/// <summary>
/// Automatically finds and assigns Left/Right hand references to HandRef components
/// when the virus spawns. Add this to your virus prefab.
/// </summary>
public class VirusHandConnector : MonoBehaviour
{
    [Header("Auto-find these HandRefs on this GameObject")]
    [SerializeField] private HandRef leftHandRef;
    [SerializeField] private HandRef rightHandRef;

    [Header("Optional: Manually specify scene hands if auto-find fails")]
    [SerializeField] private Hand leftHandOverride;
    [SerializeField] private Hand rightHandOverride;

    private void Start()
    {
        // Find HandRefs on this object if not assigned
        if (leftHandRef == null || rightHandRef == null)
        {
            HandRef[] handRefs = GetComponents<HandRef>();
            if (handRefs.Length >= 2)
            {
                leftHandRef = handRefs[0];
                rightHandRef = handRefs[1];
            }
        }

        // Find scene hands
        Hand leftHand = leftHandOverride;
        Hand rightHand = rightHandOverride;

        if (leftHand == null || rightHand == null)
        {
            Hand[] sceneHands = FindObjectsByType<Hand>(FindObjectsSortMode.None);

            foreach (var hand in sceneHands)
            {
                if (hand.Handedness == Handedness.Left)
                    leftHand = hand;
                else if (hand.Handedness == Handedness.Right)
                    rightHand = hand;
            }
        }

        // Assign to HandRefs
        if (leftHandRef != null && leftHand != null)
        {
            leftHandRef.InjectHand(leftHand);
            Debug.Log($"[VirusHandConnector] Connected Left Hand to {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[VirusHandConnector] Could not connect Left Hand!");
        }

        if (rightHandRef != null && rightHand != null)
        {
            rightHandRef.InjectHand(rightHand);
            Debug.Log($"[VirusHandConnector] Connected Right Hand to {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[VirusHandConnector] Could not connect Right Hand!");
        }
    }
}