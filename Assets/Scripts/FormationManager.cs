using Fusion;
using System.Collections.Generic;
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
    private Vector3 _cachedExamplePosition;
    private Quaternion _cachedExampleRotation = Quaternion.identity;
    private bool _hasCachedExamplePose;
    private readonly List<NetworkObject> _roundSpawned = new List<NetworkObject>();
    public bool HasSpawned => _spawned;

    // Called by VirusSpawner when table QR fires — spawns placeholder + work viruses only
    public void TrySpawnFormations(NetworkRunner masterRunner, Vector3 tablePosition)
    {
        if (_spawned || masterRunner == null || formationData == null) return;

        SpawnPlaceholderFormation(masterRunner, tablePosition);
        SpawnWorkViruses(masterRunner, tablePosition);
        TrySpawnExampleFormation(masterRunner);

        _spawned = true;
        Debug.Log("[FormationManager] Placeholder, work viruses, and example formation spawned.");
    }

    // Called when box QR is detected or on round reset — uses ExampleFormationSpawnPoint world pose (under TableRoot).
    public void TrySpawnExampleFormation(NetworkRunner masterRunner)
    {
        if (_exampleSpawned || masterRunner == null || formationData == null) return;
        if (exampleFormationPrefab == null) return;

        Vector3 pos;
        Quaternion rot;
        if (_hasCachedExamplePose)
        {
            pos = _cachedExamplePosition;
            rot = _cachedExampleRotation;
        }
        else if (exampleFormationSpawnPoint != null)
        {
            // TableRoot follows NetworkedTableAnchor in Render(); sync now so world pose is correct on first spawn.
            SyncTableRootToAnchor();
            pos = exampleFormationSpawnPoint.position;
            rot = exampleFormationSpawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("[FormationManager] No ExampleFormationSpawnPoint — example formation not spawned.");
            return;
        }

        Debug.Log($"[FormationManager] SpawnExampleFormation at {pos}");

        NetworkObject obj = masterRunner.Spawn(exampleFormationPrefab, pos, rot);
        if (obj == null) return;
        TrackSpawn(obj);

        _cachedExamplePosition = obj.transform.position;
        _cachedExampleRotation = obj.transform.rotation;
        _hasCachedExamplePose = true;

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

    public void DespawnRoundEntities(NetworkRunner runner)
    {
        if (runner == null) return;

        for (int i = _roundSpawned.Count - 1; i >= 0; i--)
        {
            NetworkObject obj = _roundSpawned[i];
            if (obj != null && obj.IsValid)
                runner.Despawn(obj);
        }

        _roundSpawned.Clear();
        _spawned = false;
        _exampleSpawned = false;
    }

    private void SpawnPlaceholderFormation(NetworkRunner runner, Vector3 tablePosition)
    {
        if (placeholderFormationPrefab == null) return;

        Vector3 pos = tablePosition + placeholderOffset;
        NetworkObject obj = runner.Spawn(placeholderFormationPrefab, pos, Quaternion.identity);
        if (obj == null) return;
        TrackSpawn(obj);

        PlaceholderFormation formation = obj.GetComponent<PlaceholderFormation>();
        if (formation != null)
            formation.ConfigureSlots(formationData);
    }

    private void SpawnWorkViruses(NetworkRunner runner, Vector3 tablePosition)
    {
        if (virusWorkPrefab == null || formationData == null) return;

        for (int i = 0; i < formationData.slots.Length; i++)
        {
            Vector3 pos = tablePosition + workVirusBaseOffset + workVirusSpacing * i;
            NetworkObject work = runner.Spawn(virusWorkPrefab, pos, Quaternion.identity);
            if (work != null)
                TrackSpawn(work);
        }
    }

    private void TrackSpawn(NetworkObject obj)
    {
        if (obj != null && !_roundSpawned.Contains(obj))
            _roundSpawned.Add(obj);
    }

    private static void SyncTableRootToAnchor()
    {
        NetworkedTableAnchor anchor = FindFirstObjectByType<NetworkedTableAnchor>(FindObjectsInactive.Include);
        if (anchor == null || !anchor.IsTablePlaced)
            return;

        anchor.transform.SetPositionAndRotation(anchor.PlacedSurfacePosition, anchor.PlacedRotation);
    }

}
