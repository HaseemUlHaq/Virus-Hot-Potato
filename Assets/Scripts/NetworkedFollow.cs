using Fusion;
using UnityEngine;

public class NetworkedFollow : NetworkBehaviour
{
    public GameObject LocalPlayerObject;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || LocalPlayerObject == null) return;
        transform.SetPositionAndRotation(LocalPlayerObject.transform.position, LocalPlayerObject.transform.rotation);
    }
}
