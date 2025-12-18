using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class DebugLogger
{
    private static TMP_Text debugText;             // Reference to the in-game debug text
    private static List<string> logMessages = new List<string>();
    private static int maxMessages = 20;

    /// <summary>
    /// Initialize the logger with a TMP_Text reference.
    /// Call this from a MonoBehaviour on Start.
    /// </summary>
    public static void Initialize(TMP_Text textComponent, int maxLog = 20) {
        debugText = textComponent;
        maxMessages = maxLog;
        logMessages.Clear();
        Log("DebugLogger initialized.");
    }

    /// <summary>
    /// Adds a new message to the in-game log and console.
    /// </summary>
    public static void Log(string message) {
        Debug.Log(message); // still prints to console

        logMessages.Add(message);
        if (logMessages.Count > maxMessages)
            logMessages.RemoveAt(0);

        if (debugText != null)
            debugText.text = string.Join("\n", logMessages);
    }
}
