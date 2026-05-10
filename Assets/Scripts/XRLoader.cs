using UnityEngine;
using UnityEngine.XR.Management;
using System.Collections;

public class XRLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;

        if (xrManager.activeLoader == null)
        {
            yield return xrManager.InitializeLoader();
            xrManager.StartSubsystems();
        }
    }
}