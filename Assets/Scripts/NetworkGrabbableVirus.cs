using UnityEngine;
using Fusion;
using Oculus.Interaction;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Grabbable))]
public class NetworkGrabbableVirus : NetworkBehaviour
{
    private Grabbable _grabbable;
    private Rigidbody _rb;

    public override void Spawned()
    {
        _grabbable = GetComponent<Grabbable>();
        _rb = GetComponent<Rigidbody>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                // Request authority so THIS client can move it
                Object.RequestStateAuthority();
                // Freeze rigidbody while grabbing
                if (_rb != null) _rb.isKinematic = true;
                break;

            case PointerEventType.Unselect:
                // Release authority so others can grab it
                if (_rb != null) _rb.isKinematic = false;
                Object.ReleaseStateAuthority();
                break;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}