using System;
using UnityEngine;

/// <summary>
/// Reflection bridge for NativeFilePicker plugin.
/// Keeps compile-time dependency optional.
/// </summary>
public static class NativeFilePickerBridge
{
    private const string LogTag = "FilePickerDebug";

    public static void PickVideoFile(Action<string> onPicked, Action onUnavailable)
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        Debug.Log($"{LogTag}: Calling NativeFilePicker.PickFile with allowed type 'video/*'.");
        try
        {
            NativeFilePicker.PickFile(path =>
            {
                Debug.Log($"{LogTag}: Native callback received path='{path ?? "<null>"}'");
                onPicked?.Invoke(string.IsNullOrWhiteSpace(path) ? null : path);
            }, "video/*");
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LogTag}: NativeFilePicker.PickFile threw exception: {ex}");
            onUnavailable?.Invoke();
        }
#else
        Debug.LogWarning($"{LogTag}: PickVideoFile called on unsupported platform.");
        onUnavailable?.Invoke();
#endif
    }
}
