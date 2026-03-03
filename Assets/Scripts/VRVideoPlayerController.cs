using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Controls 360 video playback. Hides the play screen and plays video when triggered.
/// Loads video from StreamingAssets (e.g. 360.mp4).
/// </summary>
public class VRVideoPlayerController : MonoBehaviour
{
    [Tooltip("Video filename in StreamingAssets folder (e.g. 360.mp4).")]
    public string videoFileName = "360.mp4";

    [Tooltip("GameObject to hide when video starts (e.g. Play screen).")]
    public GameObject playScreen;

    [Tooltip("VideoPlayer component. If null, will use the one on this GameObject.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Sphere/quad that displays the video. Must have a Renderer with a material.")]
    public Renderer videoRenderer;

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
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = path;
        videoPlayer.renderMode = VideoRenderMode.RenderTexturePreferred;

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
        if (_isPlaying)
            return;

        _isPlaying = true;

        if (playScreen != null)
            playScreen.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
            _renderTexture.Release();
    }
}
