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

        // Welcome panel in front of the user.
        GameObject welcomeScreen = new GameObject("WelcomeScreen");
        welcomeScreen.transform.position = new Vector3(0f, 1.55f, 1.8f);
        welcomeScreen.transform.rotation = Quaternion.identity;

        BuildWelcomeCanvas(welcomeScreen.transform);

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

    private static void BuildWelcomeCanvas(Transform parent)
    {
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
        titleText.enableWordWrapping = false;
        titleText.color = Color.white;

        var subtitleObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleObj.transform.SetParent(panelObj.transform, false);
        var subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRt.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRt.pivot = new Vector2(0.5f, 0.5f);
        subtitleRt.anchoredPosition = new Vector2(0f, -140f);
        subtitleRt.sizeDelta = new Vector2(1200f, 120f);
        var subtitleText = subtitleObj.GetComponent<TextMeshProUGUI>();
        subtitleText.text = "Welcome to our VR experience";
        subtitleText.fontSize = 62f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.enableWordWrapping = false;
        subtitleText.color = new Color(0.90f, 0.95f, 1f, 0.95f);
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
}
