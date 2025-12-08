using UnityEngine;

public static class Keys
{
    public static string LoadGemini() {
        TextAsset geminiAsset = Resources.Load<TextAsset>("Secure/gemini");
        if (geminiAsset == null) {
            Debug.LogError("Gemini API key not found in Resources/Secure/gemini.txt");
            return string.Empty;
        }
        return geminiAsset.text.Trim();
    }

    public static string LoadEleven() {
        TextAsset elevenAsset = Resources.Load<TextAsset>("Secure/elevenlabs");
        if (elevenAsset == null) {
            Debug.LogError("ElevenLabs API key not found in Resources/Secure/elevenlabs.txt");
            return string.Empty;
        }
        return elevenAsset.text.Trim();
    }
}
