using Fusion;
using UnityEngine;

public class VirusSpawner : MonoBehaviour
{
    public NetworkObject VirusPrefab;
    private bool _virusSpawned = false;

    public void PlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_virusSpawned) return;

        // Only the master client spawns the virus
        if (runner.IsSharedModeMasterClient)
        {
            runner.Spawn(VirusPrefab, new Vector3(0, 0.05f, 0), Quaternion.identity);
            _virusSpawned = true;
        }
    }
}