using Fusion;
using Oculus.Interaction;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    // player head
    public GameObject PlayerHead;
    public GameObject PlayerPrefab;

    // left hand
    public HandVisual LeftHandVisual;
    public GameObject LeftHandPrefab;

    // right hand
    public HandVisual RightHandVisual;
    public GameObject RightHandPrefab;

    public void PlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (SpectatorSession.LocalIsSpectator)
            return;

        if (player == runner.LocalPlayer)
        {
            Debug.Log("Local Player " + player.PlayerId + " joined!");

            // player head avatar
            var playerObject = runner.Spawn(PlayerPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
            playerObject.GetComponent<NetworkedFollow>().LocalPlayerObject = PlayerHead;

            // only spawn hands on supported Quest headsets
            var headsetType = OVRPlugin.GetSystemHeadsetType();
            if (headsetType == OVRPlugin.SystemHeadset.Meta_Quest_3
                || headsetType == OVRPlugin.SystemHeadset.Meta_Quest_3S
                || headsetType == OVRPlugin.SystemHeadset.Meta_Quest_Pro
                || headsetType == OVRPlugin.SystemHeadset.Oculus_Quest_2)
            {
                // left hand
                var leftHandObject = runner.Spawn(LeftHandPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
                var leftHand = leftHandObject.GetComponent<NetworkedHandSimple>();
                leftHand.PlayerHandVisual = LeftHandVisual;
                leftHand.IsLeftHand = true;

                // hide local player's own hand mesh — they see their real hands through the headset
                var leftHandVisualMeshRenderer = leftHandObject.GetComponentInChildren<SkinnedMeshRenderer>();
                leftHandVisualMeshRenderer.enabled = false;

                // right hand
                var rightHandObject = runner.Spawn(RightHandPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
                var rightHand = rightHandObject.GetComponent<NetworkedHandSimple>();
                rightHand.PlayerHandVisual = RightHandVisual;
                rightHand.IsLeftHand = false;

                var rightHandVisualMeshRenderer = rightHandObject.GetComponentInChildren<SkinnedMeshRenderer>();
                rightHandVisualMeshRenderer.enabled = false;
            }
        }
    }
}
