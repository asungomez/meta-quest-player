using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the "Save / Exit" file selection dialog.
/// The name field must be a <see cref="TMP_InputField"/> authored in the dialog prefab under <c>LibraryNameInput</c>
/// (e.g. UI Set <c>TextField</c>); setup code only assigns the reference.
/// </summary>
public class FileSelectionDialogController : MonoBehaviour
{
    private const string LogTag = "DialogDebug";
    private const int NestedPopupCanvasSortingOrder = 5000;

    public ControlModeManager modeManager;
    public GameObject dialogRoot;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public TMP_Text subtitleText;

    [Header("Name Input (prefab — TMP_InputField under LibraryNameInput)")]
    [Tooltip("Assign in prefab or let VR Video Player setup find the first TMP_InputField under LibraryNameInput.")]
    public TMP_InputField nameInput;
    [Tooltip("UI Set helper line under HelperText — enable the TextMeshPro component in the prefab so the hint shows; errors reuse this line.")]
    public TMP_Text nameHelperText;
    [Header("Audio Tracks")]
    [Tooltip("Metadata label inside AudioTrackRow. If left empty, the controller will try to find AudioTrackRow/MetadataText at runtime.")]
    public TMP_Text audioTrackMetadataText;

    private string currentPickedPath;
    private readonly List<Toggle> dialogToggles = new List<Toggle>();
    private bool nameHelperDefaultsCaptured;
    private string nameHelperDefaultText = "";
    private Color nameHelperDefaultColor = Color.white;

    public void ShowForSelectedFile(string pickedPath)
    {
        Debug.Log($"{LogTag}: ShowForSelectedFile called with path='{pickedPath ?? "<null>"}'");
        currentPickedPath = pickedPath;

        NormalizeDialogControlState();

        string fileName = string.IsNullOrWhiteSpace(pickedPath) ? "Selected file" : Path.GetFileName(pickedPath);
        if (titleText != null)
            titleText.text = fileName;

        if (bodyText != null)
            bodyText.text = BuildAudioTrackSummary(pickedPath);
        UpdatePrimaryAudioTrackRow(pickedPath);

        if (dialogRoot != null)
            dialogRoot.SetActive(true);

        EnsureNestedPopupCanvasesRenderAboveDialog();

        if (nameInput != null)
        {
            nameInput.text = string.IsNullOrWhiteSpace(pickedPath)
                ? ""
                : Path.GetFileNameWithoutExtension(pickedPath);
            ClearNameValidation();

            var libRoot = nameInput.transform.parent != null
                ? nameInput.transform.parent.GetComponent<RectTransform>()
                : null;
            if (libRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(libRoot);
            Canvas.ForceUpdateCanvases();
        }

        modeManager?.SetDialogOpen(true);
    }

    private void LateUpdate()
    {
        if (dialogRoot == null || !dialogRoot.activeInHierarchy)
            return;

        // Dropdown popups can toggle active state after the dialog opens, so keep their canvases elevated.
        EnsureNestedPopupCanvasesRenderAboveDialog();
    }

    private void EnsureNestedPopupCanvasesRenderAboveDialog()
    {
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return;

        var childCanvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < childCanvases.Length; i++)
        {
            var childCanvas = childCanvases[i];
            if (childCanvas == null || childCanvas == parentCanvas)
                continue;

            // Popup lists (like the LanguageSelector dropdown) need their own canvas above the dialog.
            childCanvas.overrideSorting = true;
            childCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            childCanvas.sortingOrder = NestedPopupCanvasSortingOrder;
            childCanvas.transform.SetAsLastSibling();
        }
    }

    private void NormalizeDialogControlState()
    {
        if (dialogRoot == null)
            return;

        dialogRoot.GetComponentsInChildren(true, dialogToggles);
        for (int i = 0; i < dialogToggles.Count; i++)
        {
            var t = dialogToggles[i];
            if (t == null)
                continue;
            t.SetIsOnWithoutNotify(false);
        }
    }

    public void HandleSave()
    {
        Debug.Log($"{LogTag}: HandleSave pressed.");

        string enteredName = nameInput != null ? nameInput.text : null;
        if (string.IsNullOrWhiteSpace(enteredName))
        {
            Debug.Log($"{LogTag}: Save blocked — name is empty.");
            ShowNameValidationError("Please enter a name for this video.");
            return;
        }

        Debug.Log($"{LogTag}: Saving with name='{enteredName}' path='{currentPickedPath}'");

        if (subtitleText != null && !string.IsNullOrWhiteSpace(currentPickedPath))
            subtitleText.text = $"Saved: {enteredName}";

        HideDialog();
    }

    public void HandleExit()
    {
        Debug.Log($"{LogTag}: HandleExit pressed.");
        HideDialog();
    }

    private void ShowNameValidationError(string message)
    {
        if (nameHelperText == null)
        {
            Debug.LogWarning($"{LogTag}: nameHelperText is not assigned — validation message not shown.");
            return;
        }

        CaptureNameHelperDefaultsOnce();
        nameHelperText.text = message;
        nameHelperText.color = new Color(1f, 0.4f, 0.4f, 1f);
    }

    private void ClearNameValidation()
    {
        if (nameHelperText == null)
            return;
        CaptureNameHelperDefaultsOnce();
        nameHelperText.text = nameHelperDefaultText;
        nameHelperText.color = nameHelperDefaultColor;
    }

    /// <summary>
    /// Remembers prefab hint copy/color so we can restore after showing a validation error.
    /// </summary>
    private void CaptureNameHelperDefaultsOnce()
    {
        if (nameHelperDefaultsCaptured || nameHelperText == null)
            return;
        nameHelperDefaultText = nameHelperText.text;
        nameHelperDefaultColor = nameHelperText.color;
        nameHelperDefaultsCaptured = true;
    }

    private void HideDialog()
    {
        Debug.Log($"{LogTag}: HideDialog.");
        if (dialogRoot != null)
            dialogRoot.SetActive(false);

        modeManager?.SetDialogOpen(false);
    }

    private string BuildAudioTrackSummary(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "No file selected.";

#if UNITY_ANDROID && !UNITY_EDITOR
        var summary = BuildAndroidAudioTrackSummary(path);
        if (!string.IsNullOrWhiteSpace(summary))
            return summary;
#endif

        return "Audio track details are unavailable on this platform.";
    }

    private void UpdatePrimaryAudioTrackRow(string path)
    {
        var target = ResolveAudioTrackMetadataText();
        if (target == null)
            return;

        target.text = BuildPrimaryAudioTrackLabel(path);
    }

    private TMP_Text ResolveAudioTrackMetadataText()
    {
        if (audioTrackMetadataText != null)
            return audioTrackMetadataText;

        Transform row = transform.Find("AudioTrackRow");
        if (row == null)
            return null;

        var metadata = row.Find("MetadataText");
        if (metadata != null)
            audioTrackMetadataText = metadata.GetComponent<TMP_Text>();

        if (audioTrackMetadataText == null)
            audioTrackMetadataText = row.GetComponentInChildren<TMP_Text>(true);

        return audioTrackMetadataText;
    }

    private string BuildPrimaryAudioTrackLabel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "No audio tracks detected.";

#if UNITY_ANDROID && !UNITY_EDITOR
        var tracks = GetAndroidAudioTracks(path);
        if (tracks.Count > 0)
            return BuildAudioTrackRowLabel(tracks[0]);
        return "No audio tracks detected.";
#else
        return "Audio track metadata unavailable on this platform.";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private readonly struct AudioTrackInfo
    {
        public AudioTrackInfo(int trackIndex, int audioOrdinal, string language, string title)
        {
            TrackIndex = trackIndex;
            AudioOrdinal = audioOrdinal;
            Language = language;
            Title = title;
        }

        public int TrackIndex { get; }
        public int AudioOrdinal { get; }
        public string Language { get; }
        public string Title { get; }
    }

    private string BuildAndroidAudioTrackSummary(string path)
    {
        var tracks = GetAndroidAudioTracks(path);
        if (tracks == null)
            return "Audio tracks: unavailable";

        var sb = new StringBuilder();
        sb.AppendLine($"Audio tracks: {tracks.Count}");
        if (tracks.Count == 0)
        {
            sb.Append("No audio tracks detected.");
        }
        else
        {
            for (int i = 0; i < tracks.Count; i++)
                sb.AppendLine($"- {BuildAudioTrackRowLabel(tracks[i])}");
        }

        return sb.ToString().TrimEnd();
    }

    private List<AudioTrackInfo> GetAndroidAudioTracks(string path)
    {
        AndroidJavaObject extractor = null;
        try
        {
            extractor = new AndroidJavaObject("android.media.MediaExtractor");
            extractor.Call("setDataSource", path);

            int totalTracks = extractor.Call<int>("getTrackCount");
            int audioCount = 0;
            var tracks = new List<AudioTrackInfo>();

            for (int i = 0; i < totalTracks; i++)
            {
                using (var format = extractor.Call<AndroidJavaObject>("getTrackFormat", i))
                {
                    if (format == null)
                        continue;

                    string mime = TryGetMediaFormatString(format, "mime");
                    if (string.IsNullOrWhiteSpace(mime) || !mime.StartsWith("audio/"))
                        continue;

                    audioCount++;
                    string lang = NormalizeLanguageCode(TryGetMediaFormatString(format, "language"));
                    string title = TryGetMediaFormatString(format, "title");
                    tracks.Add(new AudioTrackInfo(i, audioCount, lang, title));
                }
            }

            Debug.Log($"{LogTag}: Audio analysis complete. tracks={audioCount}");
            return tracks;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"{LogTag}: Failed to read audio track metadata. {ex.Message}");
            return null;
        }
        finally
        {
            if (extractor != null)
                extractor.Dispose();
        }
    }

    private static string BuildAudioTrackRowLabel(AudioTrackInfo track)
    {
        string label = !string.IsNullOrWhiteSpace(track.Title)
            ? track.Title.Trim()
            : $"Track {track.AudioOrdinal}";

        if (!string.IsNullOrWhiteSpace(track.Language))
            return $"{label} | detected: {track.Language.ToUpperInvariant()}";

        return label;
    }

    private static string NormalizeLanguageCode(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        language = language.Trim();
        if (string.Equals(language, "und", System.StringComparison.OrdinalIgnoreCase))
            return null;

        return language.Replace('_', '-');
    }

    private static string TryGetMediaFormatString(AndroidJavaObject format, string key)
    {
        if (format == null || string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            bool hasKey = format.Call<bool>("containsKey", key);
            if (!hasKey)
                return null;
            return format.Call<string>("getString", key);
        }
        catch
        {
            return null;
        }
    }

#endif
}
