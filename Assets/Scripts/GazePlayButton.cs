using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gaze-activatable "Play" button. When the user looks at this object for the dwell time, OnGazeActivated fires.
/// Uses a ray from the camera center (head gaze). Compatible with Main Camera and OVRCameraRig.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GazePlayButton : MonoBehaviour
{
    [Tooltip("Camera to cast gaze ray from. If null, uses Camera.main.")]
    public Camera gazeCamera;

    [Tooltip("How long to gaze before activation (seconds).")]
    [Range(0.5f, 3f)]
    public float dwellTime = 1.5f;

    [Tooltip("Max ray distance for gaze detection.")]
    public float maxRayDistance = 10f;

    [Tooltip("If true, can only activate once until ResetActivation is called.")]
    public bool oneShot = true;

    public UnityEvent OnGazeActivated;

    private float _gazeAccumulator;
    private bool _hasActivated;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null)
            _collider.isTrigger = false; // Raycasts need non-trigger colliders
    }

    private void Start()
    {
        if (gazeCamera == null)
        {
            gazeCamera = Camera.main;
#if UNITY_ANDROID || UNITY_EDITOR
            // On Quest, OVRCameraRig's center eye is typically the main camera
            if (gazeCamera == null)
            {
                var ovrRig = FindFirstObjectByType<OVRCameraRig>();
                if (ovrRig != null && ovrRig.centerEyeAnchor != null)
                    gazeCamera = ovrRig.centerEyeAnchor.GetComponent<Camera>();
            }
#endif
        }
    }

    private void Update()
    {
        if ((oneShot && _hasActivated) || gazeCamera == null)
            return;

        Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);

        if (_collider.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            _gazeAccumulator += Time.deltaTime;
            if (_gazeAccumulator >= dwellTime)
            {
                if (oneShot)
                    _hasActivated = true;
                OnGazeActivated?.Invoke();
                _gazeAccumulator = 0f;
            }
        }
        else
        {
            _gazeAccumulator = 0f;
        }
    }

    public void ResetActivation()
    {
        _hasActivated = false;
        _gazeAccumulator = 0f;
    }
}
