using UnityEngine;
using Meta.WitAi.TTS.UX;
using Meta.WitAi.TTS.Utilities;

public class VoiceTriggerManager : MonoBehaviour
{
    public static VoiceTriggerManager Instance;

    [Header("TTS Components")]
    [SerializeField] private TTSSpeaker speaker;
    public GameObject avatar;
    [Header("Default Settings")]
    [SerializeField] private string defaultVoiceId = "WIT$CAM";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Set default voice if none is selected
        if (string.IsNullOrEmpty(speaker.VoiceID))
            speaker.VoiceID = defaultVoiceId;
    }

    /// <summary>
    /// Call this to speak a message immediately
    /// </summary>
    public void SpeakMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (!avatar.activeSelf) avatar.SetActive(true);
        // You can choose async or queued here
        speaker.Speak(message);
    }

    /// <summary>
    /// Update voice when settings panel changes
    /// </summary>
    public void UpdateVoice(string newVoiceId)
    {
        if (!string.IsNullOrEmpty(newVoiceId))
            speaker.VoiceID = newVoiceId;
    }
}
