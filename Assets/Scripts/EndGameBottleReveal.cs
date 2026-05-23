using System.Collections;
using UnityEngine;

// Attach to any scene object. Watches PlaceholderFormation.IsComplete and enables
// all spray bottles locally after a delay when puzzle is solved.
// Place 3 bottles at different positions under TableRoot so co-located players
// don't reach for the same spot.
public class EndGameBottleReveal : MonoBehaviour
{
    [SerializeField] private PlaceholderFormation placeholderFormation;
    [SerializeField] private GameObject[] sprayBottles;
    [SerializeField] private GameObject[] uiLabels;
    [SerializeField] private float revealDelay = 2f;
    [Tooltip("Skip the puzzle gate and reveal bottles immediately on Start. Disable before shipping.")]
    [SerializeField] private bool debugRevealOnStart = false;

    private bool _triggered;

    private void Start()
    {
        if (debugRevealOnStart)
        {
            _triggered = true;
            StartCoroutine(RevealAfterDelay());
        }
    }

    private void Update()
    {
        if (_triggered) return;
        if (placeholderFormation == null || !placeholderFormation.IsComplete) return;

        _triggered = true;
        StartCoroutine(RevealAfterDelay());
    }

    private IEnumerator RevealAfterDelay()
    {
        yield return new WaitForSeconds(revealDelay);

        foreach (var bottle in sprayBottles)
            if (bottle != null) bottle.SetActive(true);

        foreach (var label in uiLabels)
            if (label != null) label.SetActive(true);

        Debug.Log("[EndGame] Spray bottles revealed.");
    }
}
