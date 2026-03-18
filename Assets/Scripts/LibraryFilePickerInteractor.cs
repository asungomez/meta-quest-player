using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// File picker button with the same interaction standard as other buttons:
/// - gaze dwell with radial progress
/// - controller click via native Button.onClick
/// - optional controller-only tooltip behavior
/// </summary>
public class LibraryFilePickerInteractor : MonoBehaviour
{
    private const string LogTag = "FilePickerDebug";

    [System.Serializable]
    public class FilePickedEvent : UnityEvent<string> { }

    public ControlModeManager modeManager;

    [Header("Button UI")]
    public RectTransform targetRect;
    public Image progressImage;
    public Graphic iconToReplaceWhileProgress;
    public TMP_Text statusText;
    public GameObject controllerOnlyTooltipRoot;
    public TMP_Text controllerOnlyTooltipText;
    public Vector2 controllerOnlyTooltipOffset = new Vector2(0f, -8f);

    [Header("Head-gaze dwell")]
    public float dwellSeconds = 1.0f;
    public bool controllerOnly = false;
    public string controllerOnlyTooltip = "This button is only available with controllers.";
    public bool onlyVisibleWhenUnlocked = true;

    [Header("Copy")]
    public string pickingStatus = "Opening file picker...";
    public string noSelectionStatus = "No videos in the library";
    public string pickedStatusFormat = "Selected: {0}";
    public string pickerUnavailableStatus = "Install NativeFilePicker plugin to select videos.";

    [Header("Events")]
    public FilePickedEvent onFilePicked;

    private float dwellTimer;
    private bool activatedThisHover;
    private bool isPicking;
    private Button nativeButton;
    private RectTransform controllerOnlyTooltipRect;
    private bool lastVisibleState = true;

    private void Awake()
    {
        if (targetRect != null)
        {
            nativeButton = targetRect.GetComponentInChildren<Button>(true);
            if (nativeButton != null)
                nativeButton.onClick.AddListener(HandleNativeButtonClick);
        }

        SetProgress(0f);
        SetControllerOnlyTooltipVisible(false);
        controllerOnlyTooltipRect = controllerOnlyTooltipRoot != null ? controllerOnlyTooltipRoot.GetComponent<RectTransform>() : null;
        ApplyControllerOnlyTooltipOffset();
        if (controllerOnlyTooltipText != null)
            controllerOnlyTooltipText.text = controllerOnlyTooltip;
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

        bool shouldBeVisible = !onlyVisibleWhenUnlocked || !modeManager.IsLocked;
        if (targetRect.gameObject.activeSelf != shouldBeVisible)
            targetRect.gameObject.SetActive(shouldBeVisible);
        if (!shouldBeVisible)
        {
            if (lastVisibleState)
            {
                ResetHoverState();
                SetControllerOnlyTooltipVisible(false);
                lastVisibleState = false;
            }
            return;
        }
        lastVisibleState = true;

        if (modeManager.IsControllerSwitchDialogOpen)
        {
            ResetHoverState();
            SetControllerOnlyTooltipVisible(false);
            return;
        }

        bool gazeHasRay = modeManager.TryGetHeadGazeRay(out Ray gazeRay);
        bool gazeHovering = gazeHasRay && IsRayPointingAtRect(gazeRay, targetRect);

        if (controllerOnly)
        {
            SetControllerOnlyTooltipVisible(gazeHovering);
            if (gazeHovering)
                ResetHoverState();
            SetProgress(0f);
            return;
        }

        SetControllerOnlyTooltipVisible(false);
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
                TriggerPick();
            }
        }
        else
        {
            ResetHoverState();
        }
    }

    private void HandleNativeButtonClick()
    {
        TriggerPick();
    }

    private void TriggerPick()
    {
        if (isPicking)
        {
            Debug.Log($"{LogTag}: TriggerPick ignored because a pick is already in progress.");
            return;
        }

        isPicking = true;
        Debug.Log($"{LogTag}: TriggerPick started. controllerOnly={controllerOnly}, dwellSeconds={dwellSeconds:0.00}");
        if (statusText != null && !string.IsNullOrWhiteSpace(pickingStatus))
            statusText.text = pickingStatus;

        NativeFilePickerBridge.PickVideoFile(path =>
        {
            isPicking = false;
            Debug.Log($"{LogTag}: Pick callback received. path='{path ?? "<null>"}'");
            if (string.IsNullOrWhiteSpace(path))
            {
                if (statusText != null && !string.IsNullOrWhiteSpace(noSelectionStatus))
                    statusText.text = noSelectionStatus;
                return;
            }

            if (statusText != null)
                statusText.text = string.Format(pickedStatusFormat, Path.GetFileName(path));
            onFilePicked?.Invoke(path);
        },
        () =>
        {
            isPicking = false;
            Debug.LogError($"{LogTag}: Picker unavailable callback fired.");
            if (statusText != null && !string.IsNullOrWhiteSpace(pickerUnavailableStatus))
                statusText.text = pickerUnavailableStatus;
        });
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

    private void SetControllerOnlyTooltipVisible(bool visible)
    {
        if (controllerOnlyTooltipRoot == null)
            return;
        if (controllerOnlyTooltipRoot.activeSelf != visible)
            controllerOnlyTooltipRoot.SetActive(visible);
    }

    private void ApplyControllerOnlyTooltipOffset()
    {
        if (controllerOnlyTooltipRect == null)
            return;
        controllerOnlyTooltipRect.anchoredPosition = controllerOnlyTooltipOffset;
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
