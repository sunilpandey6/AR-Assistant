using UnityEngine;
using TMPro;

public class DebugLoggerInitializer : MonoBehaviour
{
    [Header("Assign your TMP_Text here")]
    public TMP_Text debugText;

    void Awake() {
        if (debugText != null) {
            DebugLogger.Initialize(debugText); // Pass the TMP_Text to the static logger
        } else {
            Debug.LogError("DebugText not assigned in DebugLoggerInitializer!");
        }
    }
}
