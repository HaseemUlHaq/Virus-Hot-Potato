using Fusion;
using UnityEngine;

// Spawns the example formation, placeholder formation, and work viruses once the master client has a table position.
public class FormationManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject exampleFormationPrefab;
    [SerializeField] private NetworkObject placeholderFormationPrefab;
    [SerializeField] private NetworkObject virusWorkPrefab;

    [Header("Formation Data")]
    [SerializeField] private VirusFormationData formationData;

    [Header("Offsets from table surface position")]
    [Tooltip("Where the example formation appears — beside/above the table for easy viewing.")]
    [SerializeField] private Vector3 exampleOffset = new Vector3(0.6f, 0.4f, 0f);
    [Tooltip("Where the placeholder formation hovers — directly above the work area.")]
    [SerializeField] private Vector3 placeholderOffset = new Vector3(0f, 0.3f, 0f);
    [Tooltip("Base spawn position for work viruses on the table surface.")]
    [SerializeField] private Vector3 workVirusBaseOffset = new Vector3(-0.3f, 0.15f, 0f);
    [Tooltip("Spacing between spawned work viruses.")]
    [SerializeField] private Vector3 workVirusSpacing = new Vector3(0.15f, 0f, 0f);

    private bool _spawned;

    public void TrySpawnFormations(NetworkRunner masterRunner, Vector3 tablePosition)
    {
        if (_spawned || masterRunner == null || formationData == null) return;

        SpawnExampleFormation(masterRunner, tablePosition);
        SpawnPlaceholderFormation(masterRunner, tablePosition);
        SpawnWorkViruses(masterRunner, tablePosition);

        _spawned = true;
        Debug.Log("[FormationManager] Formations and work viruses spawned.");
    }

    public void ResetForNewRound()
    {
        _spawned = false;
    }

    private void SpawnExampleFormation(NetworkRunner runner, Vector3 tablePosition)
    {
        if (exampleFormationPrefab == null) return;

        Vector3 pos = tablePosition + exampleOffset;
        NetworkObject obj = runner.Spawn(exampleFormationPrefab, pos, Quaternion.identity);
        if (obj == null) return;

        ExampleVirusFormation formation = obj.GetComponent<ExampleVirusFormation>();
        if (formation != null)
            formation.ApplyFormationData(formationData);

        BuildExampleConnectionLines(formation);
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

    private void BuildExampleConnectionLines(ExampleVirusFormation formation)
    {
        if (formation == null || formationData == null) return;
        // TODO: dynamic line creation
    }

    private void BuildPlaceholderConnectionLines(PlaceholderFormation formation)
    {
        if (formation == null || formationData == null) return;
        // TODO: dynamic line creation
    }
}
