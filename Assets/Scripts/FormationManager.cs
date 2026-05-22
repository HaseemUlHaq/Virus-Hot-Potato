using System.Collections;
using Fusion;
using UnityEngine;

// Spawns placeholder formation and work viruses from table QR.
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

    [Header("Example Formation Spawn Point")]
    [Tooltip("Drag an empty GameObject (FormationSpawnPoint) inside ToolboxRoot here.")]
    [SerializeField] private Transform exampleFormationSpawnPoint;

    [Header("Offsets from table QR position")]
    [Tooltip("Where the placeholder formation hovers — above the centre of the work area.")]
    [SerializeField] private Vector3 placeholderOffset = new Vector3(0.1f, 0.35f, 0.2f);
    [Tooltip("Base spawn position for work viruses on the table surface.")]
    [SerializeField] private Vector3 workVirusBaseOffset = new Vector3(-0.1f, 0.15f, 0f);
    [Tooltip("Spacing between spawned work viruses.")]
    [SerializeField] private Vector3 workVirusSpacing = new Vector3(0.1f, 0f, 0f);

    private bool _spawned;
    private bool _exampleSpawned;
    private bool _workAreaSpawned;
    public bool HasSpawned => _spawned;

    private Vector3 _tablePosition;
    private NetworkRunner _runner;

    // Called by VirusSpawner when table QR fires — spawns example formation only
    public void TrySpawnFormations(NetworkRunner masterRunner, Vector3 tablePosition)
    {
        if (_spawned || masterRunner == null || formationData == null) return;

        _tablePosition = tablePosition;
        _runner = masterRunner;

        // Wait one frame so TableRoot (and ToolboxRoot) have moved to their real world positions
        StartCoroutine(SpawnExampleFormationNextFrame(masterRunner));

        _spawned = true;
        Debug.Log("[FormationManager] Table QR fired — spawning example formation. Work area waits for box trigger.");
    }

    // Called by BoxFrontWallTrigger when player opens the box
    public void SpawnWorkArea()
    {
        if (_runner == null || formationData == null) return;
        if (_workAreaSpawned) return;

        SpawnPlaceholderFormation(_runner, _tablePosition);
        SpawnWorkViruses(_runner, _tablePosition);
        _workAreaSpawned = true;
        Debug.Log("[FormationManager] Box opened — placeholder formation and work viruses spawned.");
    }

    private IEnumerator SpawnExampleFormationNextFrame(NetworkRunner masterRunner)
    {
        yield return null; // wait one frame for TableRoot to move
        TrySpawnExampleFormation(masterRunner);
    }

    public void TrySpawnExampleFormation(NetworkRunner masterRunner)
    {
        if (_exampleSpawned || masterRunner == null || formationData == null) return;
        if (exampleFormationPrefab == null) return;
        if (exampleFormationSpawnPoint == null) { Debug.LogWarning("[FormationManager] ExampleFormationSpawnPoint not assigned!"); return; }

        // TableRoot has already moved — read world position directly
        Vector3 pos = exampleFormationSpawnPoint.position;
        Debug.Log($"[FormationManager] SpawnExampleFormation at {pos}");

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
        _workAreaSpawned = false;
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

    [Header("Spawn Animation")]
    [SerializeField] private float virusSpawnStagger = 0.4f;

    private void SpawnWorkViruses(NetworkRunner runner, Vector3 tablePosition)
    {
        if (virusWorkPrefab == null || formationData == null) return;
        StartCoroutine(SpawnWorkVirusesStaggered(runner, tablePosition));
    }

    private IEnumerator SpawnWorkVirusesStaggered(NetworkRunner runner, Vector3 tablePosition)
    {
        Vector3 placeholderOrigin = tablePosition + placeholderOffset;
        for (int i = 0; i < formationData.slots.Length; i++)
        {
            Vector3 pos = placeholderOrigin + formationData.slots[i].localPosition;
            runner.Spawn(virusWorkPrefab, pos, Quaternion.identity);
            yield return new WaitForSeconds(virusSpawnStagger);
        }
    }

    private void BuildPlaceholderConnectionLines(PlaceholderFormation formation)
    {
        if (formation == null || formationData == null) return;
        // TODO: dynamic line creation
    }
}
