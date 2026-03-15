using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Changes a world-space UI button color when the user's head-gaze points at it.
/// </summary>
public class HeadGazeButtonHover : MonoBehaviour
{
    [Tooltip("Button rect to test against head-gaze ray.")]
    public RectTransform targetRect;

    [Tooltip("Image that will change color on hover.")]
    public Image targetImage;

    [Tooltip("Default button color.")]
    public Color normalColor = new Color(0.16f, 0.43f, 0.96f, 1f);

    [Tooltip("Button color when head gaze points at it.")]
    public Color hoverColor = new Color(0.08f, 0.23f, 0.58f, 1f);

    private Camera gazeCamera;

    private void Awake()
    {
        ApplyColor(normalColor);
    }

    private void Update()
    {
        if (targetRect == null || targetImage == null)
            return;

        if (gazeCamera == null)
            gazeCamera = ResolveGazeCamera();
        if (gazeCamera == null)
            return;

        bool hovering = IsGazePointingAtRect(gazeCamera, targetRect);
        ApplyColor(hovering ? hoverColor : normalColor);
    }

    private void ApplyColor(Color color)
    {
        if (targetImage != null && targetImage.color != color)
            targetImage.color = color;
    }

    private static bool IsGazePointingAtRect(Camera cam, RectTransform rect)
    {
        Vector3 rayOrigin = cam.transform.position;
        Vector3 rayDirection = cam.transform.forward;
        var ray = new Ray(rayOrigin, rayDirection);

        var plane = new Plane(rect.forward, rect.position);
        if (!plane.Raycast(ray, out float enter) || enter <= 0f)
            return false;

        Vector3 worldHit = ray.GetPoint(enter);
        Vector3 localHit = rect.InverseTransformPoint(worldHit);
        Rect r = rect.rect;

        return localHit.x >= r.xMin && localHit.x <= r.xMax &&
               localHit.y >= r.yMin && localHit.y <= r.yMax;
    }

    private static Camera ResolveGazeCamera()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            var centerCam = rig.centerEyeAnchor.GetComponent<Camera>();
            if (centerCam != null)
                return centerCam;
        }

        return Camera.main;
    }
}
