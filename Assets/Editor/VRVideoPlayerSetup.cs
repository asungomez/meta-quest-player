using UnityEngine;
using UnityEditor;
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
        libraryScreen.transform.position = new Vector3(0, 1.6f, 2.5f);
        libraryScreen.transform.rotation = Quaternion.identity;

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(libraryScreen.transform, false);
        titleObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        TextMesh titleText = titleObj.AddComponent<TextMesh>();
        titleText.text = "Video Library";
        titleText.fontSize = 72;
        titleText.characterSize = 0.03f;
        titleText.anchor = TextAnchor.MiddleCenter;
        titleText.alignment = TextAlignment.Center;
        titleText.color = Color.white;

        // Status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(libraryScreen.transform, false);
        statusObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        TextMesh statusText = statusObj.AddComponent<TextMesh>();
        statusText.text = "Loading videos...";
        statusText.fontSize = 44;
        statusText.characterSize = 0.02f;
        statusText.anchor = TextAnchor.UpperCenter;
        statusText.alignment = TextAlignment.Center;
        statusText.color = new Color(0.85f, 0.9f, 1f);

        // Admin help text
        GameObject adminObj = new GameObject("AdminHelpText");
        adminObj.transform.SetParent(libraryScreen.transform, false);
        adminObj.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        TextMesh adminText = adminObj.AddComponent<TextMesh>();
        adminText.text = "Admin actions appear with controllers.";
        adminText.fontSize = 34;
        adminText.characterSize = 0.017f;
        adminText.anchor = TextAnchor.UpperCenter;
        adminText.alignment = TextAlignment.Center;
        adminText.color = new Color(0.7f, 0.85f, 1f);

        // Anchor where dynamic gaze buttons are created
        GameObject buttonAnchor = new GameObject("ButtonAnchor");
        buttonAnchor.transform.SetParent(libraryScreen.transform, false);
        buttonAnchor.transform.localPosition = new Vector3(0f, 0.3f, 0f);

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
