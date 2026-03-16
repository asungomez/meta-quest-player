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
    public Image targetImage;
    public Image progressImage;
    public Graphic iconToReplaceWhileProgress;
    public TMP_Text statusText;

    [Header("Visuals")]
    public Color normalColor = new Color(0.16f, 0.43f, 0.96f, 1f);
    public Color hoverColor = new Color(0.08f, 0.23f, 0.58f, 1f);
    public bool useHoverTint = false;

    [Header("Copy")]
    public string instructionText = "Click the start button";
    public string clickedTextFormat = "Start button clicked {0} times";

    [Header("Head-gaze dwell")]
    public float dwellSeconds = 1.0f;

    private float dwellTimer;
    private bool activatedThisHover;
    private int clickCount;
    private ControlModeManager.ControlMode? lastMode;

    private void Awake()
    {
        if (useHoverTint)
            ApplyColor(normalColor);
        SetProgress(0f);
        if (statusText != null)
            statusText.text = instructionText;
    }

    private void Update()
    {
        if (modeManager == null || targetRect == null)
            return;

        if (modeManager.IsControllerSwitchDialogOpen)
        {
            if (useHoverTint)
                ApplyColor(normalColor);
            ResetHoverState();
            return;
        }

        if (lastMode == null || lastMode.Value != modeManager.CurrentMode)
        {
            ResetHoverState();
            lastMode = modeManager.CurrentMode;
        }

        bool hasRay = modeManager.TryGetInteractionRay(out Ray ray);
        bool hovering = hasRay && IsRayPointingAtRect(ray, targetRect);
        if (useHoverTint)
            ApplyColor(hovering ? hoverColor : normalColor);

        if (modeManager.CurrentMode == ControlModeManager.ControlMode.HeadGaze)
            HandleHeadGaze(hovering);
        else
            HandleController(hovering);
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

    private void HandleController(bool hovering)
    {
        // Hide/reset radial fill in controller mode.
        SetProgress(0f);
        activatedThisHover = false;
        dwellTimer = 0f;

        if (hovering && modeManager.ControllerSelectDownThisFrame)
        {
            clickCount++;
            UpdateClickedText();
        }
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

    private void ApplyColor(Color color)
    {
        if (targetImage != null && targetImage.color != color)
            targetImage.color = color;
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
