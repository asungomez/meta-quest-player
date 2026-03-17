using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles Start button hover/click behavior for both head-gaze and controller modes.
/// </summary>
public class StartButtonInteractor : MonoBehaviour
{
    public ControlModeManager modeManager;

    [Header("Button UI")]
    public RectTransform targetRect;
    public Image progressImage;
    public Graphic iconToReplaceWhileProgress;
    public TMP_Text statusText;

    [Header("Copy")]
    public string instructionText = "Click the start button";
    public string clickedTextFormat = "Start button clicked {0} times";

    [Header("Head-gaze dwell")]
    public float dwellSeconds = 1.0f;

    [Header("Debug")]
    public bool debugLogs = true;

    private float dwellTimer;
    private bool activatedThisHover;
    private int clickCount;
    private Button nativeButton;
    private bool lastGazeHover;
    private bool lastControllerHover;
    private bool lastControllerRayAvailable;

    private void Awake()
    {
        if (targetRect != null)
        {
            nativeButton = targetRect.GetComponentInChildren<Button>(true);
            if (nativeButton != null)
                nativeButton.onClick.AddListener(HandleNativeButtonClick);
        }

        if (debugLogs)
        {
            if (nativeButton == null)
            {
                Debug.LogWarning("StartButtonInteractor: No native Unity Button found under targetRect. Native click events will not fire.");
            }
            else
            {
                Debug.Log(
                    $"StartButtonInteractor: Bound native button '{nativeButton.gameObject.name}' " +
                    $"(activeInHierarchy={nativeButton.gameObject.activeInHierarchy}, interactable={nativeButton.interactable}).");
            }
        }

        SetProgress(0f);
        if (statusText != null)
            statusText.text = instructionText;
    }

    private void OnDestroy()
    {
        if (nativeButton != null)
            nativeButton.onClick.RemoveListener(HandleNativeButtonClick);
    }

    private void Update()
    {
        if (modeManager == null || targetRect == null)
            return;

        if (modeManager.IsControllerSwitchDialogOpen)
        {
            ResetHoverState();
            return;
        }

        bool gazeHasRay = modeManager.TryGetHeadGazeRay(out Ray gazeRay);
        bool gazeHovering = gazeHasRay && IsRayPointingAtRect(gazeRay, targetRect);

        bool controllerHasRay = modeManager.TryGetRightControllerRay(out Ray controllerRay);
        bool controllerHovering = controllerHasRay && IsRayPointingAtRect(controllerRay, targetRect);

        if (debugLogs)
        {
            if (controllerHasRay != lastControllerRayAvailable)
            {
                lastControllerRayAvailable = controllerHasRay;
                Debug.Log($"StartButtonInteractor: Right controller ray available = {controllerHasRay}");
            }

            if (gazeHovering != lastGazeHover)
            {
                lastGazeHover = gazeHovering;
                Debug.Log($"StartButtonInteractor: Gaze hover = {gazeHovering}");
            }

            if (controllerHovering != lastControllerHover)
            {
                lastControllerHover = controllerHovering;
                Debug.Log($"StartButtonInteractor: Controller hover = {controllerHovering}");
            }
        }

        HandleHeadGaze(gazeHovering);
    }

    private void HandleHeadGaze(bool hovering)
    {
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
                UpdateClickedText();
            }
        }
        else
        {
            ResetHoverState();
        }
    }

    private void HandleNativeButtonClick()
    {
        if (debugLogs)
            Debug.Log("StartButtonInteractor: Native Button.onClick fired.");

        clickCount++;
        UpdateClickedText();
    }

    private void UpdateClickedText()
    {
        if (statusText != null)
            statusText.text = string.Format(clickedTextFormat, clickCount);
    }

    private void ResetHoverState()
    {
        dwellTimer = 0f;
        activatedThisHover = false;
        SetProgress(0f);
    }

    private void SetProgress(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (progressImage != null)
            progressImage.fillAmount = clamped;

        if (iconToReplaceWhileProgress != null)
            iconToReplaceWhileProgress.enabled = clamped <= 0.001f;
    }

    private static bool IsRayPointingAtRect(Ray ray, RectTransform rect)
    {
        var plane = new Plane(rect.forward, rect.position);
        if (!plane.Raycast(ray, out float enter) || enter <= 0f)
            return false;

        Vector3 worldHit = ray.GetPoint(enter);
        Vector3 localHit = rect.InverseTransformPoint(worldHit);
        Rect r = rect.rect;

        return localHit.x >= r.xMin && localHit.x <= r.xMax &&
               localHit.y >= r.yMin && localHit.y <= r.yMax;
    }
}
