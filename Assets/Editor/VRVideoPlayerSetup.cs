using UnityEngine;
using UnityEditor;
using UnityEngine.Video;

/// <summary>
/// Editor menu to set up the gaze-controlled 360 video player in the current scene.
/// </summary>
public static class VRVideoPlayerSetup
{
    [MenuItem("VR Video Player/Setup Scene")]
    public static void SetupScene()
    {
        // 1. Create Video Sphere (inverted for 360 inside-out view)
        GameObject videoRoot = new GameObject("VRVideoPlayer");
        VideoPlayer vp = videoRoot.AddComponent<VideoPlayer>();
        VRVideoPlayerController controller = videoRoot.AddComponent<VRVideoPlayerController>();

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VideoSphere";
        sphere.transform.SetParent(videoRoot.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = new Vector3(-1, 1, 1); // Inverted normals for 360

        // Create unlit material for video
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.name = "VideoMaterial";
        sphere.GetComponent<Renderer>().material = mat;

        controller.videoPlayer = vp;
        controller.videoRenderer = sphere.GetComponent<Renderer>();
        controller.videoFileName = "360.mp4";

        // 2. Create Play Screen with "Play" text
        GameObject playScreen = new GameObject("PlayScreen");
        playScreen.transform.position = new Vector3(0, 1.6f, 2.5f); // In front of user
        playScreen.transform.rotation = Quaternion.identity;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PlayButtonQuad";
        quad.transform.SetParent(playScreen.transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale = new Vector3(1.5f, 0.5f, 1f);
        quad.GetComponent<Renderer>().enabled = false; // Canvas provides visuals, collider handles raycast

        // Canvas for "Play" text
        GameObject canvasObj = new GameObject("PlayCanvas");
        canvasObj.transform.SetParent(playScreen.transform);
        canvasObj.transform.localPosition = Vector3.zero;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 120);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Dark background panel
        GameObject panelObj = new GameObject("Background");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var image = panelObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        GameObject textObj = new GameObject("PlayText");
        textObj.transform.SetParent(canvasObj.transform, false);

        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "Play";
        text.fontSize = 72;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Gaze button on the quad (collider for raycast)
        GazePlayButton gazeBtn = quad.AddComponent<GazePlayButton>();
        controller.playScreen = playScreen;
        gazeBtn.OnGazeActivated.AddListener(controller.PlayVideo);

        // Ensure camera reference
        gazeBtn.gazeCamera = Camera.main;

        Undo.RegisterCreatedObjectUndo(videoRoot, "Create VR Video Player");
        Undo.RegisterCreatedObjectUndo(playScreen, "Create Play Screen");
        Selection.activeGameObject = playScreen;
    }
}
