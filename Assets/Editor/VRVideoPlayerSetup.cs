using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Editor menu to set up a minimal passthrough + welcome screen scene.
/// </summary>
public static class VRVideoPlayerSetup
{
    private const string SecondaryButtonPrefabPath = "Assets/Prefabs/UI/UnityUIButtonBased/SecondaryButton_IconAndLabel_UnityUIButton.prefab";
    private const string FilePickerButtonVariantPath = "Assets/Prefabs/UI/UnityUIButtonBased/FilePickerButton_UnityUIButton.prefab";
    private const string UiSetDialog2ButtonTextOnlyLocalPath = "Assets/Prefabs/UI/Dialog/Dialog2Button_TextOnly.prefab";
    private const string UiSetDialog2ButtonTextOnlyPackagePath = "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/UISet/Prefabs/Dialog/Dialog2Button_TextOnly.prefab";

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
            out RectTransform contentHostRect,
            out RectTransform buttonRect,
            out Image buttonImage,
            out TextMeshProUGUI subtitleText,
            out RectTransform filePickerRect,
            out Image filePickerProgressImage,
            out Image filePickerIconImage,
            out GameObject filePickerTooltipRoot,
            out TextMeshProUGUI filePickerTooltipText,
            out Image progressImage,
            out Image startButtonIconImage,
            out GameObject startButtonTooltipRoot,
            out TextMeshProUGUI startButtonTooltipText,
            out RectTransform switchRect,
            out Image switchImage,
            out Image switchIconImage,
            out Image switchDwellProgressImage,
            out GameObject switchTooltipRoot,
            out TextMeshProUGUI switchTooltipText);

        var modeManager = welcomeRoot.AddComponent<ControlModeManager>();
        modeManager.headGazePointer = headPointer;
        modeManager.showControllerPointer = false; // Use native ISDK controller ray cursor.
        modeManager.switchButtonRect = switchRect;
        modeManager.switchButtonImage = switchImage;
        modeManager.switchButtonIconImage = switchIconImage;
        modeManager.switchDwellProgressImage = switchDwellProgressImage;
        var lockedIcon = LoadSpriteFromAtlasByName("Assets/Images/OCUI_24_Filled_2x.png", "icon_lock_24_Filled");
        if (lockedIcon == null)
            lockedIcon = LoadSpriteByNameAcrossProject("icon_lock_24_Filled");
        if (lockedIcon == null)
            lockedIcon = LoadSpriteFromImagesFile("lock-on.png");
        if (lockedIcon == null)
        {
            Debug.LogWarning("VRVideoPlayerSetup: Could not find lock icon sprite. Using generated fallback icon.");
            lockedIcon = CreateLockOnSprite(128, 128);
        }
        var unlockedIcon = LoadSpriteFromAtlasByName("Assets/Images/OCUI_24_Filled_2x.png", "icon_lock-off_24_Filled");
        if (unlockedIcon == null)
            unlockedIcon = LoadSpriteByNameAcrossProject("icon_lock-off_24_Filled");
        if (unlockedIcon == null)
            unlockedIcon = LoadSpriteByNameAcrossProject("icon_lockoff_24_Filled");
        if (unlockedIcon == null)
            unlockedIcon = lockedIcon;
        modeManager.lockedIconSprite = lockedIcon;
        modeManager.unlockedIconSprite = unlockedIcon;
        modeManager.switchControllerOnly = true;
        modeManager.switchControllerOnlyTooltipRoot = switchTooltipRoot;
        modeManager.switchControllerOnlyTooltipText = switchTooltipText;
        modeManager.switchControllerOnlyTooltip = "This button is only available with controllers.";

        var startInteractor = welcomeRoot.AddComponent<StartButtonInteractor>();
        startInteractor.modeManager = modeManager;
        startInteractor.targetRect = buttonRect;
        startInteractor.progressImage = progressImage;
        startInteractor.iconToReplaceWhileProgress = startButtonIconImage;
        startInteractor.controllerOnlyTooltipRoot = startButtonTooltipRoot;
        startInteractor.controllerOnlyTooltipText = startButtonTooltipText;
        startInteractor.controllerOnlyTooltipOffset = new Vector2(0f, -8f);
        startInteractor.dwellSeconds = 1.0f;
        startInteractor.controllerOnly = true;
        startInteractor.controllerOnlyTooltip = "This button is only available with controllers.";

        var filePickerInteractor = welcomeRoot.AddComponent<LibraryFilePickerInteractor>();
        filePickerInteractor.modeManager = modeManager;
        filePickerInteractor.targetRect = filePickerRect;
        filePickerInteractor.progressImage = filePickerProgressImage;
        filePickerInteractor.iconToReplaceWhileProgress = filePickerIconImage;
        filePickerInteractor.statusText = subtitleText;
        filePickerInteractor.dwellSeconds = 1.0f;
        filePickerInteractor.controllerOnly = true;
        filePickerInteractor.onlyVisibleWhenUnlocked = true;
        filePickerInteractor.controllerOnlyTooltipRoot = filePickerTooltipRoot;
        filePickerInteractor.controllerOnlyTooltipText = filePickerTooltipText;
        filePickerInteractor.controllerOnlyTooltip = "This button is only available with controllers.";
        filePickerInteractor.noSelectionStatus = "No videos in the library";
        filePickerInteractor.pickingStatus = "Opening file picker...";
        filePickerInteractor.pickedStatusFormat = "Selected: {0}";
        filePickerInteractor.pickerUnavailableStatus = "Picker unavailable. See logcat.";

        BuildFileSelectionDialog(
            contentHostRect,
            modeManager,
            subtitleText,
            out FileSelectionDialogController dialogController);
        if (dialogController != null)
        {
            filePickerInteractor.dialogController = dialogController;
            if (filePickerInteractor.onFilePicked == null)
                filePickerInteractor.onFilePicked = new LibraryFilePickerInteractor.FilePickedEvent();
            Debug.Log("VRVideoPlayerSetup: File picker dialog controller wired.");
        }

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
        }

        // Fix: PointableCanvasModule doesn't call SendUpdateEventToSelectedObject(),
        // which breaks TMP_InputField focus and system keyboard triggering.
        if (pointableCanvasModuleObject.GetComponent<PointableCanvasInputFix>() == null)
            pointableCanvasModuleObject.AddComponent<PointableCanvasInputFix>();
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

    private static Sprite LoadSpriteByNameAcrossProject(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
            return null;

        string normalizedTarget = NormalizeName(spriteName);
        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { "Assets", "Packages" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
                continue;

            foreach (Object asset in assets)
            {
                if (!(asset is Sprite sprite) || sprite == null)
                    continue;

                string normalizedName = NormalizeName(sprite.name);
                if (normalizedName == normalizedTarget || normalizedName.Contains(normalizedTarget))
                    return sprite;
            }
        }

        return null;
    }

    private static Sprite LoadSpriteFromAtlasByName(string atlasPath, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(atlasPath) || string.IsNullOrWhiteSpace(spriteName))
            return null;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
        if (assets == null || assets.Length == 0)
            return null;

        string normalizedTarget = NormalizeName(spriteName);
        foreach (Object asset in assets)
        {
            if (!(asset is Sprite sprite) || sprite == null)
                continue;

            string normalizedName = NormalizeName(sprite.name);
            if (normalizedName == normalizedTarget || normalizedName.Contains(normalizedTarget))
                return sprite;
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.ToLowerInvariant()
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);
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

    private static GameObject EnsureFilePickerButtonPrefabVariant()
    {
        var basePrefab = LoadRequiredPrefab(SecondaryButtonPrefabPath, "SecondaryButton_IconAndLabel_UnityUIButton");
        var tempInstance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        if (tempInstance == null)
            throw new System.InvalidOperationException("Failed to instantiate SecondaryButton_IconAndLabel_UnityUIButton while creating file picker variant.");

        try
        {
            tempInstance.name = "FilePickerButton_UnityUIButton";

            var rect = tempInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                float widened = Mathf.Max(1f, rect.sizeDelta.x) * (4f / 3f);
                rect.sizeDelta = new Vector2(widened, rect.sizeDelta.y);
            }

            var layout = tempInstance.GetComponent<LayoutElement>();
            if (layout == null)
                layout = tempInstance.AddComponent<LayoutElement>();
            if (rect != null)
                layout.preferredWidth = rect.sizeDelta.x;
            layout.minWidth = 0f;
            layout.flexibleWidth = 0f;

            var label = tempInstance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = "Choose video";

            ApplyFilePickerButtonVisualLayout(tempInstance);

            var saved = PrefabUtility.SaveAsPrefabAsset(tempInstance, FilePickerButtonVariantPath);
            if (saved == null)
            {
                throw new System.InvalidOperationException(
                    $"Failed to save file picker button prefab variant at '{FilePickerButtonVariantPath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return saved;
        }
        finally
        {
            Object.DestroyImmediate(tempInstance);
        }
    }

    private static void ApplyFilePickerButtonVisualLayout(GameObject buttonObj)
    {
        if (buttonObj == null)
            return;

        // In this UI Set button, icon/text alignment is driven by the Elements row.
        // Fallback to Background in case prefab internals differ across package versions.
        HorizontalLayoutGroup layout = null;
        var elementsTransform = buttonObj.transform.Find("Content/Background/Elements");
        if (elementsTransform != null)
            layout = elementsTransform.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            var backgroundTransform = buttonObj.transform.Find("Content/Background");
            if (backgroundTransform != null)
                layout = backgroundTransform.GetComponent<HorizontalLayoutGroup>();
        }
        if (layout == null)
            return;

        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 0f;

        if (layout.padding == null)
            layout.padding = new RectOffset();
        layout.padding.left = 15;
    }

    private static void BuildFileSelectionDialog(
        RectTransform contentHostRect,
        ControlModeManager modeManager,
        TMP_Text subtitleText,
        out FileSelectionDialogController dialogController)
    {
        dialogController = null;
        if (contentHostRect == null)
            return;

        GameObject dialogPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(UiSetDialog2ButtonTextOnlyLocalPath) ??
            AssetDatabase.LoadAssetAtPath<GameObject>(UiSetDialog2ButtonTextOnlyPackagePath);
        if (dialogPrefab == null)
        {
            Debug.LogWarning(
                "VRVideoPlayerSetup: UI Set dialog prefab not found. " +
                $"Checked '{UiSetDialog2ButtonTextOnlyLocalPath}' and '{UiSetDialog2ButtonTextOnlyPackagePath}'.");
            return;
        }

        var dialogHostObj = new GameObject("FileSelectionDialogHost", typeof(RectTransform));
        dialogHostObj.transform.SetParent(contentHostRect, false);
        var dialogHostRt = dialogHostObj.GetComponent<RectTransform>();
        dialogHostRt.anchorMin = Vector2.zero;
        dialogHostRt.anchorMax = Vector2.one;
        dialogHostRt.offsetMin = Vector2.zero;
        dialogHostRt.offsetMax = Vector2.zero;

        var dialogObj = PrefabUtility.InstantiatePrefab(dialogPrefab, dialogHostObj.transform) as GameObject;
        if (dialogObj == null)
            return;

        dialogObj.name = "FileSelectionDialog";
        var dialogRect = dialogObj.GetComponent<RectTransform>();
        if (dialogRect != null)
        {
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = new Vector2(0f, 24f);
        }

        TextMeshProUGUI title = FindDialogTitleText(dialogObj);

        if (!TryGetDialogActionTargets(dialogObj, out RectTransform saveTarget, out RectTransform exitTarget))
        {
            Debug.LogWarning("VRVideoPlayerSetup: Dialog prefab does not contain two action targets (Button/Toggle/Selectable).");
            dialogObj.SetActive(false);
            return;
        }

        ResolveDialogActionRolesFromPrefab(saveTarget, exitTarget, out RectTransform resolvedSaveTarget, out RectTransform resolvedExitTarget);

        dialogController = dialogObj.AddComponent<FileSelectionDialogController>();
        dialogController.modeManager = modeManager;
        dialogController.dialogRoot = dialogHostObj;
        dialogController.titleText = title;
        dialogController.bodyText = FindDialogBodyText(dialogObj, title, resolvedSaveTarget, resolvedExitTarget);
        dialogController.subtitleText = subtitleText;

        // Name field: TMP_InputField must exist in the prefab under LibraryNameInput (no runtime UI creation).
        dialogController.nameInput = FindDialogNameInputField(dialogObj);
        dialogController.nameHelperText = FindDialogNameHelperText(dialogObj);

        ConfigureDialogButtonInteractor(resolvedSaveTarget, modeManager, contentHostRect, dialogController, UiSetButtonInteractor.DialogAction.Save);
        ConfigureDialogButtonInteractor(resolvedExitTarget, modeManager, contentHostRect, dialogController, UiSetButtonInteractor.DialogAction.Exit);

        dialogHostObj.SetActive(false);
    }

    private static void ResolveDialogActionRolesFromPrefab(RectTransform first, RectTransform second, out RectTransform save, out RectTransform exit)
    {
        save = first;
        exit = second;
        if (first == null || second == null)
            return;

        bool firstLooksSave = LooksLikeAction(first, "save", "confirm", "ok");
        bool firstLooksExit = LooksLikeAction(first, "exit", "cancel", "close");
        bool secondLooksSave = LooksLikeAction(second, "save", "confirm", "ok");
        bool secondLooksExit = LooksLikeAction(second, "exit", "cancel", "close");

        if (firstLooksSave && secondLooksExit)
        {
            save = first;
            exit = second;
            return;
        }
        if (secondLooksSave && firstLooksExit)
        {
            save = second;
            exit = first;
            return;
        }

        // Fallback: keep current ordering from prefab and log so designers can rename controls for deterministic mapping.
        Debug.LogWarning(
            "VRVideoPlayerSetup: Could not infer Save/Exit controls from prefab names/text. " +
            "Using prefab order for action wiring. Rename button objects/text to include 'Save' and 'Exit' for deterministic mapping.");
    }

    private static bool LooksLikeAction(RectTransform target, params string[] keywords)
    {
        if (target == null)
            return false;

        string name = target.gameObject.name.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            if (name.Contains(keywords[i]))
                return true;
        }

        var text = target.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null || string.IsNullOrWhiteSpace(text.text))
            return false;
        string label = text.text.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            if (label.Contains(keywords[i]))
                return true;
        }

        return false;
    }

    private static TextMeshProUGUI FindDialogTitleText(GameObject dialogObj)
    {
        if (dialogObj == null)
            return null;

        var allTexts = dialogObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI byName = null;
        for (int i = 0; i < allTexts.Length; i++)
        {
            var t = allTexts[i];
            if (t == null)
                continue;
            string n = t.gameObject.name.ToLowerInvariant();
            if (n.Contains("title") || n.Contains("header"))
            {
                byName = t;
                break;
            }
        }
        if (byName != null)
            return byName;

        TextMeshProUGUI largest = null;
        float maxSize = float.MinValue;
        for (int i = 0; i < allTexts.Length; i++)
        {
            var t = allTexts[i];
            if (t == null)
                continue;
            if (t.fontSize > maxSize)
            {
                maxSize = t.fontSize;
                largest = t;
            }
        }
        return largest;
    }

    private static TextMeshProUGUI FindDialogBodyText(
        GameObject dialogObj,
        TextMeshProUGUI titleText,
        RectTransform saveTarget,
        RectTransform exitTarget)
    {
        if (dialogObj == null)
            return null;

        // Exclude text fields inside the name input group
        Transform nameInputRoot = dialogObj.transform.Find("LibraryNameInput");

        var allTexts = dialogObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI best = null;
        int bestLen = -1;

        foreach (var t in allTexts)
        {
            if (t == null || t == titleText)
                continue;

            if (saveTarget != null && t.transform.IsChildOf(saveTarget))
                continue;
            if (exitTarget != null && t.transform.IsChildOf(exitTarget))
                continue;
            if (nameInputRoot != null && t.transform.IsChildOf(nameInputRoot))
                continue;

            int len = string.IsNullOrWhiteSpace(t.text) ? 0 : t.text.Trim().Length;
            if (len > bestLen)
            {
                best = t;
                bestLen = len;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds a <see cref="TMP_InputField"/> already present under <c>LibraryNameInput</c> in the dialog prefab.
    /// Does not create or modify prefab hierarchy.
    /// </summary>
    private static TMP_InputField FindDialogNameInputField(GameObject dialogObj)
    {
        if (dialogObj == null)
            return null;

        var nameInputRoot = dialogObj.transform.Find("LibraryNameInput");
        if (nameInputRoot == null)
        {
            Debug.LogWarning(
                "VRVideoPlayerSetup: LibraryNameInput not found in dialog prefab. Add LibraryNameInput with a TMP_InputField (e.g. UI Set TextField).");
            return null;
        }

        // Paths use '/' — Unity resolves nested children (e.g. UI Set TextInputField prefab).
        string[] preferredPaths =
        {
            "TextInputField/TextField",
            "TextField",
            "InputField",
            "NameInput",
        };
        for (int i = 0; i < preferredPaths.Length; i++)
        {
            var t = nameInputRoot.Find(preferredPaths[i]);
            if (t == null)
                continue;
            var field = t.GetComponent<TMP_InputField>();
            if (field != null)
                return field;
        }

        var found = nameInputRoot.GetComponentInChildren<TMP_InputField>(true);
        if (found == null)
        {
            Debug.LogWarning(
                "VRVideoPlayerSetup: No TMP_InputField under LibraryNameInput. Add one in the dialog prefab (Meta UI Set uses child name TextField).");
        }

        return found;
    }

    /// <summary>
    /// Locates the HelperText label under LibraryNameInput.
    /// </summary>
    private static TextMeshProUGUI FindDialogNameHelperText(GameObject dialogObj)
    {
        if (dialogObj == null)
            return null;

        var nameInputRoot = dialogObj.transform.Find("LibraryNameInput");
        if (nameInputRoot == null)
            return null;

        string[] helperPaths = { "HelperText", "TextInputField/HelperText" };
        for (int i = 0; i < helperPaths.Length; i++)
        {
            var helperTf = nameInputRoot.Find(helperPaths[i]);
            if (helperTf == null)
                continue;
            // UI Set: TMP is usually on a child (e.g. "Text"), not on the HelperText root.
            var tmp = helperTf.GetComponent<TextMeshProUGUI>()
                      ?? helperTf.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                return tmp;
        }

        // Fallback: first TMPro under LibraryNameInput named HelperText (any depth).
        foreach (var tr in nameInputRoot.GetComponentsInChildren<Transform>(true))
        {
            if (tr == null || tr.name != "HelperText")
                continue;
            var tmp = tr.GetComponent<TextMeshProUGUI>()
                      ?? tr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                return tmp;
        }

        return null;
    }


    private static bool TryGetDialogActionTargets(GameObject dialogObj, out RectTransform first, out RectTransform second)
    {
        first = null;
        second = null;
        if (dialogObj == null)
            return false;

        var buttons = dialogObj.GetComponentsInChildren<Button>(true);
        if (buttons.Length >= 2)
        {
            first = buttons[0].GetComponent<RectTransform>();
            second = buttons[1].GetComponent<RectTransform>();
            return first != null && second != null;
        }

        var toggles = dialogObj.GetComponentsInChildren<Toggle>(true);
        if (toggles.Length >= 2)
        {
            first = toggles[0].GetComponent<RectTransform>();
            second = toggles[1].GetComponent<RectTransform>();
            return first != null && second != null;
        }

        var selectables = dialogObj.GetComponentsInChildren<Selectable>(true);
        var uniqueRoots = new List<RectTransform>();
        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;
            var rt = selectable.GetComponent<RectTransform>();
            if (rt == null)
                continue;
            if (!uniqueRoots.Contains(rt))
                uniqueRoots.Add(rt);
        }

        if (uniqueRoots.Count >= 2)
        {
            first = uniqueRoots[0];
            second = uniqueRoots[1];
            return true;
        }

        return false;
    }

    private static void ConfigureDialogButtonInteractor(
        RectTransform actionTarget,
        ControlModeManager modeManager,
        RectTransform contentHostRect,
        FileSelectionDialogController dialogController,
        UiSetButtonInteractor.DialogAction dialogAction)
    {
        if (actionTarget == null || modeManager == null)
            return;

        var interactor = actionTarget.gameObject.AddComponent<UiSetButtonInteractor>();
        interactor.modeManager = modeManager;
        interactor.targetRect = actionTarget;
        interactor.dwellSeconds = 1.0f;
        interactor.controllerOnly = false;
        interactor.requireUnlockedMode = false;
        interactor.controllerOnlyTooltip = "This button is only available with controllers.";
        interactor.dialogController = dialogController;
        interactor.dialogAction = dialogAction;

        var icon = FindRequiredUiSetIconImage(actionTarget.transform);
        interactor.iconToReplaceWhileProgress = icon;

        var progressParent = icon != null ? icon.transform : actionTarget.transform;
        var progressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        progressObj.transform.SetParent(progressParent, false);
        var progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.one;
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = Vector2.zero;

        var progressImage = progressObj.GetComponent<Image>();
        progressImage.sprite = CreateCircleSprite(192);
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
        progressImage.fillAmount = 0f;
        progressImage.color = new Color(0.84f, 0.93f, 1f, 0.9f);
        progressImage.raycastTarget = false;
        interactor.progressImage = progressImage;

        // Critical for text-only dialog buttons: keep injected progress overlay out of layout calculations
        // so it doesn't shift/offset label alignment.
        var progressLayout = progressObj.AddComponent<LayoutElement>();
        progressLayout.ignoreLayout = true;

        GameObject tooltipPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip/Tooltip.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/tooltip.prefab");
        if (tooltipPrefab == null)
            return;

        var tooltipRoot = PrefabUtility.InstantiatePrefab(tooltipPrefab, contentHostRect) as GameObject;
        if (tooltipRoot == null)
            return;

        tooltipRoot.name = $"{actionTarget.gameObject.name}ControllerOnlyTooltip";
        var tooltipRt = tooltipRoot.GetComponent<RectTransform>();
        if (tooltipRt != null)
        {
            var pos = tooltipRoot.AddComponent<TooltipPositioner>();
            pos.tooltipRect = tooltipRt;
            pos.anchorRect = interactor.targetRect;
            pos.boundsRect = contentHostRect;
            pos.positioningRoot = contentHostRect;
            pos.preferredPlacement = TooltipPositioner.Placement.Top;
            pos.gap = 6f;
            pos.offset = new Vector2(0f, -8f);
            pos.keepWithinBounds = true;
        }

        var tooltipText = tooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tooltipText != null)
            tooltipText.raycastTarget = false;
        tooltipRoot.SetActive(false);

        interactor.controllerOnlyTooltipRoot = tooltipRoot;
        interactor.controllerOnlyTooltipText = tooltipText;
    }

    private static Transform BuildWelcomeCanvas(
        Transform parent,
        out RectTransform contentHostRect,
        out RectTransform buttonRect,
        out Image buttonImage,
        out TextMeshProUGUI subtitleText,
        out RectTransform filePickerRect,
        out Image filePickerProgressImage,
        out Image filePickerIconImage,
        out GameObject filePickerTooltipRoot,
        out TextMeshProUGUI filePickerTooltipText,
        out Image progressImage,
        out Image startButtonIconImage,
        out GameObject startButtonTooltipRoot,
        out TextMeshProUGUI startButtonTooltipText,
        out RectTransform switchRect,
        out Image switchImage,
        out Image switchIconImage,
        out Image switchDwellProgressImage,
        out GameObject switchTooltipRoot,
        out TextMeshProUGUI switchTooltipText)
    {
        contentHostRect = null;
        buttonRect = null;
        buttonImage = null;
        subtitleText = null;
        filePickerRect = null;
        filePickerProgressImage = null;
        filePickerIconImage = null;
        filePickerTooltipRoot = null;
        filePickerTooltipText = null;
        progressImage = null;
        startButtonIconImage = null;
        startButtonTooltipRoot = null;
        startButtonTooltipText = null;
        switchRect = null;
        switchImage = null;
        switchIconImage = null;
        switchDwellProgressImage = null;
        switchTooltipRoot = null;
        switchTooltipText = null;
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
        contentHostRect = contentHostObj.GetComponent<RectTransform>();
        contentHostRect.anchorMin = Vector2.zero;
        contentHostRect.anchorMax = Vector2.one;
        contentHostRect.offsetMin = Vector2.zero;
        contentHostRect.offsetMax = Vector2.zero;

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
        titleText.text = "Video library";
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
        subtitleText.text = "No videos in the library";
        subtitleText.fontSize = 62f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.color = new Color(0.90f, 0.95f, 1f, 0.95f);

        GameObject filePickerButtonPrefab = EnsureFilePickerButtonPrefabVariant();
        GameObject filePickerObj = PrefabUtility.InstantiatePrefab(filePickerButtonPrefab, bodyObj.transform) as GameObject;
        if (filePickerObj == null)
            throw new System.InvalidOperationException("Failed to instantiate FilePickerButton prefab variant.");
        filePickerObj.name = "FilePickerButton";
        filePickerRect = filePickerObj.GetComponent<RectTransform>();
        ApplyFilePickerButtonVisualLayout(filePickerObj);

        filePickerIconImage = FindRequiredUiSetIconImage(filePickerObj.transform);
        if (filePickerIconImage == null)
            throw new System.InvalidOperationException("File picker button requires an icon child tagged 'QDSUIIcon'.");

        var folderIcon = LoadSpriteFromAtlasByName("Assets/Images/OCUI_24_Filled_2x.png", "icon_folder_24_Filled");
        if (folderIcon == null)
            folderIcon = LoadSpriteByNameAcrossProject("icon_folder_24_Filled");
        if (folderIcon != null)
            filePickerIconImage.sprite = folderIcon;

        var filePickerProgressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        filePickerProgressObj.transform.SetParent(filePickerIconImage.transform, false);
        var filePickerProgressRect = filePickerProgressObj.GetComponent<RectTransform>();
        filePickerProgressRect.anchorMin = Vector2.zero;
        filePickerProgressRect.anchorMax = Vector2.one;
        filePickerProgressRect.pivot = new Vector2(0.5f, 0.5f);
        filePickerProgressRect.anchoredPosition = Vector2.zero;
        filePickerProgressRect.sizeDelta = Vector2.zero;

        filePickerProgressImage = filePickerProgressObj.GetComponent<Image>();
        filePickerProgressImage.sprite = CreateCircleSprite(192);
        filePickerProgressImage.type = Image.Type.Filled;
        filePickerProgressImage.fillMethod = Image.FillMethod.Radial360;
        filePickerProgressImage.fillOrigin = (int)Image.Origin360.Top;
        filePickerProgressImage.fillClockwise = true;
        filePickerProgressImage.fillAmount = 0f;
        filePickerProgressImage.color = new Color(0.84f, 0.93f, 1f, 0.9f);
        filePickerProgressImage.raycastTarget = false;

        GameObject filePickerTooltipPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip/Tooltip.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/tooltip.prefab");
        if (filePickerTooltipPrefab != null)
        {
            filePickerTooltipRoot = PrefabUtility.InstantiatePrefab(filePickerTooltipPrefab, contentHostObj.transform) as GameObject;
            if (filePickerTooltipRoot != null)
            {
                filePickerTooltipRoot.name = "FilePickerControllerOnlyTooltip";
                var tooltipRt = filePickerTooltipRoot.GetComponent<RectTransform>();
                if (tooltipRt != null)
                {
                    var pos = filePickerTooltipRoot.AddComponent<TooltipPositioner>();
                    pos.tooltipRect = tooltipRt;
                    pos.anchorRect = filePickerRect;
                    pos.boundsRect = contentHostRect;
                    pos.positioningRoot = contentHostRect;
                    pos.preferredPlacement = TooltipPositioner.Placement.Top;
                    pos.gap = 4f;
                    pos.offset = new Vector2(0f, -8f);
                    pos.keepWithinBounds = true;
                }

                filePickerTooltipText = filePickerTooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (filePickerTooltipText != null)
                    filePickerTooltipText.raycastTarget = false;

                filePickerTooltipRoot.SetActive(false);
            }
        }

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

        GameObject tooltipPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip/Tooltip.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/tooltip.prefab");
        if (tooltipPrefab != null)
        {
            startButtonTooltipRoot = PrefabUtility.InstantiatePrefab(tooltipPrefab, contentHostObj.transform) as GameObject;
            if (startButtonTooltipRoot != null)
            {
                startButtonTooltipRoot.name = "ControllerOnlyTooltip";
                var tooltipRt = startButtonTooltipRoot.GetComponent<RectTransform>();
                if (tooltipRt != null)
                {
                    var pos = startButtonTooltipRoot.AddComponent<TooltipPositioner>();
                    pos.tooltipRect = tooltipRt;
                    pos.anchorRect = buttonRect;
                    pos.boundsRect = contentHostRect;
                    pos.positioningRoot = contentHostRect;
                    pos.preferredPlacement = TooltipPositioner.Placement.Top;
                    pos.gap = 4f;
                    pos.offset = new Vector2(0f, -8f);
                    pos.keepWithinBounds = true;
                }

                startButtonTooltipText = startButtonTooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (startButtonTooltipText != null)
                    startButtonTooltipText.raycastTarget = false;

                startButtonTooltipRoot.SetActive(false);
            }
        }

        const string switchButtonPrefabPath = "Assets/Prefabs/UI/UnityUIButtonBased/BorderlessButton_IconAndLabel_UnityUIButton.prefab";
        GameObject switchButtonPrefab = LoadRequiredPrefab(switchButtonPrefabPath, "BorderlessButton_IconAndLabel_UnityUIButton");
        var switchObj = PrefabUtility.InstantiatePrefab(switchButtonPrefab, switchClusterObj.transform) as GameObject;
        if (switchObj == null)
            throw new System.InvalidOperationException("Failed to instantiate BorderlessButton_IconAndLabel_UnityUIButton.");
        switchObj.name = "ModeSwitchButton";
        switchObj.transform.SetParent(switchClusterObj.transform, false);
        switchRect = switchObj.GetComponent<RectTransform>();
        if (switchRect != null)
            switchRect.sizeDelta = new Vector2(100f, 100f);

        switchImage = switchObj.GetComponentInChildren<Image>(true);
        switchIconImage = FindRequiredUiSetIconImage(switchObj.transform);
        if (switchIconImage == null)
        {
            throw new System.InvalidOperationException(
                "Required UI Set icon with tag 'QDSUIIcon' was not found on switch button prefab.");
        }

        // Make this an icon-only affordance.
        var switchLabel = switchObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (switchLabel != null)
            switchLabel.text = string.Empty;

        var switchProgressObj = new GameObject("ModeSwitchDwellProgress", typeof(RectTransform), typeof(Image));
        switchProgressObj.transform.SetParent(switchIconImage.transform, false);
        var switchProgressRect = switchProgressObj.GetComponent<RectTransform>();
        switchProgressRect.anchorMin = Vector2.zero;
        switchProgressRect.anchorMax = Vector2.one;
        switchProgressRect.pivot = new Vector2(0.5f, 0.5f);
        switchProgressRect.anchoredPosition = Vector2.zero;
        switchProgressRect.sizeDelta = Vector2.zero;

        switchDwellProgressImage = switchProgressObj.GetComponent<Image>();
        switchDwellProgressImage.sprite = CreateCircleSprite(192);
        switchDwellProgressImage.type = Image.Type.Filled;
        switchDwellProgressImage.fillMethod = Image.FillMethod.Radial360;
        switchDwellProgressImage.fillOrigin = (int)Image.Origin360.Top;
        switchDwellProgressImage.fillClockwise = true;
        switchDwellProgressImage.fillAmount = 0f;
        switchDwellProgressImage.color = new Color(0.80f, 0.90f, 1f, 0.95f);
        switchDwellProgressImage.raycastTarget = false;

        GameObject switchTooltipPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip/Tooltip.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/tooltip.prefab");
        if (switchTooltipPrefab != null)
        {
            switchTooltipRoot = PrefabUtility.InstantiatePrefab(switchTooltipPrefab, contentHostObj.transform) as GameObject;
            if (switchTooltipRoot != null)
            {
                switchTooltipRoot.name = "ControllerOnlyTooltip";
                var tooltipRt = switchTooltipRoot.GetComponent<RectTransform>();
                if (tooltipRt != null)
                {
                    var pos = switchTooltipRoot.AddComponent<TooltipPositioner>();
                    pos.tooltipRect = tooltipRt;
                    pos.anchorRect = switchRect;
                    pos.boundsRect = contentHostRect;
                    pos.positioningRoot = contentHostRect;
                    pos.preferredPlacement = TooltipPositioner.Placement.Left;
                    pos.gap = 12f;
                    pos.offset = new Vector2(0f, 4f);
                    pos.keepWithinBounds = true;
                }

                switchTooltipText = switchTooltipRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (switchTooltipText != null)
                    switchTooltipText.raycastTarget = false;

                switchTooltipRoot.SetActive(false);
            }
        }

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

    private static Sprite CreateLockOnSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "LockOnIcon";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float rOuter = Mathf.Min(width, height) * 0.38f;
        float rInner = Mathf.Min(width, height) * 0.26f;
        float ringThickness = Mathf.Max(2f, Mathf.Min(width, height) * 0.045f);
        float crossThickness = Mathf.Max(2f, Mathf.Min(width, height) * 0.035f);
        float feather = 1.25f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float outerRing = 1f - Mathf.Clamp01(Mathf.Abs(d - rOuter) / (ringThickness + feather));
                float innerRing = 1f - Mathf.Clamp01(Mathf.Abs(d - rInner) / (ringThickness + feather));

                float vBar = 1f - Mathf.Clamp01((Mathf.Abs(dx) - crossThickness) / feather);
                float hBar = 1f - Mathf.Clamp01((Mathf.Abs(dy) - crossThickness) / feather);
                float bars = Mathf.Max(vBar, hBar);

                float alpha = Mathf.Clamp01(Mathf.Max(Mathf.Max(outerRing, innerRing), bars));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
