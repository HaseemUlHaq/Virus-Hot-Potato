using Fusion;
using UnityEngine;

// Spawns placeholder formation and work viruses from table QR.
// ExampleFormation is spawned separately by BoxAnchor once the box QR is detected.
public class FormationManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject exampleFormationPrefab;
    [SerializeField] private NetworkObject placeholderFormationPrefab;
    [SerializeField] private NetworkObject virusWorkPrefab;

    [Header("Formation Data")]
    [Tooltip("One entry per round. FormationManager cycles through these in order.")]
    [SerializeField] private VirusFormationData[] formationDataPerRound;

    private int _currentRoundIndex = 0;
    private VirusFormationData formationData => formationDataPerRound != null && formationDataPerRound.Length > 0
        ? formationDataPerRound[_currentRoundIndex % formationDataPerRound.Length]
        : null;

    [Header("Offsets from table QR position")]
    [Tooltip("Where the placeholder formation hovers — above the centre of the work area.")]
    [SerializeField] private Vector3 placeholderOffset = new Vector3(0.1f, 0.35f, 0.2f);
    [Tooltip("Base spawn position for work viruses on the table surface.")]
    [SerializeField] private Vector3 workVirusBaseOffset = new Vector3(-0.1f, 0.15f, 0f);
    [Tooltip("Spacing between spawned work viruses.")]
    [SerializeField] private Vector3 workVirusSpacing = new Vector3(0.1f, 0f, 0f);

    private bool _spawned;
    private bool _exampleSpawned;
    public bool HasSpawned => _spawned;

    // Called by VirusSpawner when table QR fires — spawns placeholder + work viruses only
    public void TrySpawnFormations(NetworkRunner masterRunner, Vector3 tablePosition)
    {
        if (_spawned || masterRunner == null || formationData == null) return;

        SpawnPlaceholderFormation(masterRunner, tablePosition);
        SpawnWorkViruses(masterRunner, tablePosition);

        _spawned = true;
        Debug.Log("[FormationManager] Placeholder and work viruses spawned.");
    }

    // Called by BoxAnchor when box QR fires — spawns ExampleFormation inside the box
    public void TrySpawnExampleFormation(NetworkRunner masterRunner, Vector3 boxQRPosition)
    {
        if (_exampleSpawned || masterRunner == null || formationData == null) return;
        if (exampleFormationPrefab == null) return;

        Vector3 pos = boxQRPosition; // offset already applied in QR local space by BoxAnchor
        Debug.Log($"[FormationManager] SpawnExampleFormation — interior pos:{pos}");

        NetworkObject obj = masterRunner.Spawn(exampleFormationPrefab, pos, Quaternion.identity);
        if (obj == null) return;

        ExampleVirusFormation formation = obj.GetComponent<ExampleVirusFormation>();
        if (formation != null)
            formation.ApplyFormationData(formationData);

        _exampleSpawned = true;
    }

    public void ResetForNewRound()
    {
        _spawned = false;
        _exampleSpawned = false;
        _currentRoundIndex++;
    }

    private void SpawnPlaceholderFormation(NetworkRunner runner, Vector3 tablePosition)
    {
        if (placeholderFormationPrefab == null) return;

        Vector3 pos = tablePosition + placeholderOffset;
        NetworkObject obj = runner.Spawn(placeholderFormationPrefab, pos, Quaternion.identity);
        if (obj == null) return;

        PlaceholderFormation formation = obj.GetComponent<PlaceholderFormation>();
        if (formation != null)
        {
            formation.ConfigureSlots(formationData);
            BuildPlaceholderConnectionLines(formation);
        }
    }

    private void SpawnWorkViruses(NetworkRunner runner, Vector3 tablePosition)
    {
        if (virusWorkPrefab == null || formationData == null) return;

        for (int i = 0; i < formationData.slots.Length; i++)
        {
            Vector3 pos = tablePosition + workVirusBaseOffset + workVirusSpacing * i;
            runner.Spawn(virusWorkPrefab, pos, Quaternion.identity);
        }
    }

    private void BuildPlaceholderConnectionLines(PlaceholderFormation formation)
    {
        if (formation == null || formationData == null) return;
        // TODO: dynamic line creation
    }
}
