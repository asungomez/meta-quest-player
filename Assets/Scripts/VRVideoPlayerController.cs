using UnityEngine;
using UnityEngine.Video;
using System.IO;

/// <summary>
/// Controls 360 video playback. Hides the play screen and plays video when triggered.
/// Loads video from StreamingAssets (e.g. 360.mp4).
/// </summary>
public class VRVideoPlayerController : MonoBehaviour
{
    [Tooltip("GameObject to hide when video starts (e.g. Play screen).")]
    public GameObject playScreen;

    [Tooltip("VideoPlayer component. If null, will use the one on this GameObject.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Sphere/quad that displays the video. Must have a Renderer with a material.")]
    public Renderer videoRenderer;

    [Tooltip("Screen shown while choosing a video.")]
    public GameObject libraryScreen;

    private RenderTexture _renderTexture;
    private bool _isPlaying;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("VRVideoPlayerController: No VideoPlayer found.");
            return;
        }

        if (videoRenderer == null)
            videoRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        SetupVideoPlayer();
    }

    private void SetupVideoPlayer()
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Create RenderTexture for video output
        _renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = _renderTexture;

        if (videoRenderer != null && videoRenderer.material != null)
        {
            videoRenderer.material.mainTexture = _renderTexture;
        }
        else
        {
            Debug.LogWarning("VRVideoPlayerController: No Renderer assigned. Video will not display.");
        }

        videoPlayer.Prepare();
        videoPlayer.playOnAwake = false;
    }

    /// <summary>
    /// Call this when the user activates Play via gaze.
    /// </summary>
    public void PlayVideo()
    {
        Debug.LogWarning("VRVideoPlayerController: PlayVideo() called without a selected library video. Ignoring.");
    }

    public void PlayVideoByPath(string pathOrFileName)
    {
        if (_isPlaying || videoPlayer == null || string.IsNullOrWhiteSpace(pathOrFileName))
            return;

        _isPlaying = true;
        string resolvedPath = ResolveVideoPath(pathOrFileName);
        if (string.IsNullOrEmpty(resolvedPath))
        {
            _isPlaying = false;
            Debug.LogWarning("VRVideoPlayerController: Selected video path is invalid or missing.");
            return;
        }
        videoPlayer.url = resolvedPath;

        if (playScreen != null)
            playScreen.SetActive(false);

        if (libraryScreen != null)
            libraryScreen.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    public void StopVideoAndShowLibrary()
    {
        _isPlaying = false;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (libraryScreen != null)
            libraryScreen.SetActive(true);

        if (playScreen != null)
            playScreen.SetActive(false);
    }

    private string ResolveVideoPath(string pathOrFileName)
    {
        if (string.IsNullOrWhiteSpace(pathOrFileName))
            return null;

        if (Path.IsPathRooted(pathOrFileName))
        {
            if (!File.Exists(pathOrFileName))
                return null;
            return pathOrFileName;
        }

        string localPath = Path.Combine(Application.persistentDataPath, "videos", pathOrFileName);
        if (File.Exists(localPath))
            return localPath;

        return null;
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
            _renderTexture.Release();
    }
}
