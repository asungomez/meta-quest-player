using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Editor menu to set up a minimal passthrough + welcome screen scene.
/// </summary>
public static class VRVideoPlayerSetup
{
    [MenuItem("VR Video Player/Setup Scene")]
    public static void SetupScene()
    {
        // Remove previously generated objects so setup is repeatable.
        DestroyIfExists("VRVideoPlayer");
        DestroyIfExists("LibraryScreen");
        DestroyIfExists("PlayScreen");
        DestroyIfExists("WelcomeRoot");
        DestroyIfExists("WelcomeScreen");
        DestroyIfExists("WelcomeCanvas");

        // Root object that handles passthrough only.
        GameObject welcomeRoot = new GameObject("WelcomeRoot");
        welcomeRoot.AddComponent<QuestPassthroughController>();
        var headPointer = welcomeRoot.AddComponent<HeadGazePointer>();
        headPointer.screenLockedReticle = true;
        headPointer.reticleDistance = 1.0f;
        headPointer.reticleScale = 0.016f;
        headPointer.pointerColor = new Color(1f, 1f, 1f, 1f);

        // Welcome panel in front of the user.
        GameObject welcomeScreen = new GameObject("WelcomeScreen");
        if (TryGetWelcomeScreenPose(1.5f, out Vector3 welcomePos, out Quaternion welcomeRot))
        {
            welcomeScreen.transform.position = welcomePos;
            welcomeScreen.transform.rotation = welcomeRot;
        }
        else
        {
            welcomeScreen.transform.position = new Vector3(0.8325f, 2.27f, 1.5f);
            welcomeScreen.transform.rotation = Quaternion.identity;
        }

        BuildWelcomeCanvas(
            welcomeScreen.transform,
            out RectTransform buttonRect,
            out Image buttonImage,
            out TextMeshProUGUI subtitleText,
            out Image progressImage,
            out Image startButtonIconImage,
            out RectTransform switchRect,
            out Image switchImage,
            out Image switchIconImage,
            out Image switchDwellProgressImage,
            out GameObject switchDialogRoot,
            out RectTransform switchDialogCancelRect,
            out Image switchDialogCancelImage,
            out Image switchDialogCancelProgressImage,
            out RectTransform switchDialogConfirmRect,
            out Image switchDialogConfirmImage,
            out Image switchDialogConfirmProgressImage);

        var modeManager = welcomeRoot.AddComponent<ControlModeManager>();
        modeManager.headGazePointer = headPointer;
        modeManager.switchButtonRect = switchRect;
        modeManager.switchButtonImage = switchImage;
        modeManager.switchButtonIconImage = switchIconImage;
        modeManager.switchDwellProgressImage = switchDwellProgressImage;
        modeManager.switchDialogRoot = switchDialogRoot;
        modeManager.switchDialogCancelRect = switchDialogCancelRect;
        modeManager.switchDialogCancelImage = switchDialogCancelImage;
        modeManager.switchDialogCancelProgressImage = switchDialogCancelProgressImage;
        modeManager.switchDialogConfirmRect = switchDialogConfirmRect;
        modeManager.switchDialogConfirmImage = switchDialogConfirmImage;
        modeManager.switchDialogConfirmProgressImage = switchDialogConfirmProgressImage;
        modeManager.controllerIconSprite = LoadSpriteFromImagesFile("vr-controller.png");
        modeManager.headGazeIconSprite = LoadSpriteFromImagesFile("head-gaze.png");

        var startInteractor = welcomeRoot.AddComponent<StartButtonInteractor>();
        startInteractor.modeManager = modeManager;
        startInteractor.targetRect = buttonRect;
        startInteractor.progressImage = progressImage;
        startInteractor.iconToReplaceWhileProgress = startButtonIconImage;
        startInteractor.statusText = subtitleText;
        startInteractor.dwellSeconds = 1.0f;

        EnsureNativeUiRayInteractionActive();

        Undo.RegisterCreatedObjectUndo(welcomeRoot, "Create Welcome Root");
        Undo.RegisterCreatedObjectUndo(welcomeScreen, "Create Welcome Screen");
        Selection.activeGameObject = welcomeScreen;
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
            Undo.DestroyObjectImmediate(go);
    }

    private static void EnsureNativeUiRayInteractionActive()
    {
        // Required by ISDK for ray-based interaction with PointableCanvas.
        var rayInteraction = FindSceneObjectByNameIncludingInactive("ISDK_RayInteraction");
        if (rayInteraction == null)
        {
            Debug.LogWarning("VRVideoPlayerSetup: ISDK_RayInteraction object not found. Native controller ray-to-UI may not work.");
        }
        else
        {
            if (!rayInteraction.activeSelf)
                rayInteraction.SetActive(true);
            Debug.Log($"VRVideoPlayerSetup: ISDK_RayInteraction activeInHierarchy={rayInteraction.activeInHierarchy}");
        }

        var pointableCanvasModuleType = FindTypeInLoadedAssemblies("Oculus.Interaction.PointableCanvasModule");
        GameObject pointableCanvasModuleObject = null;
        if (pointableCanvasModuleType != null)
            pointableCanvasModuleObject = FindSceneObjectWithComponentIncludingInactive(pointableCanvasModuleType);
        if (pointableCanvasModuleObject == null)
            pointableCanvasModuleObject = FindSceneObjectByNameIncludingInactive("PointableCanvasModule");

        if (pointableCanvasModuleObject == null)
        {
            pointableCanvasModuleObject = new GameObject("PointableCanvasModule");
            Undo.RegisterCreatedObjectUndo(pointableCanvasModuleObject, "Create PointableCanvasModule");
            pointableCanvasModuleObject.AddComponent<EventSystem>();

            if (pointableCanvasModuleType != null)
            {
                pointableCanvasModuleObject.AddComponent(pointableCanvasModuleType);
                Debug.Log("VRVideoPlayerSetup: Created PointableCanvasModule host with EventSystem.");
            }
            else
            {
                Debug.LogWarning("VRVideoPlayerSetup: Could not locate Oculus.Interaction.PointableCanvasModule type. Created EventSystem only.");
            }
        }
        else
        {
            if (!pointableCanvasModuleObject.activeSelf)
                pointableCanvasModuleObject.SetActive(true);

            if (pointableCanvasModuleObject.GetComponent<EventSystem>() == null)
                pointableCanvasModuleObject.AddComponent<EventSystem>();
            if (pointableCanvasModuleType != null && pointableCanvasModuleObject.GetComponent(pointableCanvasModuleType) == null)
                pointableCanvasModuleObject.AddComponent(pointableCanvasModuleType);

            Debug.Log($"VRVideoPlayerSetup: PointableCanvasModule host='{pointableCanvasModuleObject.name}', activeInHierarchy={pointableCanvasModuleObject.activeInHierarchy}");
        }
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
        {
            if (go != null && go.name == objectName && go.scene.IsValid())
                return go;
        }

        return null;
    }

    private static GameObject FindSceneObjectWithComponentIncludingInactive(System.Type componentType)
    {
        if (componentType == null)
            return null;

        var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in all)
        {
            if (go == null || !go.scene.IsValid())
                continue;
            if (go.GetComponent(componentType) != null)
                return go;
        }

        return null;
    }

    private static System.Type FindTypeInLoadedAssemblies(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
            return null;

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(fullTypeName, false);
            if (type != null)
                return type;
        }

        return null;
    }

    private static bool TryGetWelcomeScreenPose(float distanceMeters, out Vector3 position, out Quaternion rotation)
    {
        // Keep this aligned with current backplate setup:
        // panel size 925x600 with canvas scale 0.0018 => half extents 0.8325m x 0.54m.
        const float halfPanelWidthMeters = 0.8325f;
        const float halfPanelHeightMeters = 0.54f;
        position = new Vector3(0f, 1.55f, distanceMeters);
        rotation = Quaternion.identity;

        // In editor setup, only trust the XR center-eye anchor if present.
        // Generic camera fallbacks (Scene/Game cameras) can place UI off-center.
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye == null)
            return false;

        Vector3 forward = centerEye.transform.forward;
        forward.y = 0f; // Keep UI upright while still centered relative to current gaze direction.
        if (forward.sqrMagnitude < 0.0001f)
            forward = centerEye.transform.forward;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        position = centerEye.transform.position + forward * Mathf.Max(0.5f, distanceMeters);
        position += centerEye.transform.right * halfPanelWidthMeters;
        position += Vector3.up * halfPanelHeightMeters;
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
    }

    private static Sprite LoadSpriteFromImagesFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // 1) Exact filename match first.
        string exactPath = $"Assets/Images/{fileName}";
        Sprite exact = LoadOrImportSpriteAtPath(exactPath);
        if (exact != null)
            return exact;

        // 2) Fallback: match stem prefix (e.g. vr-controller-<uuid>.png).
        string stem = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Images" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!name.StartsWith(stem))
                continue;

            Sprite sprite = LoadOrImportSpriteAtPath(path);
            if (sprite != null)
                return sprite;
        }

        // 3) Fallback: search anywhere under Assets by stem.
        string[] globalGuids = AssetDatabase.FindAssets($"{stem} t:Texture2D", new[] { "Assets" });
        foreach (string guid in globalGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = LoadOrImportSpriteAtPath(path);
            if (sprite != null)
                return sprite;
        }

        Debug.LogWarning($"VRVideoPlayerSetup: Could not load icon '{fileName}'. Searched Assets/Images and all Assets for stem '{stem}'.");
        return null;
    }

    private static Sprite LoadOrImportSpriteAtPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        sprite = GetFirstSpriteSubAsset(path);
        if (sprite != null)
            return sprite;

        // If texture exists but isn't imported as Sprite, convert it automatically.
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            return null;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        return GetFirstSpriteSubAsset(path);
    }

    private static Sprite GetFirstSpriteSubAsset(string path)
    {
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (subAssets == null)
            return null;

        foreach (Object asset in subAssets)
        {
            if (asset is Sprite s)
                return s;
        }

        return null;
    }

    private static GameObject LoadRequiredPrefab(string path, string displayName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new System.InvalidOperationException(
                $"Required UI Set prefab '{displayName}' was not found at '{path}'. " +
                "Please copy that prefab into Assets/Prefabs/UI and run Setup Scene again.");
        }
        return prefab;
    }

    private static Image FindRequiredUiSetIconImage(Transform root)
    {
        if (root == null)
            return null;

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img != null && img.gameObject.tag == "QDSUIIcon")
                return img;
        }

        return null;
    }

    private static Transform BuildWelcomeCanvas(
        Transform parent,
        out RectTransform buttonRect,
        out Image buttonImage,
        out TextMeshProUGUI subtitleText,
        out Image progressImage,
        out Image startButtonIconImage,
        out RectTransform switchRect,
        out Image switchImage,
        out Image switchIconImage,
        out Image switchDwellProgressImage,
        out GameObject switchDialogRoot,
        out RectTransform switchDialogCancelRect,
        out Image switchDialogCancelImage,
        out Image switchDialogCancelProgressImage,
        out RectTransform switchDialogConfirmRect,
        out Image switchDialogConfirmImage,
        out Image switchDialogConfirmProgressImage)
    {
        buttonRect = null;
        buttonImage = null;
        subtitleText = null;
        progressImage = null;
        startButtonIconImage = null;
        switchRect = null;
        switchImage = null;
        switchIconImage = null;
        switchDwellProgressImage = null;
        switchDialogRoot = null;
        switchDialogCancelRect = null;
        switchDialogCancelImage = null;
        switchDialogCancelProgressImage = null;
        switchDialogConfirmRect = null;
        switchDialogConfirmImage = null;
        switchDialogConfirmProgressImage = null;

        var rounded = CreateRoundedSprite(512, 512, 24);
        GameObject canvasObj;
        GameObject panelObj;
        RectTransform panelRt;
        if (!TryCreateMetaUiSetBackplate(parent, out canvasObj, out panelObj, out panelRt))
        {
            throw new System.InvalidOperationException(
                "UI Set backplate prefab is required but could not be created. " +
                "Expected prefab at Assets/Prefabs/UI/EmptyUIBackplateWithCanvas.prefab " +
                "with children CanvasRoot and Surface/UIBackplate.");
        }

        // Host app content directly on the backplate rect.
        var contentHostObj = new GameObject("ContentHost", typeof(RectTransform));
        contentHostObj.transform.SetParent(panelObj.transform, false);
        var contentHostRt = contentHostObj.GetComponent<RectTransform>();
        contentHostRt.anchorMin = Vector2.zero;
        contentHostRt.anchorMax = Vector2.one;
        contentHostRt.offsetMin = Vector2.zero;
        contentHostRt.offsetMax = Vector2.zero;

        var contentRootObj = new GameObject("ContentRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentRootObj.transform.SetParent(contentHostObj.transform, false);
        var contentRootRt = contentRootObj.GetComponent<RectTransform>();
        contentRootRt.anchorMin = Vector2.zero;
        contentRootRt.anchorMax = Vector2.one;
        contentRootRt.offsetMin = Vector2.zero;
        contentRootRt.offsetMax = Vector2.zero;
        var contentLayout = contentRootObj.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(36, 36, 36, 36);
        contentLayout.spacing = 24f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var topRowObj = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        topRowObj.transform.SetParent(contentRootObj.transform, false);
        var topRowLayout = topRowObj.GetComponent<HorizontalLayoutGroup>();
        topRowLayout.spacing = 14f;
        topRowLayout.childAlignment = TextAnchor.MiddleLeft;
        topRowLayout.childControlWidth = true;
        topRowLayout.childControlHeight = false;
        topRowLayout.childForceExpandWidth = false;
        topRowLayout.childForceExpandHeight = false;
        topRowObj.GetComponent<LayoutElement>().preferredHeight = 120f;

        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleObj.transform.SetParent(topRowObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(760f, 110f);
        var titleLayoutElement = titleObj.GetComponent<LayoutElement>();
        titleLayoutElement.minWidth = 0f;
        titleLayoutElement.preferredWidth = 760f;
        titleLayoutElement.flexibleWidth = 1f;
        var titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Hello world";
        titleText.fontSize = 110f;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.color = Color.white;

        var switchClusterObj = new GameObject("SwitchCluster", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        switchClusterObj.transform.SetParent(topRowObj.transform, false);
        var switchClusterLayout = switchClusterObj.GetComponent<HorizontalLayoutGroup>();
        switchClusterLayout.spacing = 10f;
        switchClusterLayout.childAlignment = TextAnchor.MiddleRight;
        switchClusterLayout.childControlWidth = false;
        switchClusterLayout.childControlHeight = false;
        switchClusterLayout.childForceExpandWidth = false;
        switchClusterLayout.childForceExpandHeight = false;
        switchClusterObj.GetComponent<LayoutElement>().flexibleWidth = 0f;

        var bodyObj = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        bodyObj.transform.SetParent(contentRootObj.transform, false);
        bodyObj.GetComponent<LayoutElement>().preferredHeight = 420f;
        var bodyLayout = bodyObj.GetComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 24f;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = false;
        bodyLayout.childControlHeight = false;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = false;

        var subtitleObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        subtitleObj.transform.SetParent(bodyObj.transform, false);
        var subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.sizeDelta = new Vector2(960f, 110f);
        subtitleObj.GetComponent<LayoutElement>().preferredWidth = 960f;
        subtitleText = subtitleObj.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Click the start button";
        subtitleText.fontSize = 62f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.color = new Color(0.90f, 0.95f, 1f, 0.95f);

        GameObject buttonObj = null;
        const string startButtonPrefabPath = "Assets/Prefabs/UI/UnityUIButtonBased/TextTileButton_IconAndLabel_Regular_UnityUIButton.prefab";
        GameObject startButtonPrefab = LoadRequiredPrefab(startButtonPrefabPath, "TextTileButton_IconAndLabel_Regular_UnityUIButton");

        buttonObj = PrefabUtility.InstantiatePrefab(startButtonPrefab, bodyObj.transform) as GameObject;
        buttonObj.name = "PrimaryButton";
        buttonRect = buttonObj.GetComponent<RectTransform>();

        var background = buttonObj.transform.Find("Content/Background");
        buttonImage = background != null
            ? background.GetComponent<Image>()
            : buttonObj.GetComponentInChildren<Image>(true);

        startButtonIconImage = FindRequiredUiSetIconImage(buttonObj.transform);
        if (startButtonIconImage == null)
        {
            throw new System.InvalidOperationException(
                "Required UI Set icon with tag 'QDSUIIcon' was not found on Start button prefab. " +
                "This setup expects a UI Set button variant that includes an icon.");
        }

        var progressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        progressObj.transform.SetParent(startButtonIconImage.transform, false);
        var progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.one;
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = Vector2.zero;

        progressImage = progressObj.GetComponent<Image>();
        progressImage.sprite = CreateCircleSprite(192);
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
        progressImage.fillAmount = 0f;
        progressImage.color = new Color(0.84f, 0.93f, 1f, 0.9f);
        progressImage.raycastTarget = false;

        var switchObj = new GameObject("ModeSwitchButton", typeof(RectTransform), typeof(Image), typeof(Outline));
        switchObj.transform.SetParent(switchClusterObj.transform, false);
        switchRect = switchObj.GetComponent<RectTransform>();
        switchRect.sizeDelta = new Vector2(100f, 100f);

        switchImage = switchObj.GetComponent<Image>();
        switchImage.sprite = rounded;
        switchImage.type = Image.Type.Sliced;
        switchImage.color = new Color(0.92f, 0.96f, 1f, 0.96f);

        var switchOutline = switchObj.GetComponent<Outline>();
        switchOutline.effectColor = new Color(0.24f, 0.38f, 0.60f, 0.30f);
        switchOutline.effectDistance = new Vector2(2f, -2f);
        switchOutline.useGraphicAlpha = true;

        var switchIconImageObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        switchIconImageObj.transform.SetParent(switchObj.transform, false);
        var switchIconImageRect = switchIconImageObj.GetComponent<RectTransform>();
        switchIconImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        switchIconImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        switchIconImageRect.pivot = new Vector2(0.5f, 0.5f);
        switchIconImageRect.anchoredPosition = Vector2.zero;
        switchIconImageRect.sizeDelta = new Vector2(58f, 58f);
        switchIconImage = switchIconImageObj.GetComponent<Image>();
        // Preserve icon pixels as-authored (black drawing + transparent background).
        switchIconImage.color = Color.white;
        switchIconImage.raycastTarget = false;
        switchIconImage.enabled = true;

        var switchProgressObj = new GameObject("ModeSwitchDwellProgress", typeof(RectTransform), typeof(Image));
        switchProgressObj.transform.SetParent(switchObj.transform, false);
        var switchProgressRect = switchProgressObj.GetComponent<RectTransform>();
        switchProgressRect.anchorMin = new Vector2(1f, 0.5f);
        switchProgressRect.anchorMax = new Vector2(1f, 0.5f);
        switchProgressRect.pivot = new Vector2(0f, 0.5f);
        switchProgressRect.anchoredPosition = new Vector2(10f, 0f);
        switchProgressRect.sizeDelta = new Vector2(52f, 52f);

        switchDwellProgressImage = switchProgressObj.GetComponent<Image>();
        switchDwellProgressImage.sprite = CreateCircleSprite(192);
        switchDwellProgressImage.type = Image.Type.Filled;
        switchDwellProgressImage.fillMethod = Image.FillMethod.Radial360;
        switchDwellProgressImage.fillOrigin = (int)Image.Origin360.Top;
        switchDwellProgressImage.fillClockwise = true;
        switchDwellProgressImage.fillAmount = 0f;
        switchDwellProgressImage.color = new Color(0.80f, 0.90f, 1f, 0.95f);
        switchDwellProgressImage.raycastTarget = false;

        switchDialogRoot = new GameObject("ModeSwitchDialog", typeof(RectTransform), typeof(Image), typeof(Outline));
        switchDialogRoot.transform.SetParent(contentHostObj.transform, false);
        var dialogRect = switchDialogRoot.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = new Vector2(0f, 20f);
        dialogRect.sizeDelta = new Vector2(1060f, 410f);
        var dialogImage = switchDialogRoot.GetComponent<Image>();
        dialogImage.sprite = rounded;
        dialogImage.type = Image.Type.Sliced;
        dialogImage.color = new Color(0.03f, 0.06f, 0.10f, 0.98f);
        var dialogOutline = switchDialogRoot.GetComponent<Outline>();
        dialogOutline.effectColor = new Color(0.63f, 0.76f, 0.94f, 0.28f);
        dialogOutline.effectDistance = new Vector2(2f, -2f);
        dialogOutline.useGraphicAlpha = true;

        var dialogTitleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        dialogTitleObj.transform.SetParent(switchDialogRoot.transform, false);
        var dialogTitleRect = dialogTitleObj.GetComponent<RectTransform>();
        dialogTitleRect.anchorMin = new Vector2(0.5f, 1f);
        dialogTitleRect.anchorMax = new Vector2(0.5f, 1f);
        dialogTitleRect.pivot = new Vector2(0.5f, 1f);
        dialogTitleRect.anchoredPosition = new Vector2(0f, -36f);
        dialogTitleRect.sizeDelta = new Vector2(920f, 80f);
        var dialogTitleText = dialogTitleObj.GetComponent<TextMeshProUGUI>();
        dialogTitleText.text = "Switch to controller mode?";
        dialogTitleText.fontSize = 54f;
        dialogTitleText.alignment = TextAlignmentOptions.Center;
        dialogTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        dialogTitleText.color = Color.white;

        var dialogBodyObj = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        dialogBodyObj.transform.SetParent(switchDialogRoot.transform, false);
        var dialogBodyRect = dialogBodyObj.GetComponent<RectTransform>();
        dialogBodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogBodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogBodyRect.pivot = new Vector2(0.5f, 0.5f);
        dialogBodyRect.anchoredPosition = new Vector2(0f, 32f);
        dialogBodyRect.sizeDelta = new Vector2(940f, 150f);
        var dialogBodyText = dialogBodyObj.GetComponent<TextMeshProUGUI>();
        dialogBodyText.text = "Switching without controllers can lock interaction.\nDo you want to continue?";
        dialogBodyText.fontSize = 36f;
        dialogBodyText.alignment = TextAlignmentOptions.Center;
        dialogBodyText.textWrappingMode = TextWrappingModes.Normal;
        dialogBodyText.color = new Color(0.87f, 0.93f, 1f, 0.97f);

        var cancelObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Outline));
        cancelObj.transform.SetParent(switchDialogRoot.transform, false);
        switchDialogCancelRect = cancelObj.GetComponent<RectTransform>();
        switchDialogCancelRect.anchorMin = new Vector2(0.5f, 0f);
        switchDialogCancelRect.anchorMax = new Vector2(0.5f, 0f);
        switchDialogCancelRect.pivot = new Vector2(0.5f, 0f);
        switchDialogCancelRect.anchoredPosition = new Vector2(-180f, 32f);
        switchDialogCancelRect.sizeDelta = new Vector2(330f, 92f);
        switchDialogCancelImage = cancelObj.GetComponent<Image>();
        switchDialogCancelImage.sprite = rounded;
        switchDialogCancelImage.type = Image.Type.Sliced;
        switchDialogCancelImage.color = new Color(0.18f, 0.45f, 0.94f, 1f);
        var cancelOutline = cancelObj.GetComponent<Outline>();
        cancelOutline.effectColor = new Color(0.75f, 0.86f, 1f, 0.20f);
        cancelOutline.effectDistance = new Vector2(2f, -2f);
        cancelOutline.useGraphicAlpha = true;

        var cancelTextObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        cancelTextObj.transform.SetParent(cancelObj.transform, false);
        var cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.offsetMin = Vector2.zero;
        cancelTextRect.offsetMax = Vector2.zero;
        var cancelText = cancelTextObj.GetComponent<TextMeshProUGUI>();
        cancelText.text = "Cancel";
        cancelText.fontSize = 40f;
        cancelText.alignment = TextAlignmentOptions.Center;
        cancelText.textWrappingMode = TextWrappingModes.NoWrap;
        cancelText.color = Color.white;

        var cancelProgressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        cancelProgressObj.transform.SetParent(cancelObj.transform, false);
        var cancelProgressRect = cancelProgressObj.GetComponent<RectTransform>();
        cancelProgressRect.anchorMin = new Vector2(1f, 0.5f);
        cancelProgressRect.anchorMax = new Vector2(1f, 0.5f);
        cancelProgressRect.pivot = new Vector2(1f, 0.5f);
        cancelProgressRect.anchoredPosition = new Vector2(-12f, 0f);
        cancelProgressRect.sizeDelta = new Vector2(46f, 46f);
        switchDialogCancelProgressImage = cancelProgressObj.GetComponent<Image>();
        switchDialogCancelProgressImage.sprite = CreateCircleSprite(192);
        switchDialogCancelProgressImage.type = Image.Type.Filled;
        switchDialogCancelProgressImage.fillMethod = Image.FillMethod.Radial360;
        switchDialogCancelProgressImage.fillOrigin = (int)Image.Origin360.Top;
        switchDialogCancelProgressImage.fillClockwise = true;
        switchDialogCancelProgressImage.fillAmount = 0f;
        switchDialogCancelProgressImage.color = new Color(0.86f, 0.93f, 1f, 0.95f);
        switchDialogCancelProgressImage.raycastTarget = false;

        var confirmObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Outline));
        confirmObj.transform.SetParent(switchDialogRoot.transform, false);
        switchDialogConfirmRect = confirmObj.GetComponent<RectTransform>();
        switchDialogConfirmRect.anchorMin = new Vector2(0.5f, 0f);
        switchDialogConfirmRect.anchorMax = new Vector2(0.5f, 0f);
        switchDialogConfirmRect.pivot = new Vector2(0.5f, 0f);
        switchDialogConfirmRect.anchoredPosition = new Vector2(180f, 32f);
        switchDialogConfirmRect.sizeDelta = new Vector2(250f, 92f);
        switchDialogConfirmImage = confirmObj.GetComponent<Image>();
        switchDialogConfirmImage.sprite = rounded;
        switchDialogConfirmImage.type = Image.Type.Sliced;
        switchDialogConfirmImage.color = new Color(0.10f, 0.14f, 0.20f, 1f);
        var confirmOutline = confirmObj.GetComponent<Outline>();
        confirmOutline.effectColor = new Color(0.64f, 0.77f, 0.95f, 0.25f);
        confirmOutline.effectDistance = new Vector2(2f, -2f);
        confirmOutline.useGraphicAlpha = true;

        var confirmTextObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        confirmTextObj.transform.SetParent(confirmObj.transform, false);
        var confirmTextRect = confirmTextObj.GetComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        confirmTextRect.offsetMin = Vector2.zero;
        confirmTextRect.offsetMax = Vector2.zero;
        var confirmText = confirmTextObj.GetComponent<TextMeshProUGUI>();
        confirmText.text = "Confirm";
        confirmText.fontSize = 34f;
        confirmText.alignment = TextAlignmentOptions.Center;
        confirmText.textWrappingMode = TextWrappingModes.NoWrap;
        confirmText.color = new Color(0.86f, 0.93f, 1f, 0.98f);

        var confirmProgressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        confirmProgressObj.transform.SetParent(confirmObj.transform, false);
        var confirmProgressRect = confirmProgressObj.GetComponent<RectTransform>();
        confirmProgressRect.anchorMin = new Vector2(1f, 0.5f);
        confirmProgressRect.anchorMax = new Vector2(1f, 0.5f);
        confirmProgressRect.pivot = new Vector2(1f, 0.5f);
        confirmProgressRect.anchoredPosition = new Vector2(-12f, 0f);
        confirmProgressRect.sizeDelta = new Vector2(46f, 46f);
        switchDialogConfirmProgressImage = confirmProgressObj.GetComponent<Image>();
        switchDialogConfirmProgressImage.sprite = CreateCircleSprite(192);
        switchDialogConfirmProgressImage.type = Image.Type.Filled;
        switchDialogConfirmProgressImage.fillMethod = Image.FillMethod.Radial360;
        switchDialogConfirmProgressImage.fillOrigin = (int)Image.Origin360.Top;
        switchDialogConfirmProgressImage.fillClockwise = true;
        switchDialogConfirmProgressImage.fillAmount = 0f;
        switchDialogConfirmProgressImage.color = new Color(0.86f, 0.93f, 1f, 0.95f);
        switchDialogConfirmProgressImage.raycastTarget = false;

        switchDialogRoot.SetActive(false);

        return panelObj.transform;
    }

    private static bool TryCreateMetaUiSetBackplate(
        Transform parent,
        out GameObject canvasObj,
        out GameObject panelObj,
        out RectTransform panelRt)
    {
        canvasObj = null;
        panelObj = null;
        panelRt = null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/EmptyUIBackplateWithCanvas.prefab");
        if (prefab == null)
            return false;

        GameObject root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (root == null)
            return false;

        root.name = "WelcomeCanvas";
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, -0.15f, 0.45f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Transform canvasRoot = root.transform.Find("CanvasRoot");
        // In the copied UI Set prefab, UIBackplate is directly under CanvasRoot.
        Transform backplate = canvasRoot != null ? canvasRoot.Find("UIBackplate") : null;
        if (canvasRoot == null || backplate == null)
        {
            Object.DestroyImmediate(root);
            return false;
        }

        canvasObj = canvasRoot.gameObject;
        panelObj = backplate.gameObject;
        canvasRoot.localScale = Vector3.one * 0.0018f;

        var canvas = canvasObj.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
        }

        var canvasRect = canvasObj.GetComponent<RectTransform>();
        if (canvasRect != null)
            canvasRect.sizeDelta = new Vector2(1600f, 1080f);

        panelRt = panelObj.GetComponent<RectTransform>();
        if (panelRt == null)
            panelRt = panelObj.AddComponent<RectTransform>();

        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(925f, 600f);

        // Disable package layout components so our own layout groups control positioning.
        var layoutGroups = panelObj.GetComponents<LayoutGroup>();
        foreach (var group in layoutGroups)
            group.enabled = false;

        Debug.Log("VRVideoPlayerSetup: Using UI Set backplate prefab.");

        return true;
    }

    private static Sprite CreateRoundedSprite(int width, int height, int radius)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "RoundedPanelSprite";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float maxRadius = Mathf.Min(width, height) * 0.5f - 2f;
        float clampedRadius = Mathf.Clamp(radius, 2f, maxRadius);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float signedDistance = SignedDistanceToRoundedRect(
                    x + 0.5f,
                    y + 0.5f,
                    width,
                    height,
                    clampedRadius);

                tex.SetPixel(x, y, signedDistance <= 0f ? Color.white : Color.clear);
            }
        }

        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        Vector4 border = new Vector4(clampedRadius, clampedRadius, clampedRadius, clampedRadius);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, border);
    }

    private static float SignedDistanceToRoundedRect(float px, float py, int width, int height, float radius)
    {
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float cx = px - halfW;
        float cy = py - halfH;

        float bx = Mathf.Abs(cx) - (halfW - radius);
        float by = Mathf.Abs(cy) - (halfH - radius);

        float outsideX = Mathf.Max(bx, 0f);
        float outsideY = Mathf.Max(by, 0f);
        float outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
        float insideDistance = Mathf.Min(Mathf.Max(bx, by), 0f);

        return outsideDistance + insideDistance - radius;
    }

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "DwellProgressCircle";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float radius = size * 0.46f;
        float feather = 1.6f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.Clamp01((d - radius) / feather);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
