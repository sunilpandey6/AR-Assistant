using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TaskBarHover : MonoBehaviour
{
    void Start()
    {
        // Find all buttons in children
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button btn in buttons)
        {
            // Try to find the hover UI child with tag "UI_Hover"
            Transform hoverUI = FindChildWithTag(btn.transform, "UI_Hover");

            if (hoverUI == null)
            {
                DebugLogger.Log($"Hover UI with tag 'UI_Hover' not found for button: {btn.name}");
                continue;
            }

            // Ensure hover UI is hidden at start
            hoverUI.gameObject.SetActive(false);

            // Add EventTrigger if not already present
            EventTrigger trigger = btn.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();

            // Pointer Enter → show hover UI
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, (eventData) => { hoverUI.gameObject.SetActive(true); });

            // Pointer Exit → hide hover UI
            AddEventTrigger(trigger, EventTriggerType.PointerExit, (eventData) => { hoverUI.gameObject.SetActive(false); });
        }
    }

    // Helper method to find a direct child by tag
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
                return child;
        }
        return null;
    }

    // Helper method to add an EventTrigger entry
    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
