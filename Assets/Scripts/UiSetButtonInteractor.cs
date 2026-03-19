using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Reusable UI Set button behavior:
/// - gaze dwell activation with radial progress
/// - controller click activation via native Button.onClick
/// - optional controller-only tooltip behavior
/// </summary>
public class UiSetButtonInteractor : MonoBehaviour
{
    private const string LogTag = "DialogDebug";

    public enum DialogAction
    {
        None,
        Save,
        Exit
    }

    public ControlModeManager modeManager;

    [Header("Button UI")]
    public RectTransform targetRect;
    public Image progressImage;
    public Graphic iconToReplaceWhileProgress;
    public GameObject controllerOnlyTooltipRoot;
    public TMP_Text controllerOnlyTooltipText;
    public string controllerOnlyTooltip = "This button is only available with controllers.";

    [Header("Behavior")]
    public float dwellSeconds = 1.0f;
    public bool controllerOnly = false;
    public bool requireUnlockedMode = false;

    [Header("Events")]
    public UnityEvent onActivated = new UnityEvent();
    public FileSelectionDialogController dialogController;
    public DialogAction dialogAction = DialogAction.None;

    private float dwellTimer;
    private bool activatedThisHover;
    private Button nativeButton;
    private Toggle nativeToggle;

    private void Awake()
    {
        if (onActivated == null)
            onActivated = new UnityEvent();

        if (targetRect != null)
        {
            nativeButton = targetRect.GetComponentInChildren<Button>(true);
            if (nativeButton != null)
                nativeButton.onClick.AddListener(HandleNativeButtonClick);

            nativeToggle = targetRect.GetComponentInChildren<Toggle>(true);
            if (nativeButton == null && nativeToggle != null)
                nativeToggle.onValueChanged.AddListener(HandleNativeToggleValueChanged);
        }

        if (controllerOnlyTooltipText != null)
            controllerOnlyTooltipText.text = controllerOnlyTooltip;

        SetProgress(0f);
        SetControllerOnlyTooltipVisible(false);
    }

    private void OnDestroy()
    {
        if (nativeButton != null)
            nativeButton.onClick.RemoveListener(HandleNativeButtonClick);
        if (nativeToggle != null)
            nativeToggle.onValueChanged.RemoveListener(HandleNativeToggleValueChanged);
    }

    private void Update()
    {
        if (modeManager == null || targetRect == null)
            return;

        if (requireUnlockedMode && modeManager.IsLocked)
        {
            ResetHoverState();
            SetControllerOnlyTooltipVisible(false);
            return;
        }

        if (modeManager.IsControllerSwitchDialogOpen)
        {
            ResetHoverState();
            SetControllerOnlyTooltipVisible(false);
            return;
        }

        bool gazeHasRay = modeManager.TryGetHeadGazeRay(out Ray gazeRay);
        bool gazeHovering = gazeHasRay && IsRayPointingAtRect(gazeRay, targetRect);
        bool interactionHasRay = modeManager.TryGetInteractionRay(out Ray interactionRay);
        bool interactionHovering = interactionHasRay && IsRayPointingAtRect(interactionRay, targetRect);

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

        // Fallback for prefabs that don't expose a native Unity Button click callback.
        if (nativeButton == null && nativeToggle == null && interactionHovering && modeManager.ControllerSelectDownThisFrame)
            Activate();
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
                Activate();
            }
        }
        else
        {
            ResetHoverState();
        }
    }

    private void HandleNativeButtonClick()
    {
        Activate();
    }

    private void HandleNativeToggleValueChanged(bool _)
    {
        Activate();
    }

    private void Activate()
    {
        onActivated?.Invoke();

        if (dialogController == null || dialogAction == DialogAction.None)
            return;

        switch (dialogAction)
        {
            case DialogAction.Save:
                Debug.Log($"{LogTag}: Dialog action SAVE activated.");
                dialogController.HandleSave();
                break;
            case DialogAction.Exit:
                Debug.Log($"{LogTag}: Dialog action EXIT activated.");
                dialogController.HandleExit();
                break;
        }
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
