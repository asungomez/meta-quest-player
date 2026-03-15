using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple visible marker for head-gaze testing.
/// Projects a ray from the active camera forward direction.
/// </summary>
public class HeadGazePointer : MonoBehaviour
{
    [Tooltip("Write runtime diagnostics to logs.")]
    public bool debugLogs = true;

    [Tooltip("When enabled, keep a reticle at the center of view (Quest-style).")]
    public bool screenLockedReticle = true;

    [Tooltip("Distance from the eye camera for the screen-locked reticle.")]
    public float reticleDistance = 1.2f;

    [Tooltip("World size for the screen-locked reticle.")]
    public float reticleScale = 0.01f;

    [Tooltip("Optional surface to project the pointer onto (for example the welcome panel).")]
    public Transform targetSurface;

    [Tooltip("Distance used when no surface intersection is found.")]
    public float fallbackDistance = 2f;

    [Tooltip("Small offset so the marker renders above the target surface.")]
    public float surfaceOffset = 0.01f;

    [Tooltip("Pointer color.")]
    public Color pointerColor = new Color(1f, 0.3f, 0.15f, 1f);

    [Tooltip("Pointer size factor relative to distance.")]
    public float sizeByDistance = 0.018f;

    private Camera gazeCamera;
    private Transform centerAnchor;
    private Transform pointerVisual;
    private Image pointerImage;
    private bool hasLoggedCamera;
    private bool hasDumpedReticleCandidates;

    private void Awake()
    {
        EnsurePointerVisual();
    }

    private void Update()
    {
        if (pointerVisual == null)
            EnsurePointerVisual();
        if (pointerVisual == null)
            return;

        if (gazeCamera == null)
            gazeCamera = ResolveGazeCamera();
        if (centerAnchor == null)
            centerAnchor = ResolveCenterAnchor(gazeCamera);

        if (gazeCamera == null)
        {
            if (debugLogs)
                Debug.LogWarning("HeadGazePointer: No suitable camera found yet.");
            return;
        }

        if (debugLogs && !hasLoggedCamera)
        {
            hasLoggedCamera = true;
            Debug.Log(
                $"HeadGazePointer: Bound to camera '{gazeCamera.name}' (enabled={gazeCamera.enabled}, active={gazeCamera.gameObject.activeInHierarchy}, stereo={gazeCamera.stereoTargetEye}).");
            DumpReticleCandidates();
        }

        Vector3 origin = gazeCamera.transform.position;
        Vector3 direction = gazeCamera.transform.forward;

        if (screenLockedReticle)
        {
            Transform anchor = centerAnchor != null ? centerAnchor : gazeCamera.transform;
            Vector3 target = anchor.position + anchor.forward * Mathf.Max(0.2f, reticleDistance);
            pointerVisual.position = target;
            pointerVisual.rotation = Quaternion.LookRotation(anchor.forward, anchor.up);
            pointerVisual.localScale = Vector3.one * Mathf.Max(0.0005f, reticleScale);
            return;
        }

        Vector3 targetPosition = origin + direction * Mathf.Max(0.2f, fallbackDistance);

        if (targetSurface != null)
        {
            Plane surfacePlane = new Plane(targetSurface.forward, targetSurface.position);
            Ray headRay = new Ray(origin, direction);
            if (surfacePlane.Raycast(headRay, out float enter) && enter > 0f)
            {
                Vector3 hitPoint = headRay.GetPoint(enter);
                Vector3 towardCamera = (origin - hitPoint).normalized;
                // Push marker toward the viewer so an opaque panel doesn't hide it.
                targetPosition = hitPoint + towardCamera * surfaceOffset;
            }
        }

        pointerVisual.position = targetPosition;
        pointerVisual.forward = (origin - targetPosition).normalized;

        float distance = Vector3.Distance(origin, targetPosition);
        float uniformScale = Mathf.Max(0.0005f, distance * sizeByDistance);
        pointerVisual.localScale = Vector3.one * uniformScale;
    }

    private static Camera ResolveGazeCamera()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            var centerEyeCam = rig.centerEyeAnchor.GetComponent<Camera>();
            if (centerEyeCam != null)
                return centerEyeCam;
        }

        // Prefer active XR cameras first.
        var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy)
                continue;
            string n = cam.name.ToLowerInvariant();
            if (n.Contains("center") || n.Contains("centereye"))
                return cam;
        }

        foreach (var cam in cameras)
        {
            if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy)
                continue;
            if (cam.stereoTargetEye != StereoTargetEyeMask.None)
                return cam;
        }

        if (Camera.main != null && Camera.main.enabled && Camera.main.gameObject.activeInHierarchy)
            return Camera.main;

        foreach (var cam in cameras)
        {
            if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                return cam;
        }

        return null;
    }

    private static Transform ResolveCenterAnchor(Camera cam)
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
            return rig.centerEyeAnchor;

        if (cam != null && cam.transform.parent != null)
            return cam.transform.parent;

        return cam != null ? cam.transform : null;
    }

    private void EnsurePointerVisual()
    {
        if (pointerVisual != null)
            return;

        try
        {
            // Use world-space UI instead of mesh shaders to avoid pink/error shader on device.
            var pointerRoot = new GameObject("HeadGazePointerVisual", typeof(RectTransform), typeof(Canvas));
            pointerRoot.layer = LayerMask.NameToLayer("Ignore Raycast");
            pointerRoot.transform.SetParent(transform, false);

            var canvas = pointerRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            var rootRect = pointerRoot.GetComponent<RectTransform>();
            // Keep reticle unit-size; world size is controlled by reticleScale.
            rootRect.sizeDelta = Vector2.one;

            var imageObj = new GameObject("Reticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObj.transform.SetParent(pointerRoot.transform, false);
            var imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            pointerImage = imageObj.GetComponent<Image>();
            pointerImage.raycastTarget = false;
            pointerImage.sprite = CreateReticleSprite(128, 128, 0.33f, 0.48f);
            pointerImage.color = pointerColor;

            pointerVisual = pointerRoot.transform;

            if (debugLogs)
                Debug.Log("HeadGazePointer: Pointer visual created (world-space UI).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"HeadGazePointer: Failed creating pointer visual. {ex}");
        }
    }

    private void DumpReticleCandidates()
    {
        if (hasDumpedReticleCandidates || !debugLogs)
            return;
        hasDumpedReticleCandidates = true;

        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy)
                continue;

            string n = r.gameObject.name.ToLowerInvariant();
            if (!(n.Contains("reticle") || n.Contains("gaze") || n.Contains("cursor") || n.Contains("pointer")))
                continue;

            string shaderName = r.sharedMaterial != null && r.sharedMaterial.shader != null
                ? r.sharedMaterial.shader.name
                : "<null>";

            Debug.Log($"HeadGazePointer: Candidate renderer '{r.gameObject.name}' path='{GetPath(r.transform)}' shader='{shaderName}'.");
        }

        var graphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        foreach (var g in graphics)
        {
            if (g == null || !g.gameObject.activeInHierarchy)
                continue;

            string n = g.gameObject.name.ToLowerInvariant();
            if (!(n.Contains("reticle") || n.Contains("gaze") || n.Contains("cursor") || n.Contains("pointer")))
                continue;

            string shaderName = g.materialForRendering != null && g.materialForRendering.shader != null
                ? g.materialForRendering.shader.name
                : "<null>";

            Debug.Log($"HeadGazePointer: Candidate graphic '{g.gameObject.name}' path='{GetPath(g.transform)}' shader='{shaderName}'.");
        }
    }

    private static string GetPath(Transform t)
    {
        if (t == null)
            return "<null>";

        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static Sprite CreateReticleSprite(int width, int height, float innerRadius01, float outerRadius01)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float rMin = Mathf.Clamp01(innerRadius01) * Mathf.Min(width, height) * 0.5f;
        float rMax = Mathf.Clamp01(outerRadius01) * Mathf.Min(width, height) * 0.5f;
        float feather = 1.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float aOuter = 1f - Mathf.Clamp01((d - rMax) / feather);
                float aInner = Mathf.Clamp01((d - rMin) / feather);
                float a = aOuter * aInner;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
