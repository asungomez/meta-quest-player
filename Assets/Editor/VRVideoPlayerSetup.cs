using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

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
        welcomeScreen.transform.position = new Vector3(0f, 1.55f, 2.4f);
        welcomeScreen.transform.rotation = Quaternion.identity;

        BuildWelcomeCanvas(
            welcomeScreen.transform,
            out RectTransform buttonRect,
            out Image buttonImage,
            out TextMeshProUGUI subtitleText,
            out Image progressImage,
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
        startInteractor.targetImage = buttonImage;
        startInteractor.progressImage = progressImage;
        startInteractor.statusText = subtitleText;
        startInteractor.dwellSeconds = 1.0f;

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

    private static Transform BuildWelcomeCanvas(
        Transform parent,
        out RectTransform buttonRect,
        out Image buttonImage,
        out TextMeshProUGUI subtitleText,
        out Image progressImage,
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

        var canvasObj = new GameObject("WelcomeCanvas");
        canvasObj.transform.SetParent(parent, false);
        canvasObj.transform.localPosition = new Vector3(0f, -0.15f, 0.45f);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.0018f;

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        canvasObj.AddComponent<GraphicRaycaster>();

        var canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(1600f, 1080f);

        var panelObj = new GameObject("WelcomeBackdrop", typeof(RectTransform), typeof(Image), typeof(Outline));
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(1480f, 960f);

        var rounded = CreateRoundedSprite(512, 512, 24);
        var panelImage = panelObj.GetComponent<Image>();
        panelImage.sprite = rounded;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.004f, 0.005f, 0.008f, 1f);

        var outline = panelObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.80f, 0.87f, 0.96f, 0.26f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 170f);
        titleRt.sizeDelta = new Vector2(1200f, 160f);
        var titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "Hello world";
        titleText.fontSize = 110f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.color = Color.white;

        var subtitleObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleObj.transform.SetParent(panelObj.transform, false);
        var subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRt.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRt.pivot = new Vector2(0.5f, 0.5f);
        subtitleRt.anchoredPosition = new Vector2(0f, -140f);
        subtitleRt.sizeDelta = new Vector2(1200f, 120f);
        subtitleText = subtitleObj.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Click the start button";
        subtitleText.fontSize = 62f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.color = new Color(0.90f, 0.95f, 1f, 0.95f);

        var buttonObj = new GameObject("PrimaryButton", typeof(RectTransform), typeof(Image), typeof(Outline));
        buttonObj.transform.SetParent(panelObj.transform, false);
        buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -300f);
        buttonRect.sizeDelta = new Vector2(560f, 130f);

        buttonImage = buttonObj.GetComponent<Image>();
        buttonImage.sprite = rounded;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.16f, 0.43f, 0.96f, 1f);

        var buttonOutline = buttonObj.GetComponent<Outline>();
        buttonOutline.effectColor = new Color(0.72f, 0.86f, 1f, 0.32f);
        buttonOutline.effectDistance = new Vector2(2f, -2f);
        buttonOutline.useGraphicAlpha = true;

        var buttonTextObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        var buttonText = buttonTextObj.GetComponent<TextMeshProUGUI>();
        buttonText.text = "Start";
        buttonText.fontSize = 56f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.textWrappingMode = TextWrappingModes.NoWrap;
        buttonText.color = Color.white;

        var progressObj = new GameObject("DwellProgress", typeof(RectTransform), typeof(Image));
        progressObj.transform.SetParent(buttonObj.transform, false);
        var progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = new Vector2(215f, 0f);
        progressRect.sizeDelta = new Vector2(76f, 76f);

        progressImage = progressObj.GetComponent<Image>();
        progressImage.sprite = CreateCircleSprite(192);
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
        progressImage.fillAmount = 0f;
        progressImage.color = new Color(0.84f, 0.93f, 1f, 0.9f);

        var switchObj = new GameObject("ModeSwitchButton", typeof(RectTransform), typeof(Image), typeof(Outline));
        switchObj.transform.SetParent(panelObj.transform, false);
        switchRect = switchObj.GetComponent<RectTransform>();
        switchRect.anchorMin = new Vector2(1f, 1f);
        switchRect.anchorMax = new Vector2(1f, 1f);
        switchRect.pivot = new Vector2(1f, 1f);
        switchRect.anchoredPosition = new Vector2(-24f, -20f);
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
        switchProgressObj.transform.SetParent(panelObj.transform, false);
        var switchProgressRect = switchProgressObj.GetComponent<RectTransform>();
        switchProgressRect.anchorMin = new Vector2(1f, 1f);
        switchProgressRect.anchorMax = new Vector2(1f, 1f);
        switchProgressRect.pivot = new Vector2(1f, 1f);
        switchProgressRect.anchoredPosition = new Vector2(-138f, -22f); // left of icon button (outside)
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
        switchDialogRoot.transform.SetParent(panelObj.transform, false);
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
