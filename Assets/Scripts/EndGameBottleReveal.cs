using System.Collections;
using Fusion;
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
    [SerializeField] private EndGameUI endGameUI;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winClip;
    [Tooltip("Skip the puzzle gate and reveal bottles immediately on Start. Disable before shipping.")]
    [SerializeField] private bool debugRevealOnStart = false;

    private bool _triggered;

    private void Start()
    {
        SetBottlesVisible(false);

        if (debugRevealOnStart)
        {
            _triggered = true;
            StartCoroutine(RevealAfterDelay());
        }
    }

    private void Update()
    {
        if (_triggered) return;

        PlaceholderFormation formation = ResolveFormation();
        if (formation == null || !formation.IsComplete) return;

        _triggered = true;
        StartCoroutine(RevealAfterDelay());
    }

    /// <summary>
    /// FormationManager spawns PlaceholderFormation at runtime; a prefab asset reference in the
    /// Inspector is not the live networked instance.
    /// </summary>
    private PlaceholderFormation ResolveFormation()
    {
        if (IsSpawnedFormation(placeholderFormation))
            return placeholderFormation;

        return FindFirstObjectByType<PlaceholderFormation>();
    }

    private static bool IsSpawnedFormation(PlaceholderFormation formation)
    {
        return formation != null
            && formation.Object != null
            && formation.Object.IsValid;
    }

    private void SetBottlesVisible(bool visible)
    {
        foreach (var bottle in sprayBottles)
            if (bottle != null) bottle.SetActive(visible);

        foreach (var label in uiLabels)
            if (label != null) label.SetActive(visible);
    }

    private IEnumerator RevealAfterDelay()
    {
        yield return new WaitForSeconds(revealDelay);
        SetBottlesVisible(true);
        if (audioSource != null && winClip != null)
            audioSource.PlayOneShot(winClip);
        if (endGameUI != null)
            endGameUI.Show();
        Debug.Log("[EndGame] Spray bottles revealed.");
    }
}
