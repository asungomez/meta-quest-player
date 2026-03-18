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
    public GameObject controllerOnlyTooltipRoot;
    public TMP_Text controllerOnlyTooltipText;
    public Vector2 controllerOnlyTooltipOffset = new Vector2(0f, -8f);

    [Header("Copy")]
    public string instructionText = "Click the start button";
    public string clickedTextFormat = "Start button clicked {0} times";

    [Header("Head-gaze dwell")]
    public float dwellSeconds = 1.0f;
    public bool controllerOnly = false;
    public string controllerOnlyTooltip = "This button is only available with controllers.";

    private float dwellTimer;
    private bool activatedThisHover;
    private int clickCount;
    private Button nativeButton;
    private RectTransform controllerOnlyTooltipRect;

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
        RefreshStatusText();
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
            SetControllerOnlyTooltipVisible(false);
            return;
        }

        bool gazeHasRay = modeManager.TryGetHeadGazeRay(out Ray gazeRay);
        bool gazeHovering = gazeHasRay && IsRayPointingAtRect(gazeRay, targetRect);

        if (controllerOnly)
        {
            // In controller-only mode, gaze only shows tooltip and never triggers dwell/click.
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
        clickCount++;
        UpdateClickedText();
    }

    private void UpdateClickedText()
    {
        RefreshStatusText();
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

    private void RefreshStatusText()
    {
        if (statusText == null)
            return;

        statusText.text = clickCount > 0
            ? string.Format(clickedTextFormat, clickCount)
            : instructionText;
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
