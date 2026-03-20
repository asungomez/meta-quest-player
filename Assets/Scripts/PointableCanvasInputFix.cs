using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Workaround for a known gap in Meta's PointableCanvasModule: it does not call
/// SendUpdateEventToSelectedObject(), which means TMP_InputField never receives
/// UpdateSelected events and the system keyboard never triggers.
///
/// Attach to the same GameObject that hosts EventSystem + PointableCanvasModule.
/// See: https://communityforums.atmeta.com/discussions/dev-unity/input-field-not-working-with-interaction-sdk/988824
/// </summary>
public class PointableCanvasInputFix : MonoBehaviour
{
    private EventSystem eventSystem;

    private void Start()
    {
        eventSystem = GetComponent<EventSystem>();
        if (eventSystem == null)
            eventSystem = EventSystem.current;
    }

    private void Update()
    {
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        ExecuteEvents.Execute(
            eventSystem.currentSelectedGameObject,
            new BaseEventData(eventSystem),
            ExecuteEvents.updateSelectedHandler);
    }
}
