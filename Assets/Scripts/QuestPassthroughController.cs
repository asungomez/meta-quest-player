using UnityEngine;
using System;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Enables Meta Quest passthrough at runtime.
/// Safe no-op if OVR components are not present.
/// </summary>
public class QuestPassthroughController : MonoBehaviour
{
    [Tooltip("Use Underlay so world-locked UI renders over passthrough.")]
    public bool useUnderlay = true;

    [Tooltip("Passthrough opacity (0-1).")]
    [Range(0f, 1f)]
    public float passthroughOpacity = 1f;

    [Tooltip("Wait a short time so XR rig/cameras are fully initialized.")]
    public float initDelaySeconds = 0.5f;

    private void Start()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        StartCoroutine(EnablePassthroughRoutine());
#endif
    }

    private IEnumerator EnablePassthroughRoutine()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && Time.realtimeSinceStartup < timeoutAt)
                yield return null;
        }
#endif

        if (initDelaySeconds > 0f)
            yield return new WaitForSeconds(initDelaySeconds);

        EnablePassthrough();
    }

    private void EnablePassthrough()
    {
        var manager = FindFirstObjectByType<OVRManager>();
        if (manager == null)
        {
            Debug.LogWarning("QuestPassthroughController: OVRManager not found. Passthrough not enabled.");
            return;
        }

        manager.isInsightPassthroughEnabled = true;
        Debug.Log("QuestPassthroughController: isInsightPassthroughEnabled set to true.");

        var layer = FindFirstObjectByType<OVRPassthroughLayer>();
        if (layer == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            GameObject target = rig != null ? rig.gameObject : manager.gameObject;
            layer = target.AddComponent<OVRPassthroughLayer>();
            Debug.Log($"QuestPassthroughController: Added OVRPassthroughLayer to {target.name}.");
        }

        layer.enabled = true;
        layer.hidden = false;
        layer.textureOpacity = passthroughOpacity;

        // SDK versions differ on how overlay/underlay is exposed.
        // Use reflection so this compiles across more Oculus/Meta package versions.
        TrySetOverlayMode(layer, useUnderlay ? "Underlay" : "Overlay");

        // Underlay passthrough needs transparent camera background to be visible.
        ConfigureCameraForUnderlay();
        Debug.Log("QuestPassthroughController: Passthrough configuration complete.");
    }

    private static void TrySetOverlayMode(OVRPassthroughLayer layer, string enumName)
    {
        var property = layer.GetType().GetProperty("overlayType");
        if (property == null || !property.PropertyType.IsEnum)
            return;

        try
        {
            object enumValue = Enum.Parse(property.PropertyType, enumName, ignoreCase: true);
            property.SetValue(layer, enumValue);
        }
        catch
        {
            // Ignore enum mismatches across SDK versions.
        }
    }

    private static void ConfigureCameraForUnderlay()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            SetTransparentBackground(rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null);
            SetTransparentBackground(rig.leftEyeAnchor != null ? rig.leftEyeAnchor.GetComponent<Camera>() : null);
            SetTransparentBackground(rig.rightEyeAnchor != null ? rig.rightEyeAnchor.GetComponent<Camera>() : null);
        }
        else
        {
            SetTransparentBackground(Camera.main);
        }
    }

    private static void SetTransparentBackground(Camera cam)
    {
        if (cam == null)
            return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        var c = cam.backgroundColor;
        cam.backgroundColor = new Color(c.r, c.g, c.b, 0f);
    }
}
