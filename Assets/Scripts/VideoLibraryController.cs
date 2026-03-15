using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles a simple video library for Quest:
/// - Regular users: gaze-select and play existing videos.
/// - Admin users (with controllers): import from inbox and delete selected videos.
/// </summary>
public class VideoLibraryController : MonoBehaviour
{
    [Header("References")]
    public VRVideoPlayerController videoPlayerController;
    public Transform buttonAnchor;
    public Camera gazeCamera;
    public TextMesh statusText;
    public TextMesh adminHelpText;

    [Header("Button Layout")]
    public float buttonWidth = 1.5f;
    public float buttonHeight = 0.2f;
    public float buttonSpacing = 0.24f;
    public int maxVisibleButtons = 8;

    [Header("Gaze")]
    [Range(0.5f, 3f)]
    public float dwellTime = 1.2f;
    public float gazeDistance = 10f;

    [Header("Admin")]
    [Tooltip("Name of folder under persistentDataPath where imported videos are stored.")]
    public string libraryFolderName = "videos";
    [Tooltip("Name of folder under persistentDataPath where admins can drop files to import.")]
    public string inboxFolderName = "inbox";

    private readonly List<string> _videoFiles = new();
    private readonly List<GameObject> _buttons = new();

    private int _selectedIndex;
    private float _nextNavigateTime;
    private float _nextDeviceRefreshTime;
    private bool _prevPrimaryButton;
    private bool _prevSecondaryButton;
    private bool _prevMenuButton;

    private InputDevice _leftController;
    private InputDevice _rightController;

    private string LibraryPath => Path.Combine(Application.persistentDataPath, libraryFolderName);
    private string InboxPath => Path.Combine(Application.persistentDataPath, inboxFolderName);

    private void Start()
    {
        if (gazeCamera == null)
            gazeCamera = Camera.main;

        EnsureFolders();
        RefreshLibrary();
        RefreshDevices();
        UpdateAdminHelpText();
    }

    private void Update()
    {
        if (Time.unscaledTime >= _nextDeviceRefreshTime)
        {
            RefreshDevices();
            _nextDeviceRefreshTime = Time.unscaledTime + 2f;
        }

        HandleAdminControllerInput();
    }

    public void RefreshLibrary()
    {
        _videoFiles.Clear();
        if (Directory.Exists(LibraryPath))
        {
            foreach (string path in Directory.GetFiles(LibraryPath))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".mp4" || ext == ".mov" || ext == ".mkv" || ext == ".webm")
                    _videoFiles.Add(path);
            }
        }

        _videoFiles.Sort();
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _videoFiles.Count - 1));
        RebuildButtons();
        UpdateStatus();
        UpdateAdminHelpText();
    }

    private void RebuildButtons()
    {
        foreach (GameObject button in _buttons)
        {
            if (button != null)
                Destroy(button);
        }
        _buttons.Clear();

        if (buttonAnchor == null)
            return;

        int count = Mathf.Min(_videoFiles.Count, maxVisibleButtons);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            string videoPath = _videoFiles[i];

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"VideoButton_{i}";
            quad.transform.SetParent(buttonAnchor, false);
            quad.transform.localPosition = new Vector3(0f, -i * buttonSpacing, 0f);
            quad.transform.localScale = new Vector3(buttonWidth, buttonHeight, 1f);

            var renderer = quad.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));

            var gazeButton = quad.AddComponent<GazePlayButton>();
            gazeButton.oneShot = false;
            gazeButton.gazeCamera = gazeCamera;
            gazeButton.dwellTime = dwellTime;
            gazeButton.maxRayDistance = gazeDistance;
            gazeButton.OnGazeActivated.AddListener(() =>
            {
                _selectedIndex = index;
                UpdateSelectionVisuals();
                PlaySelected();
            });

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(quad.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            var label = labelObj.AddComponent<TextMesh>();
            label.text = Path.GetFileName(videoPath);
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.02f;
            label.color = Color.white;

            _buttons.Add(quad);
        }

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var renderer = _buttons[i].GetComponent<Renderer>();
            if (renderer == null)
                continue;

            renderer.material.color = (i == _selectedIndex)
                ? new Color(0.2f, 0.45f, 0.95f, 0.95f)
                : new Color(0.08f, 0.1f, 0.16f, 0.9f);
        }
    }

    private void HandleAdminControllerInput()
    {
        if (!HasControllers())
            return;

        bool primary = GetButton(_rightController, CommonUsages.primaryButton);
        bool secondary = GetButton(_rightController, CommonUsages.secondaryButton);
        bool menu = GetButton(_leftController, CommonUsages.primaryButton);
        bool backToLibrary = GetButton(_leftController, CommonUsages.secondaryButton);

        Vector2 nav = GetAxis(_rightController, CommonUsages.primary2DAxis);
        if (Mathf.Abs(nav.y) > 0.6f && Time.unscaledTime >= _nextNavigateTime && _videoFiles.Count > 0)
        {
            _selectedIndex = Mathf.Clamp(_selectedIndex + (nav.y > 0 ? -1 : 1), 0, _videoFiles.Count - 1);
            _nextNavigateTime = Time.unscaledTime + 0.25f;
            UpdateSelectionVisuals();
            UpdateStatus();
        }

        if (primary && !_prevPrimaryButton)
            PlaySelected();

        if (secondary && !_prevSecondaryButton)
            DeleteSelectedVideo();

        if (menu && !_prevMenuButton)
            ImportFromInbox();

        if (backToLibrary && videoPlayerController != null)
            videoPlayerController.StopVideoAndShowLibrary();

        _prevPrimaryButton = primary;
        _prevSecondaryButton = secondary;
        _prevMenuButton = menu;
    }

    private void PlaySelected()
    {
        if (_videoFiles.Count == 0 || videoPlayerController == null)
            return;

        videoPlayerController.PlayVideoByPath(_videoFiles[_selectedIndex]);
        UpdateStatus($"Playing: {Path.GetFileName(_videoFiles[_selectedIndex])}");
    }

    private void DeleteSelectedVideo()
    {
        if (_videoFiles.Count == 0)
            return;

        string target = _videoFiles[_selectedIndex];
        try
        {
            File.Delete(target);
            UpdateStatus($"Deleted: {Path.GetFileName(target)}");
            RefreshLibrary();
        }
        catch (IOException ex)
        {
            UpdateStatus($"Delete failed: {ex.Message}");
        }
    }

    private void ImportFromInbox()
    {
        if (!Directory.Exists(InboxPath))
        {
            UpdateStatus("Inbox folder not found.");
            return;
        }

        string[] files = Directory.GetFiles(InboxPath);
        int imported = 0;
        foreach (string source in files)
        {
            string ext = Path.GetExtension(source).ToLowerInvariant();
            if (ext != ".mp4" && ext != ".mov" && ext != ".mkv" && ext != ".webm")
                continue;

            string destination = Path.Combine(LibraryPath, Path.GetFileName(source));
            if (File.Exists(destination))
                destination = Path.Combine(LibraryPath, $"{Path.GetFileNameWithoutExtension(source)}_{System.DateTime.Now:yyyyMMdd_HHmmss}{ext}");

            File.Move(source, destination);
            imported++;
        }

        UpdateStatus(imported > 0
            ? $"Imported {imported} file(s) from inbox."
            : "No supported video files in inbox.");

        RefreshLibrary();
    }

    private void EnsureFolders()
    {
        Directory.CreateDirectory(LibraryPath);
        Directory.CreateDirectory(InboxPath);
    }

    private void RefreshDevices()
    {
        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private bool HasControllers()
    {
        return _leftController.isValid || _rightController.isValid;
    }

    private static bool GetButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.isValid && device.TryGetFeatureValue(usage, out bool value) && value;
    }

    private static Vector2 GetAxis(InputDevice device, InputFeatureUsage<Vector2> usage)
    {
        if (device.isValid && device.TryGetFeatureValue(usage, out Vector2 axis))
            return axis;
        return Vector2.zero;
    }

    private void UpdateAdminHelpText()
    {
        if (adminHelpText == null)
            return;

        if (HasControllers())
        {
            adminHelpText.text =
                "Admin (controllers):\n" +
                "Right A: Play selected\n" +
                "Right B: Delete selected\n" +
                "Left X: Import from inbox\n" +
                "Left Y: Back to library";
        }
        else
        {
            adminHelpText.text =
                "Regular mode (gaze only):\n" +
                "Look at a video button until it activates.";
        }
    }

    private void UpdateStatus(string message = null)
    {
        if (statusText == null)
            return;

        if (!string.IsNullOrEmpty(message))
        {
            statusText.text = message;
            return;
        }

        if (_videoFiles.Count == 0)
        {
            statusText.text =
                "No videos found.\n" +
                $"Drop files into:\n{InboxPath}\n" +
                "Admin uses controller import.";
            return;
        }

        statusText.text = $"Videos: {_videoFiles.Count}\nSelected: {Path.GetFileName(_videoFiles[_selectedIndex])}";
    }
}
