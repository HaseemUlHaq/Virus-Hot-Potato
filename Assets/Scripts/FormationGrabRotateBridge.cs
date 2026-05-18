using Fusion;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// Listens to Meta Interaction grab events and drives networked Y rotation on <see cref="PlaceholderFormation"/>.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class FormationGrabRotateBridge : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private PlaceholderFormation formation;
    [SerializeField] private Transform rotationPivot;

    [Tooltip("Ignore tiny hand jitter (degrees).")]
    [SerializeField] private float minDeltaDegrees = 0.25f;

    private bool _grabbing;
    private float _lastHandAngleY;

    private void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();
        if (formation == null)
            formation = GetComponent<PlaceholderFormation>();
        if (rotationPivot == null && formation != null)
            rotationPivot = formation.FormationRoot != null ? formation.FormationRoot : formation.transform;
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (formation == null || rotationPivot == null) return;

        switch (evt.Type)
        {
            case PointerEventType.Select:
                BeginGrab(evt);
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                _grabbing = false;
                break;

            case PointerEventType.Move:
                if (_grabbing)
                    UpdateGrab(evt);
                break;
        }
    }

    private void BeginGrab(PointerEvent evt)
    {
        NetworkObject netObj = formation.Object;
        if (netObj != null && netObj.IsValid && !netObj.HasStateAuthority)
            netObj.RequestStateAuthority();

        if (!TryGetPointerWorldPosition(evt, out Vector3 handPos))
            return;

        _lastHandAngleY = GetYawAroundPivot(handPos);
        _grabbing = true;
    }

    private void UpdateGrab(PointerEvent evt)
    {
        if (!TryGetPointerWorldPosition(evt, out Vector3 handPos))
            return;

        float angle = GetYawAroundPivot(handPos);
        float delta = Mathf.DeltaAngle(_lastHandAngleY, angle);
        _lastHandAngleY = angle;

        if (Mathf.Abs(delta) < minDeltaDegrees)
            return;

        formation.AddRotationDegrees(delta);
    }

    private float GetYawAroundPivot(Vector3 worldPoint)
    {
        Vector3 offset = worldPoint - rotationPivot.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 1e-6f)
            return _lastHandAngleY;
        return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    private static bool TryGetPointerWorldPosition(PointerEvent evt, out Vector3 position)
    {
        position = default;
        object data = evt.Data;
        if (data == null) return false;

        if (data is HandGrabInteractor handGrab && handGrab.Hand != null &&
            handGrab.Hand.GetRootPose(out Pose handPose))
        {
            position = handPose.position;
            return true;
        }

        if (data is DistanceHandGrabInteractor distanceGrab && distanceGrab.Hand != null &&
            distanceGrab.Hand.GetRootPose(out Pose distPose))
        {
            position = distPose.position;
            return true;
        }

        if (data is IHand hand && hand.GetRootPose(out Pose iHandPose))
        {
            position = iHandPose.position;
            return true;
        }

        if (data is Component component)
        {
            position = component.transform.position;
            return true;
        }

        if (data is GameObject go)
        {
            position = go.transform.position;
            return true;
        }

        return false;
    }
}
