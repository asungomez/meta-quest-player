using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Manages active interaction mode (head gaze vs controller) and mode switch UI.
/// </summary>
public class ControlModeManager : MonoBehaviour
{
    public enum ControlMode
    {
        HeadGaze,
        Controller
    }

    [Header("Mode Objects")]
    public HeadGazePointer headGazePointer;

    [Header("Switch Button UI")]
    public RectTransform switchButtonRect;
    public Image switchButtonImage;
    public Image switchButtonIconImage;
    public Image switchDwellProgressImage;
    public Sprite controllerIconSprite;
    public Sprite headGazeIconSprite;

    [Header("Switch Behavior")]
    public float headDwellSeconds = 0.8f;
    public Color normalColor = new Color(0.92f, 0.96f, 1f, 0.96f);
    public Color hoverColor = new Color(0.80f, 0.89f, 1f, 0.98f);
    public Color switchProgressColor = new Color(0.80f, 0.90f, 1f, 0.95f);

    [Header("Controller Switch Dialog")]
    public GameObject switchDialogRoot;
    public RectTransform switchDialogCancelRect;
    public Image switchDialogCancelImage;
    public Image switchDialogCancelProgressImage;
    public RectTransform switchDialogConfirmRect;
    public Image switchDialogConfirmImage;
    public Image switchDialogConfirmProgressImage;
    public Color switchDialogCancelNormal = new Color(0.18f, 0.45f, 0.94f, 1f);
    public Color switchDialogCancelHover = new Color(0.10f, 0.30f, 0.72f, 1f);
    public Color switchDialogConfirmNormal = new Color(0.10f, 0.14f, 0.20f, 1f);
    public Color switchDialogConfirmHover = new Color(0.06f, 0.10f, 0.16f, 1f);
    public Color switchDialogProgressColor = new Color(0.86f, 0.93f, 1f, 0.95f);

    [Header("Controller Pointer Visual")]
    public bool showControllerPointer = true;
    public float controllerPointerDistance = 1.8f;
    public float controllerPointerScale = 0.012f;
    public Color controllerPointerColor = new Color(0.95f, 0.98f, 1f, 1f);

    public ControlMode CurrentMode { get; private set; } = ControlMode.HeadGaze;
    public bool ControllerSelectDownThisFrame { get; private set; }
    public bool IsControllerSwitchDialogOpen { get; private set; }

    private float switchDwellTimer;
    private float dialogDwellTimer;
    private bool previousControllerSelect;
    private Camera gazeCamera;
    private Transform controllerPointerVisual;
    private bool hasWarnedNoControllerRay;
    private SwitchDialogChoice dialogChoice = SwitchDialogChoice.None;

    private enum SwitchDialogChoice
    {
        None,
        Cancel,
        Confirm
    }

    private void Awake()
    {
        EnsureControllerPointerVisual();
        ApplyMode(CurrentMode);
        SetDialogOpen(false);
    }

    private void Update()
    {
        ControllerSelectDownThisFrame = false;

        bool hasRay = TryGetInteractionRay(out Ray ray);
        bool hovering = hasRay && switchButtonRect != null && IsRayPointingAtRect(ray, switchButtonRect);
        if (switchButtonImage != null)
            switchButtonImage.color = hovering ? hoverColor : normalColor;

        UpdateSwitchDwellProgressVisual(hovering);
        UpdateControllerPointerVisual(hasRay, ray);

        if (IsControllerSwitchDialogOpen)
        {
            HandleSwitchDialogInteraction(hasRay, ray);
            previousControllerSelect = false;
            return;
        }

        if (CurrentMode == ControlMode.HeadGaze)
        {
            if (hovering)
            {
                switchDwellTimer += Time.deltaTime;
                if (switchDwellTimer >= headDwellSeconds)
                {
                    SetDialogOpen(true);
                    switchDwellTimer = 0f;
                }
            }
            else
            {
                switchDwellTimer = 0f;
            }

            previousControllerSelect = false;
            return;
        }

        bool controllerPressed = GetControllerSelectPressed();
        ControllerSelectDownThisFrame = controllerPressed && !previousControllerSelect;
        previousControllerSelect = controllerPressed;

        if (!hasRay && !hasWarnedNoControllerRay)
        {
            hasWarnedNoControllerRay = true;
            Debug.LogWarning("ControlModeManager: Controller mode active but no controller pose found. Check headset hand/controller tracking settings.");
        }
        else if (hasRay)
        {
            hasWarnedNoControllerRay = false;
        }

        if (hovering && ControllerSelectDownThisFrame)
            ApplyMode(ControlMode.HeadGaze);
    }

    public bool TryGetInteractionRay(out Ray ray)
    {
        if (CurrentMode == ControlMode.HeadGaze)
        {
            var cam = ResolveGazeCamera();
            if (cam != null)
            {
                ray = new Ray(cam.transform.position, cam.transform.forward);
                return true;
            }
        }
        else
        {
            if (TryGetControllerRay(out ray))
                return true;
        }

        ray = default;
        return false;
    }

    private void ApplyMode(ControlMode mode)
    {
        CurrentMode = mode;
        switchDwellTimer = 0f;
        hasWarnedNoControllerRay = false;

        if (headGazePointer != null)
            headGazePointer.enabled = mode == ControlMode.HeadGaze;

        if (switchButtonIconImage != null)
        {
            switchButtonIconImage.enabled = true;
            switchButtonIconImage.sprite = mode == ControlMode.HeadGaze ? controllerIconSprite : headGazeIconSprite;
            switchButtonIconImage.preserveAspect = true;
        }

        if (switchDwellProgressImage != null)
        {
            switchDwellProgressImage.color = switchProgressColor;
            switchDwellProgressImage.fillAmount = 0f;
            switchDwellProgressImage.gameObject.SetActive(mode == ControlMode.HeadGaze && !IsControllerSwitchDialogOpen);
        }
    }

    private void UpdateSwitchDwellProgressVisual(bool hovering)
    {
        if (switchDwellProgressImage == null)
            return;

        bool show = CurrentMode == ControlMode.HeadGaze && !IsControllerSwitchDialogOpen;
        if (switchDwellProgressImage.gameObject.activeSelf != show)
            switchDwellProgressImage.gameObject.SetActive(show);
        if (!show)
            return;

        if (headDwellSeconds <= 0f)
        {
            switchDwellProgressImage.fillAmount = hovering ? 1f : 0f;
            return;
        }

        float normalized = hovering ? Mathf.Clamp01(switchDwellTimer / headDwellSeconds) : 0f;
        switchDwellProgressImage.fillAmount = normalized;
    }

    private void HandleSwitchDialogInteraction(bool hasRay, Ray ray)
    {
        if (CurrentMode != ControlMode.HeadGaze)
        {
            SetDialogOpen(false);
            return;
        }

        bool hoverCancel = hasRay && switchDialogCancelRect != null && IsRayPointingAtRect(ray, switchDialogCancelRect);
        bool hoverConfirm = hasRay && switchDialogConfirmRect != null && IsRayPointingAtRect(ray, switchDialogConfirmRect);

        if (switchDialogCancelImage != null)
            switchDialogCancelImage.color = hoverCancel ? switchDialogCancelHover : switchDialogCancelNormal;
        if (switchDialogConfirmImage != null)
            switchDialogConfirmImage.color = hoverConfirm ? switchDialogConfirmHover : switchDialogConfirmNormal;
        if (switchDialogCancelProgressImage != null)
            switchDialogCancelProgressImage.fillAmount = 0f;
        if (switchDialogConfirmProgressImage != null)
            switchDialogConfirmProgressImage.fillAmount = 0f;

        SwitchDialogChoice currentChoice = hoverCancel ? SwitchDialogChoice.Cancel :
                                          hoverConfirm ? SwitchDialogChoice.Confirm :
                                          SwitchDialogChoice.None;

        if (currentChoice == SwitchDialogChoice.None)
        {
            dialogChoice = SwitchDialogChoice.None;
            dialogDwellTimer = 0f;
            return;
        }

        if (dialogChoice != currentChoice)
        {
            dialogChoice = currentChoice;
            dialogDwellTimer = 0f;
        }

        dialogDwellTimer += Time.deltaTime;
        float normalized = headDwellSeconds > 0f ? Mathf.Clamp01(dialogDwellTimer / headDwellSeconds) : 1f;
        if (dialogChoice == SwitchDialogChoice.Cancel && switchDialogCancelProgressImage != null)
            switchDialogCancelProgressImage.fillAmount = normalized;
        if (dialogChoice == SwitchDialogChoice.Confirm && switchDialogConfirmProgressImage != null)
            switchDialogConfirmProgressImage.fillAmount = normalized;

        if (normalized < 1f)
            return;

        dialogDwellTimer = 0f;
        if (dialogChoice == SwitchDialogChoice.Cancel)
        {
            SetDialogOpen(false);
            return;
        }

        if (dialogChoice == SwitchDialogChoice.Confirm)
        {
            SetDialogOpen(false);
            ApplyMode(ControlMode.Controller);
        }
    }

    private void SetDialogOpen(bool isOpen)
    {
        IsControllerSwitchDialogOpen = isOpen;
        dialogDwellTimer = 0f;
        dialogChoice = SwitchDialogChoice.None;

        if (switchDialogRoot != null)
            switchDialogRoot.SetActive(isOpen);

        if (switchDialogCancelImage != null)
            switchDialogCancelImage.color = switchDialogCancelNormal;
        if (switchDialogConfirmImage != null)
            switchDialogConfirmImage.color = switchDialogConfirmNormal;
        if (switchDialogCancelProgressImage != null)
        {
            switchDialogCancelProgressImage.color = switchDialogProgressColor;
            switchDialogCancelProgressImage.fillAmount = 0f;
        }
        if (switchDialogConfirmProgressImage != null)
        {
            switchDialogConfirmProgressImage.color = switchDialogProgressColor;
            switchDialogConfirmProgressImage.fillAmount = 0f;
        }

        if (switchDwellProgressImage != null)
        {
            switchDwellProgressImage.fillAmount = 0f;
            switchDwellProgressImage.gameObject.SetActive(CurrentMode == ControlMode.HeadGaze && !isOpen);
        }
    }

    private void EnsureControllerPointerVisual()
    {
        if (!showControllerPointer || controllerPointerVisual != null)
            return;

        var root = new GameObject("ControllerPointerVisual", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = Vector2.one;

        var imageObj = new GameObject("Reticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObj.transform.SetParent(root.transform, false);
        var imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        var img = imageObj.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = CreateReticleSprite(128, 128, 0.35f, 0.48f);
        img.color = controllerPointerColor;

        controllerPointerVisual = root.transform;
        controllerPointerVisual.gameObject.SetActive(false);
    }

    private void UpdateControllerPointerVisual(bool hasRay, Ray ray)
    {
        if (!showControllerPointer)
            return;

        EnsureControllerPointerVisual();
        if (controllerPointerVisual == null)
            return;

        bool visible = CurrentMode == ControlMode.Controller && hasRay;
        controllerPointerVisual.gameObject.SetActive(visible);
        if (!visible)
            return;

        Vector3 pos = ray.GetPoint(Mathf.Max(0.2f, controllerPointerDistance));
        controllerPointerVisual.position = pos;
        controllerPointerVisual.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
        controllerPointerVisual.localScale = Vector3.one * Mathf.Max(0.0005f, controllerPointerScale);
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

    private Camera ResolveGazeCamera()
    {
        if (gazeCamera != null)
            return gazeCamera;

        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            gazeCamera = rig.centerEyeAnchor.GetComponent<Camera>();
            if (gazeCamera != null)
                return gazeCamera;
        }

        gazeCamera = Camera.main;
        return gazeCamera;
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

    private static bool TryGetControllerRay(out Ray ray)
    {
        if (TryGetDeviceRay(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, out ray))
            return true;
        if (TryGetDeviceRay(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, out ray))
            return true;

        ray = default;
        return false;
    }

    private static bool TryGetDeviceRay(InputDeviceCharacteristics handCharacteristics, out Ray ray)
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(handCharacteristics, devices);
        foreach (var device in devices)
        {
            if (!device.isValid)
                continue;
            if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                continue;
            if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                continue;

            ray = new Ray(pos, rot * Vector3.forward);
            return true;
        }

        ray = default;
        return false;
    }

    private static bool GetControllerSelectPressed()
    {
        return GetSelectPressedFor(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller) ||
               GetSelectPressedFor(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller);
    }

    private static bool GetSelectPressedFor(InputDeviceCharacteristics characteristics)
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
        foreach (var device in devices)
        {
            if (!device.isValid)
                continue;

            bool pressed = false;
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out pressed) && pressed)
                return true;
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out pressed) && pressed)
                return true;
        }
        return false;
    }
}
