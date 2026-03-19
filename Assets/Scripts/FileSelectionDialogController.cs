using System.IO;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the "Save / Exit" file selection dialog.
/// First iteration: shows selected file name and handles button actions.
/// </summary>
public class FileSelectionDialogController : MonoBehaviour
{
    private const string LogTag = "DialogDebug";

    public ControlModeManager modeManager;
    public GameObject dialogRoot;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public TMP_Text subtitleText;

    private string currentPickedPath;
    private readonly List<Toggle> dialogToggles = new List<Toggle>();

    private void Awake()
    {
        // Startup visibility is controlled by setup script; avoid mutating it here because
        // this Awake can run on first show (when host was initially inactive).
        Debug.Log($"{LogTag}: Awake. dialogRoot={(dialogRoot != null ? dialogRoot.name : "<null>")} active={dialogRoot != null && dialogRoot.activeSelf}");
    }

    public void ShowForSelectedFile(string pickedPath)
    {
        Debug.Log($"{LogTag}: ShowForSelectedFile called with path='{pickedPath ?? "<null>"}'");
        currentPickedPath = pickedPath;

        NormalizeDialogControlState();

        string fileName = string.IsNullOrWhiteSpace(pickedPath) ? "Selected file" : Path.GetFileName(pickedPath);
        if (titleText != null)
            titleText.text = fileName;
        else
            Debug.LogWarning($"{LogTag}: titleText reference is null.");

        if (bodyText != null)
            bodyText.text = BuildAudioTrackSummary(pickedPath);
        else
            Debug.LogWarning($"{LogTag}: bodyText reference is null.");

        if (dialogRoot != null)
            dialogRoot.SetActive(true);
        else
            Debug.LogWarning($"{LogTag}: dialogRoot reference is null.");

        modeManager?.SetDialogOpen(true);
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
        if (subtitleText != null && !string.IsNullOrWhiteSpace(currentPickedPath))
            subtitleText.text = $"Selected: {Path.GetFileName(currentPickedPath)}";

        HideDialog();
    }

    public void HandleExit()
    {
        Debug.Log($"{LogTag}: HandleExit pressed.");
        HideDialog();
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
        var summary = GetAndroidAudioTrackSummary(path);
        if (!string.IsNullOrWhiteSpace(summary))
            return summary;
#endif

        return "Audio track details are unavailable on this platform.";
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private string GetAndroidAudioTrackSummary(string path)
    {
        AndroidJavaObject extractor = null;
        try
        {
            extractor = new AndroidJavaObject("android.media.MediaExtractor");
            extractor.Call("setDataSource", path);

            int totalTracks = extractor.Call<int>("getTrackCount");
            int audioCount = 0;
            var lines = new List<string>();

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
                    string lang = TryGetMediaFormatString(format, "language");
                    string title = TryGetMediaFormatString(format, "title");
                    string label = !string.IsNullOrWhiteSpace(title)
                        ? title
                        : (!string.IsNullOrWhiteSpace(lang) ? lang : mime.Replace("audio/", ""));

                    lines.Add($"- Track {audioCount}: {label}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Audio tracks: {audioCount}");
            if (audioCount == 0)
            {
                sb.Append("No audio tracks detected.");
            }
            else
            {
                for (int i = 0; i < lines.Count; i++)
                    sb.AppendLine(lines[i]);
            }

            Debug.Log($"{LogTag}: Audio analysis complete. tracks={audioCount}");
            return sb.ToString().TrimEnd();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"{LogTag}: Failed to read audio track metadata. {ex.Message}");
            return "Audio tracks: unavailable";
        }
        finally
        {
            if (extractor != null)
                extractor.Dispose();
        }
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
