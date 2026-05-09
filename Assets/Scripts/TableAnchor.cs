using UnityEngine;
using Meta.XR.MRUtilityKit;

public class TableAnchor : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject virtualTable;
    [SerializeField] private VirusSpawner virusSpawner;

    [Header("Tune these in Inspector")]
    [SerializeField] private float yRotationOffset = 90f;

    // Any script can read these
    public static Vector3 TableSurfacePosition { get; private set; }
    public static bool TableFound { get; private set; } = false;

    void Start()
    {
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
    }

    void OnSceneLoaded()
    {
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        if (room == null)
        {
            UnityEngine.Debug.LogWarning("MRUK room is null");
            return;
        }

        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                UnityEngine.Debug.Log("Table found at: " + anchor.transform.position);

                // Snap virtual table to real table position
                virtualTable.transform.position = anchor.transform.position;

                // Apply rotation — X fix + Y offset
                Quaternion baseRotation = anchor.transform.rotation *
                                          Quaternion.Euler(90, 0, 0);
                virtualTable.transform.rotation = baseRotation *
                                                   Quaternion.Euler(0, yRotationOffset, 0);

                // Store for other scripts
                TableSurfacePosition = anchor.transform.position;
                TableFound = true;

                // Tell virus spawner where the table is
                if (virusSpawner != null)
                    virusSpawner.SetTablePosition(anchor.transform.position);

                return;
            }
        }

        UnityEngine.Debug.LogWarning("No table found — check room scan labels the table");
    }
}