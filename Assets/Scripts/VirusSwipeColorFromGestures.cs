using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Oculus.Interaction;

using Oculus.Interaction.Input;

using TMPro;

using UnityEngine;



/// <summary>

/// Lives on the <b>Gestures</b> object (parent of HandSwipeGestureLeft / Right).

/// When a swipe gesture becomes active, optionally requires the tracked <see cref="IHand"/>

/// to be within <see cref="maxSwipeDistanceFromVirusMeters"/> of a <see cref="NetworkGrabbableVirus"/>.

/// </summary>

[DisallowMultipleComponent]

public class VirusSwipeColorFromGestures : MonoBehaviour

{

    [Tooltip("Direct children of this object — names must match your Hierarchy.")]

    [SerializeField]

    private string leftGestureObjectName = "HandSwipeGestureLeft";



    [SerializeField]

    private string rightGestureObjectName = "HandSwipeGestureRight";



    [Header("Interaction SDK hands (SwipeForwardGesture _hand)")]

    [Tooltip("If null, the script searches the scene for an IHand with matching Handedness.")]

    [SerializeField]

    private MonoBehaviour leftInteractionHandOverride;



    [SerializeField]

    private MonoBehaviour rightInteractionHandOverride;



    [Header("Proximity (swipe near the virus)")]

    [Tooltip("If enabled, a swipe only recolors viruses when the swiping hand is within the distance below.")]

    [SerializeField]

    private bool requireHandNearVirus = true;



    [SerializeField]

    private float maxSwipeDistanceFromVirusMeters = 0.55f;



    [Tooltip("Optional world-space anchors used only for the distance check (e.g. OVR LeftHandAnchor / RightHandAnchor). When set, these take priority over IHand root pose for proximity.")]

    [SerializeField]

    private Transform leftProximityAnchorOverride;



    [SerializeField]

    private Transform rightProximityAnchorOverride;



    [Header("Colors (persist on the virus until the next swipe)")]

    [SerializeField]

    private Color colorOnLeftSwipe = new Color(0.25f, 0.55f, 1f, 1f);



    [SerializeField]

    private Color colorOnRightSwipe = new Color(1f, 0.35f, 0.25f, 1f);



    [SerializeField]

    private bool logWarnings = true;



    [Header("Diagnostics")]

    [Tooltip("Logs swipe rising edges, hand binding, IActiveState counts, and proximity vs virus. Turn off when done testing.")]

    [SerializeField]

    private bool logSwipeDiagnostics;



    [Tooltip("Appends each diagnostic line to a text file under Application.persistentDataPath (easy to adb pull on Quest).")]

    [SerializeField]

    private bool mirrorSwipeDiagnosticsToFile = true;



    [Tooltip("Draws the last several diagnostic lines on top of the framebuffer (visible in headset and Game view). Unity Console is separate.")]

    [SerializeField]

    private bool showSwipeDiagnosticsHud = true;



    [Tooltip("Optional: assign a world-space or screen TMP label to mirror the same lines.")]

    [SerializeField]

    private TextMeshProUGUI swipeDebugScreenText;



    private const string DiagFileName = "VirusSwipeDiag.log";

    private const int HudMaxLines = 20;

    private readonly List<string> _hudLines = new List<string>(HudMaxLines);

    private GUIStyle _hudBoxStyle;

    private GUIStyle _hudLabelStyle;

    private bool _loggedDiagFilePath;



    private Transform _leftGestureRoot;

    private Transform _rightGestureRoot;



    private readonly List<(MonoBehaviour mb, IActiveState state)> _leftStates = new List<(MonoBehaviour, IActiveState)>();

    private readonly List<(MonoBehaviour mb, IActiveState state)> _rightStates = new List<(MonoBehaviour, IActiveState)>();

    private readonly Dictionary<MonoBehaviour, bool> _wasActive = new Dictionary<MonoBehaviour, bool>();



    private bool _loggedResolve;

    private bool _loggedHandBind;

    private bool _loggedStartupDiag;



    private void Start()

    {

        ResolveGestureRoots();

        TryBindSwipePrefabHandReferences();

        CollectActiveStatesForBothSides();

        LogStartupSummaryOnce();

        LogDiagFilePathOnce();

    }



    private static string DiagFileFullPath => Path.Combine(Application.persistentDataPath, DiagFileName);



    private void LogDiagFilePathOnce()

    {

        if (!logSwipeDiagnostics || !mirrorSwipeDiagnosticsToFile || _loggedDiagFilePath)

            return;

        _loggedDiagFilePath = true;

        string path = DiagFileFullPath;

        Debug.Log("[VirusSwipe] Diagnostics are also appended to file: " + path, this);

        print("[VirusSwipe] Diagnostics file: " + path);

    }



    private void OnGUI()

    {

        if (!logSwipeDiagnostics || !showSwipeDiagnosticsHud)

            return;

        if (_hudLines.Count == 0)

            return;



        if (_hudBoxStyle == null)

        {

            _hudBoxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 18, richText = false };

            _hudLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 18, wordWrap = true, richText = false };

            _hudLabelStyle.normal.textColor = Color.white;

        }



        const float pad = 8f;

        float w = Mathf.Min(Screen.width - pad * 2f, 920f);

        float h = Mathf.Min(Screen.height * 0.35f, 28f * HudMaxLines + pad * 2f);

        var rect = new Rect(pad, pad, w, h);

        GUI.depth = -2000;

        GUI.Box(rect, GUIContent.none, _hudBoxStyle);

        rect.xMin += pad;

        rect.yMin += pad;

        rect.width -= pad * 2f;

        rect.height -= pad * 2f;

        GUI.Label(rect, string.Join("\n", _hudLines), _hudLabelStyle);

    }



    private void Update()

    {

        if (_leftStates.Count == 0 && _rightStates.Count == 0)

        {

            ResolveGestureRoots();

            CollectActiveStatesForBothSides();

        }



        if (RisingEdgeAny(_leftStates, "LEFT"))

            TryApplySwipeColor(isLeftSwipe: true, colorOnLeftSwipe);



        if (RisingEdgeAny(_rightStates, "RIGHT"))

            TryApplySwipeColor(isLeftSwipe: false, colorOnRightSwipe);

    }



    private void LogStartupSummaryOnce()

    {

        if (!logSwipeDiagnostics || _loggedStartupDiag)

            return;

        _loggedStartupDiag = true;



        var sb = new StringBuilder(512);

        sb.Append("Startup: ");

        sb.Append($"leftRoot={DescribeTransform(_leftGestureRoot)} rightRoot={DescribeTransform(_rightGestureRoot)}; ");

        sb.Append($"IActiveState count L={_leftStates.Count} R={_rightStates.Count}; ");

        sb.Append($"requireProximity={requireHandNearVirus} maxDist={maxSwipeDistanceFromVirusMeters:F2}m; ");

        sb.Append($"proximityAnchors L={DescribeTransform(leftProximityAnchorOverride)} R={DescribeTransform(rightProximityAnchorOverride)}.");

        LogDiag(sb.ToString());

    }



    private static string DescribeTransform(Transform t)

    {

        if (t == null)

            return "null";

        return $"\"{t.name}\"";

    }



    private void LogDiag(string message)

    {

        if (!logSwipeDiagnostics)

            return;

        string line = "[VirusSwipe] " + message;

        Debug.Log(line, this);

        print(line);

        AppendHudLine(line);

        MirrorDiagToFile(line);

        MirrorDiagToTmp(message);

    }



    private void AppendHudLine(string line)

    {

        while (_hudLines.Count >= HudMaxLines)

            _hudLines.RemoveAt(0);

        _hudLines.Add(line);

    }



    /// <summary>

    /// Last diagnostic lines for <see cref="VirusDebugSceneManager"/> (or other HUD). Empty when <see cref="logSwipeDiagnostics"/> is off.

    /// </summary>

    public string GetDiagnosticsHudText()

    {

        if (!logSwipeDiagnostics || _hudLines.Count == 0)

            return string.Empty;

        return string.Join("\n", _hudLines);

    }



    /// <summary>

    /// Shown under <c>--- Swipe ---</c> on <see cref="VirusDebugSceneManager"/>. Includes a live line every frame plus buffered events.

    /// </summary>

    public string FormatSwipeDiagnosticsForDebugPanel()

    {

        if (!isActiveAndEnabled)

            return "VirusSwipeColorFromGestures is disabled.";



        if (!logSwipeDiagnostics)

            return "Turn ON Log Swipe Diagnostics on the Gestures object.";



        var sb = new StringBuilder(512);

        sb.Append("LIVE ");

        sb.Append($"t={Time.time:F0}s ");

        sb.Append($"buf={_hudLines.Count} ");

        sb.Append($"IActive L={_leftStates.Count} R={_rightStates.Count}");

        sb.Append(enabled ? "" : " (component disabled)");

        sb.Append(gameObject.activeInHierarchy ? "" : " (Gestures inactive)");



        if (_hudLines.Count > 0)

        {

            sb.Append('\n');

            for (int i = 0; i < _hudLines.Count; i++)

            {

                if (i > 0)

                    sb.Append('\n');

                sb.Append(_hudLines[i]);

            }

        }



        return sb.ToString();

    }



    private void MirrorDiagToFile(string line)

    {

        if (!mirrorSwipeDiagnosticsToFile)

            return;

        try

        {

            File.AppendAllText(DiagFileFullPath, $"{DateTime.UtcNow:O} {line}{Environment.NewLine}");

        }

        catch (Exception ex)

        {

            if (logWarnings)

                Debug.LogWarning("[VirusSwipe] Could not write diag file: " + ex.Message, this);

        }

    }



    private void MirrorDiagToTmp(string messageWithoutPrefix)

    {

        if (swipeDebugScreenText == null)

            return;

        const int maxChars = 3500;

        string next = swipeDebugScreenText.text;

        if (!string.IsNullOrEmpty(next))

            next += "\n";

        next += messageWithoutPrefix;

        if (next.Length > maxChars)

            next = next.Substring(next.Length - maxChars);

        swipeDebugScreenText.text = next;

    }



    private void TryApplySwipeColor(bool isLeftSwipe, Color color)

    {

        string side = isLeftSwipe ? "LEFT" : "RIGHT";

        NetworkGrabbableVirus[] viruses = FindObjectsByType<NetworkGrabbableVirus>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (viruses == null || viruses.Length == 0)

        {

            LogDiag($"{side} swipe → no NetworkGrabbableVirus in scene (spawn / prefab?).");

            return;

        }



        if (!requireHandNearVirus)

        {

            LogDiag($"{side} swipe → requireHandNearVirus=false, applying color to {viruses.Length} virus(es).");

            foreach (NetworkGrabbableVirus v in viruses)

                v.ApplyPersistedVirusSurfaceColor(color);

            return;

        }



        if (!TryGetProximitySampleWorldPosition(isLeftSwipe, out Vector3 handWorld))

        {

            if (logWarnings && !_loggedResolve)

            {

                _loggedResolve = true;

                Debug.LogWarning(

                    "VirusSwipeColorFromGestures: proximity is on but no hand position could be resolved. " +

                    "Assign Left/Right Proximity Anchor Override (e.g. OVR LeftHandAnchor / RightHandAnchor), " +

                    "add Meta Interaction SDK Hand objects to the scene, or disable Require Hand Near Virus.",

                    this);

            }

            LogDiag($"{side} swipe → could not resolve proximity sample position (anchors + IHand root pose failed).");

            return;

        }



        int applied = 0;

        for (int i = 0; i < viruses.Length; i++)

        {

            NetworkGrabbableVirus v = viruses[i];

            if (v == null)

                continue;

            Vector3 virusPoint = GetVirusSamplePoint(v);

            float d = Vector3.Distance(handWorld, virusPoint);

            if (d <= maxSwipeDistanceFromVirusMeters)

            {

                v.ApplyPersistedVirusSurfaceColor(color);

                applied++;

                LogDiag($"{side} swipe → APPLIED color to \"{v.name}\" (dist={d:F3}m ≤ {maxSwipeDistanceFromVirusMeters:F2}m).");

            }

            else

            {

                LogDiag($"{side} swipe → skipped \"{v.name}\" (dist={d:F3}m > {maxSwipeDistanceFromVirusMeters:F2}m).");

            }

        }



        if (applied == 0)

            LogDiag($"{side} swipe → rising edge OK but no virus within range (checked {viruses.Length}). Hand sample={handWorld}");

    }



    private bool TryGetProximitySampleWorldPosition(bool isLeftSwipe, out Vector3 worldPosition)

    {

        Transform anchor = isLeftSwipe ? leftProximityAnchorOverride : rightProximityAnchorOverride;

        if (anchor != null)

        {

            worldPosition = anchor.position;

            return true;

        }



        if (TryGetIHandWorldPosition(isLeftSwipe, out worldPosition))

            return true;



        return false;

    }



    private bool TryGetIHandWorldPosition(bool isLeftSwipe, out Vector3 worldPosition)

    {

        worldPosition = default;

        IHand hand = ResolveIHandForSide(isLeftSwipe);

        if (hand == null)

            return false;

        if (!hand.IsConnected || !hand.GetRootPose(out Pose pose))

            return false;

        worldPosition = pose.position;

        return true;

    }



    private IHand ResolveIHandForSide(bool isLeftSwipe)

    {

        MonoBehaviour ovr = isLeftSwipe ? leftInteractionHandOverride : rightInteractionHandOverride;

        if (ovr != null && ovr is IHand h)

            return h;

        Handedness want = isLeftSwipe ? Handedness.Left : Handedness.Right;

        return FindFirstIHandByHandedness(want);

    }



    private static IHand FindFirstIHandByHandedness(Handedness handedness)

    {

        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)

        {

            MonoBehaviour mb = all[i];

            if (mb != null && mb is IHand h && h.Handedness == handedness)

                return h;

        }

        return null;

    }



    private static Vector3 GetVirusSamplePoint(NetworkGrabbableVirus v)

    {

        Collider col = v.GetComponentInChildren<Collider>();

        if (col != null)

            return col.bounds.center;

        return v.transform.position;

    }



    private bool RisingEdgeAny(List<(MonoBehaviour mb, IActiveState state)> list, string sideLabel)

    {

        bool fired = false;

        for (int i = 0; i < list.Count; i++)

        {

            (MonoBehaviour mb, IActiveState state) = list[i];

            bool now = state.Active;

            _wasActive.TryGetValue(mb, out bool before);

            if (now && !before)

            {

                fired = true;

                LogDiag($"RISING EDGE {sideLabel}: {mb.GetType().Name} on \"{mb.gameObject.name}\" → Active true (was false).");

            }

            _wasActive[mb] = now;

        }

        return fired;

    }



    private void ResolveGestureRoots()

    {

        Transform parent = transform;

        _leftGestureRoot = FindDirectChildByName(parent, leftGestureObjectName);

        _rightGestureRoot = FindDirectChildByName(parent, rightGestureObjectName);

    }



    private void CollectActiveStatesForBothSides()

    {

        _leftStates.Clear();

        _rightStates.Clear();

        _wasActive.Clear();



        if (_leftGestureRoot != null)

            CollectActiveStates(_leftGestureRoot, _leftStates);

        if (_rightGestureRoot != null)

            CollectActiveStates(_rightGestureRoot, _rightStates);



        if ((_leftStates.Count == 0 || _rightStates.Count == 0) && logWarnings && !_loggedResolve)

        {

            _loggedResolve = true;

            Debug.LogWarning(

                $"VirusSwipeColorFromGestures on \"{name}\": under \"{leftGestureObjectName}\" found {_leftStates.Count} IActiveState component(s), " +

                $"under \"{rightGestureObjectName}\" found {_rightStates.Count}. Expected at least one each. Check child names and prefab setup.",

                this);

        }

    }



    private void TryBindSwipePrefabHandReferences()

    {

        MonoBehaviour leftHand = leftInteractionHandOverride;

        MonoBehaviour rightHand = rightInteractionHandOverride;

        if (leftHand == null)

            leftHand = FindFirstIHandMono(Handedness.Left);

        if (rightHand == null)

            rightHand = FindFirstIHandMono(Handedness.Right);



        if (leftHand == null || rightHand == null)

        {

            if (logWarnings && !_loggedHandBind)

            {

                _loggedHandBind = true;

                Debug.LogWarning(

                    "VirusSwipeColorFromGestures: no Interaction SDK IHand found for left and/or right. " +

                    "SwipeForwardGesture needs its root _hand field assigned (same as Gesture Examples). " +

                    "Add Meta Interaction SDK <Hand> objects to your rig or assign Left/Right Interaction Hand Override.",

                    this);

            }

        }



        bool leftAssigned = TryAssignSwipeHandSerializedField(_leftGestureRoot, leftHand);

        bool rightAssigned = TryAssignSwipeHandSerializedField(_rightGestureRoot, rightHand);

        LogDiag(

            $"Hand bind: left IHand={(leftHand != null ? leftHand.name + " (" + leftHand.GetType().Name + ")" : "null")} assignOk={leftAssigned}; " +

            $"right IHand={(rightHand != null ? rightHand.name + " (" + rightHand.GetType().Name + ")" : "null")} assignOk={rightAssigned}.");

    }



    private static MonoBehaviour FindFirstIHandMono(Handedness handedness)

    {

        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)

        {

            MonoBehaviour mb = all[i];

            if (mb != null && mb is IHand h && h.Handedness == handedness)

                return mb;

        }

        return null;

    }



    /// <summary>

    /// SwipeForwardGesture root has a serialized <c>_hand</c> field (null in the package prefab). Gesture Examples assigns it in the scene; we mirror that at runtime when possible.

    /// </summary>

    private static bool TryAssignSwipeHandSerializedField(Transform gestureRoot, MonoBehaviour handSource)

    {

        if (gestureRoot == null || handSource == null || handSource is not IHand)

            return false;



        MonoBehaviour[] onRoot = gestureRoot.GetComponents<MonoBehaviour>();

        for (int i = 0; i < onRoot.Length; i++)

        {

            MonoBehaviour mb = onRoot[i];

            if (mb == null)

                continue;

            FieldInfo fi = mb.GetType().GetField("_hand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fi == null)

                continue;

            object cur = fi.GetValue(mb);

            if (cur is UnityEngine.Object uo && uo != null)

                continue;

            fi.SetValue(mb, handSource);

            return true;

        }



        return false;

    }



    private static void CollectActiveStates(Transform gestureRoot, List<(MonoBehaviour, IActiveState)> into)

    {

        foreach (MonoBehaviour mb in gestureRoot.GetComponentsInChildren<MonoBehaviour>(true))

        {

            if (mb != null && mb is IActiveState state)

                into.Add((mb, state));

        }

    }



    private static Transform FindDirectChildByName(Transform parent, string childName)

    {

        for (int i = 0; i < parent.childCount; i++)

        {

            Transform c = parent.GetChild(i);

            if (c.name == childName)

                return c;

        }

        return null;

    }

}
