using Fusion;
using TMPro;
using UnityEngine;

public class VirusDebugSceneManager : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private NetworkGrabbableVirus virus;
    [SerializeField] private float virusSearchIntervalSeconds = 1f;

    private float _nextSearchTime;

    private void Update()
    {
        if (debugText == null)
            return;

        if (virus == null && Time.time >= _nextSearchTime)
        {
            virus = FindFirstObjectByType<NetworkGrabbableVirus>();
            _nextSearchTime = Time.time + virusSearchIntervalSeconds;
        }

        if (virus == null)
        {
            debugText.text = "Virus Debug\nWaiting for spawned virus...";
            return;
        }

        NetworkRunner runner = virus.Runner;
        if (runner == null)
        {
            debugText.text = "Virus Debug\nVirus found, waiting for runner...";
            return;
        }

        float remainingSeconds = virus.GetRemainingSeconds();
        string holderText = virus.DebugHasHolder ? virus.DebugCurrentHolder.ToString() : "None";
        string lastTouchedText = virus.DebugLastTouchedPlayer.ToString();
        string eliminatedText = virus.DebugHasElimination ? virus.EliminatedPlayer.ToString() : "None";
        string authorityText = virus.HasStateAuthority ? "Yes" : "No";

        debugText.text =
            $"Fuse Started: {virus.DebugFuseStarted} | Remaining: {remainingSeconds:0.00}s\n" +
            $"Current Holder: {holderText} | Last Touched: {lastTouchedText}\n" +
            $"Round Resolved: {virus.DebugRoundResolved}\n" +
            $"Eliminated Player: {eliminatedText}\n" +
            $"State Authority: {authorityText} | Local Player: {runner.LocalPlayer}";
    }
}
