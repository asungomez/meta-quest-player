using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Video;

/// <summary>
/// Editor menu to set up the VR video library player scene in the current scene.
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
        VideoLibraryController libraryController = videoRoot.AddComponent<VideoLibraryController>();
        videoRoot.AddComponent<QuestPassthroughController>();

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

        // 2. Create library screen shown at startup
        GameObject libraryScreen = new GameObject("LibraryScreen");
        libraryScreen.transform.position = new Vector3(0, 1.55f, 1.8f);
        libraryScreen.transform.rotation = Quaternion.identity;

        // Dark contrast panel behind the UI for readability.
        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "LibraryBackdrop";
        backdrop.transform.SetParent(libraryScreen.transform, false);
        backdrop.transform.localPosition = new Vector3(0f, -0.15f, 0.45f);
        backdrop.transform.localScale = new Vector3(3.2f, 2.4f, 1f);
        var backdropRenderer = backdrop.GetComponent<Renderer>();
        backdropRenderer.material = new Material(Shader.Find("Unlit/Color"));
        backdropRenderer.material.color = new Color(0.03f, 0.04f, 0.07f, 1f);

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(libraryScreen.transform, false);
        titleObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        titleObj.transform.localScale = Vector3.one * 0.01f;
        TextMeshPro titleText = titleObj.AddComponent<TextMeshPro>();
        titleText.text = "Video Library";
        titleText.fontSize = 90f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = Color.white;

        // Status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(libraryScreen.transform, false);
        statusObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        statusObj.transform.localScale = Vector3.one * 0.01f;
        TextMeshPro statusText = statusObj.AddComponent<TextMeshPro>();
        statusText.text = "Loading videos...";
        statusText.fontSize = 54f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        statusText.overflowMode = TextOverflowModes.Overflow;
        statusText.color = new Color(0.85f, 0.9f, 1f);

        // Admin help text
        GameObject adminObj = new GameObject("AdminHelpText");
        adminObj.transform.SetParent(libraryScreen.transform, false);
        adminObj.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        adminObj.transform.localScale = Vector3.one * 0.01f;
        TextMeshPro adminText = adminObj.AddComponent<TextMeshPro>();
        adminText.text = "Admin actions appear with controllers.";
        adminText.fontSize = 42f;
        adminText.alignment = TextAlignmentOptions.Center;
        adminText.textWrappingMode = TextWrappingModes.NoWrap;
        adminText.overflowMode = TextOverflowModes.Overflow;
        adminText.color = new Color(0.7f, 0.85f, 1f);

        // Anchor where dynamic gaze buttons are created
        GameObject buttonAnchor = new GameObject("ButtonAnchor");
        buttonAnchor.transform.SetParent(libraryScreen.transform, false);
        buttonAnchor.transform.localPosition = new Vector3(0f, -0.05f, 0f);

        // Optional legacy play screen remains unused
        GameObject playScreen = new GameObject("PlayScreen");
        playScreen.transform.position = new Vector3(0, 1.6f, 2.5f);
        playScreen.SetActive(false);

        controller.playScreen = playScreen;
        controller.libraryScreen = libraryScreen;

        libraryController.videoPlayerController = controller;
        libraryController.buttonAnchor = buttonAnchor.transform;
        libraryController.statusText = statusText;
        libraryController.adminHelpText = adminText;
        libraryController.gazeCamera = Camera.main;

        Undo.RegisterCreatedObjectUndo(videoRoot, "Create VR Video Player");
        Undo.RegisterCreatedObjectUndo(libraryScreen, "Create Library Screen");
        Undo.RegisterCreatedObjectUndo(playScreen, "Create Legacy Play Screen");
        Selection.activeGameObject = libraryScreen;
    }
}
