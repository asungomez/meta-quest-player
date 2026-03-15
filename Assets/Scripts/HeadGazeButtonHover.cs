using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Tooltip("Circular progress image that fills while dwelling.")]
    public Image progressImage;

    [Tooltip("Subtitle/status label to update when click completes.")]
    public TMP_Text statusText;

    [Tooltip("Instruction shown before completion.")]
    public string instructionText = "Click the start button";

    [Tooltip("Status text format after click. Use {0} for click count.")]
    public string clickedTextFormat = "Start button clicked {0} times";

    [Tooltip("Seconds required to trigger a dwell click.")]
    public float dwellSeconds = 1.0f;

    private Camera gazeCamera;
    private float dwellTimer;
    private bool activatedThisHover;
    private int clickCount;

    private void Awake()
    {
        ApplyColor(normalColor);
        SetProgress(0f);
        if (statusText != null)
            statusText.text = instructionText;
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

        if (hovering)
        {
            if (activatedThisHover)
            {
                SetProgress(1f);
                return;
            }

            dwellTimer += Time.deltaTime;
            float progress = dwellSeconds > 0f ? Mathf.Clamp01(dwellTimer / dwellSeconds) : 1f;
            SetProgress(progress);

            if (progress >= 1f)
            {
                activatedThisHover = true;
                clickCount++;
                if (statusText != null)
                    statusText.text = string.Format(clickedTextFormat, clickCount);
            }
        }
        else
        {
            dwellTimer = 0f;
            activatedThisHover = false;
            SetProgress(0f);
        }
    }

    private void ApplyColor(Color color)
    {
        if (targetImage != null && targetImage.color != color)
            targetImage.color = color;
    }

    private void SetProgress(float value)
    {
        if (progressImage == null)
            return;
        progressImage.fillAmount = Mathf.Clamp01(value);
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
