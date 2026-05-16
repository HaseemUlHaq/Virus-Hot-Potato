using Fusion;
using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;

public class NetworkedHandSimple : NetworkBehaviour
{
    public HandVisual PlayerHandVisual;

    [Networked] public NetworkBool IsLeftHand { get; set; }

    [SerializeField] private List<Transform> jointTransformsGhostHand = new List<Transform>();

    [Networked, Capacity(26)] public NetworkArray<Vector3> JointPositions => default;
    [Networked, Capacity(26)] private NetworkArray<Quaternion> JointRotations => default;

    public Vector3 GetPalmPosition() => JointPositions.Get(0);

    public override void Spawned()
    {
        // Remote (opposite) player's ghost hand visual is broken with spatial anchors —
        // hide the mesh but keep joint data flowing so interactions still work.
        if (!HasStateAuthority)
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (PlayerHandVisual == null) return;

        var joints = PlayerHandVisual.Joints;
        for (int i = 0; i < joints.Count && i < 26; i++)
        {
            JointPositions.Set(i, joints[i].position);
            JointRotations.Set(i, joints[i].rotation);
        }
    }

    public override void Render()
    {
        if (HasStateAuthority) return;

        for (int i = 0; i < jointTransformsGhostHand.Count && i < 26; i++)
        {
            if (jointTransformsGhostHand[i] != null)
                jointTransformsGhostHand[i].SetPositionAndRotation(
                    JointPositions.Get(i),
                    JointRotations.Get(i));
        }
    }
}
