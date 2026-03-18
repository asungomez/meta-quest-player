using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Manages active lock mode state and switch UI.
/// </summary>
public class ControlModeManager : MonoBehaviour
{
    public enum ControlMode
    {
        Locked,
        Unlocked
    }

    [Header("Mode Objects")]
    public HeadGazePointer headGazePointer;

    [Header("Switch Button UI")]
    public RectTransform switchButtonRect;
    public Image switchButtonImage;
    public Image switchButtonIconImage;
    public Image switchDwellProgressImage;
    public Sprite lockedIconSprite;
    public Sprite unlockedIconSprite;

    [Header("Switch Behavior")]
    public float headDwellSeconds = 0.8f;
    public Color normalColor = new Color(0.92f, 0.96f, 1f, 0.96f);
    public Color hoverColor = new Color(0.80f, 0.89f, 1f, 0.98f);
    public Color switchProgressColor = new Color(0.80f, 0.90f, 1f, 0.95f);

    [Header("Switch Controller-Only")]
    public bool switchControllerOnly = true;
    public GameObject switchControllerOnlyTooltipRoot;
    public TMP_Text switchControllerOnlyTooltipText;
    public string switchControllerOnlyTooltip = "This button is only available with controllers.";

    [Header("Controller Pointer Visual")]
    public bool showControllerPointer = true;
    public float controllerPointerDistance = 1.8f;
    public float controllerPointerScale = 0.012f;
    public Color controllerPointerColor = new Color(0.95f, 0.98f, 1f, 1f);

    public ControlMode CurrentMode { get; private set; } = ControlMode.Locked;
    public bool IsLocked => CurrentMode == ControlMode.Locked;
    public bool ControllerSelectDownThisFrame { get; private set; }
    public bool RightIndexTriggerDownThisFrame { get; private set; }
    public bool IsControllerSwitchDialogOpen { get; private set; }

    private float switchDwellTimer;
    private bool previousControllerSelect;
    private bool previousRightIndexTrigger;
    private Camera gazeCamera;
    private Transform controllerPointerVisual;
    private Button switchNativeButton;

    private void Awake()
    {
        if (switchButtonRect != null)
        {
            switchNativeButton = switchButtonRect.GetComponentInChildren<Button>(true);
            if (switchNativeButton != null)
                switchNativeButton.onClick.AddListener(HandleSwitchNativeButtonClick);
        }

        EnsureControllerPointerVisual();
        ApplyMode(CurrentMode);

        if (switchControllerOnlyTooltipText != null)
            switchControllerOnlyTooltipText.text = switchControllerOnlyTooltip;
        SetSwitchControllerOnlyTooltipVisible(false);
    }

    private void OnDestroy()
    {
        if (switchNativeButton != null)
            switchNativeButton.onClick.RemoveListener(HandleSwitchNativeButtonClick);
    }

    private void Update()
    {
        ControllerSelectDownThisFrame = false;
        RightIndexTriggerDownThisFrame = false;

        bool controllerPressed = GetControllerSelectPressed();
        ControllerSelectDownThisFrame = controllerPressed && !previousControllerSelect;
        previousControllerSelect = controllerPressed;

        bool rightIndexTriggerPressed = GetRightIndexTriggerPressed();
        RightIndexTriggerDownThisFrame = rightIndexTriggerPressed && !previousRightIndexTrigger;
        previousRightIndexTrigger = rightIndexTriggerPressed;

        bool gazeHasRay = TryGetHeadGazeRay(out Ray gazeRay);
        bool gazeHovering = gazeHasRay && switchButtonRect != null && IsRayPointingAtRect(gazeRay, switchButtonRect);

        bool hasRay = TryGetInteractionRay(out Ray ray);
        bool hovering = hasRay && switchButtonRect != null && IsRayPointingAtRect(ray, switchButtonRect);
        UpdateSwitchDwellProgressVisual(hovering && !switchControllerOnly);

        if (switchControllerOnly)
        {
            SetSwitchControllerOnlyTooltipVisible(gazeHovering);
            switchDwellTimer = 0f;
        }
        else
        {
            SetSwitchControllerOnlyTooltipVisible(false);
        }

        bool hasControllerRayForVisual = TryGetRightControllerRay(out Ray controllerRayForVisual);
        UpdateControllerPointerVisual(hasControllerRayForVisual, controllerRayForVisual);

        if (switchControllerOnly)
            return;

        if (CurrentMode == ControlMode.Locked)
        {
            if (hovering)
            {
                switchDwellTimer += Time.deltaTime;
                if (switchDwellTimer >= headDwellSeconds)
                {
                    ApplyMode(ControlMode.Unlocked);
                    switchDwellTimer = 0f;
                }
            }
            else
            {
                switchDwellTimer = 0f;
            }

            return;
        }

        if (hovering && ControllerSelectDownThisFrame)
            ApplyMode(ControlMode.Locked);
    }

    public bool TryGetInteractionRay(out Ray ray)
    {
        var cam = ResolveGazeCamera();
        if (cam != null)
        {
            ray = new Ray(cam.transform.position, cam.transform.forward);
            return true;
        }
        if (TryGetControllerRay(out ray))
            return true;

        ray = default;
        return false;
    }

    public bool TryGetHeadGazeRay(out Ray ray)
    {
        var cam = ResolveGazeCamera();
        if (cam != null)
        {
            ray = new Ray(cam.transform.position, cam.transform.forward);
            return true;
        }

        ray = default;
        return false;
    }

    public bool TryGetRightControllerRay(out Ray ray)
    {
        return TryGetDeviceRay(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, out ray);
    }

    private void ApplyMode(ControlMode mode)
    {
        CurrentMode = mode;
        switchDwellTimer = 0f;

        if (headGazePointer != null)
            headGazePointer.enabled = true;

        if (switchButtonIconImage != null)
        {
            switchButtonIconImage.enabled = true;
            if (lockedIconSprite != null && unlockedIconSprite != null)
                switchButtonIconImage.sprite = mode == ControlMode.Locked ? lockedIconSprite : unlockedIconSprite;
            switchButtonIconImage.preserveAspect = true;
        }

        if (switchDwellProgressImage != null)
        {
            switchDwellProgressImage.color = switchProgressColor;
            switchDwellProgressImage.fillAmount = 0f;
            switchDwellProgressImage.gameObject.SetActive(mode == ControlMode.Locked && !switchControllerOnly);
        }
    }

    private void UpdateSwitchDwellProgressVisual(bool hovering)
    {
        if (switchDwellProgressImage == null)
            return;

        bool show = CurrentMode == ControlMode.Locked && !switchControllerOnly;
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

    private void HandleSwitchNativeButtonClick()
    {
        ApplyMode(CurrentMode == ControlMode.Locked ? ControlMode.Unlocked : ControlMode.Locked);
    }

    private void SetSwitchControllerOnlyTooltipVisible(bool visible)
    {
        if (switchControllerOnlyTooltipRoot == null)
            return;
        if (switchControllerOnlyTooltipRoot.activeSelf != visible)
            switchControllerOnlyTooltipRoot.SetActive(visible);
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

        bool visible = hasRay;
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

    private static bool GetRightIndexTriggerPressed()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        foreach (var device in devices)
        {
            if (!device.isValid)
                continue;

            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed)
                return true;
        }

        return false;
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
