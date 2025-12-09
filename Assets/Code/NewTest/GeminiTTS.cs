using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiTTS : MonoBehaviour
{
    [Header("Gemini Settings")]
    private string apiKey;
    [SerializeField] private string model = "gemini-2.5-flash-preview-tts";
    [SerializeField] private string voiceName = "Schedar";

    [Header("Avatar Components")]
    [SerializeField] private AudioSource voiceSource;

    private string apiUrl;

    private void Start() {
        apiKey = Keys.LoadGemini();
        apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        DebugLogger.Log("GeminiTTS Start: API Key loaded? " + !string.IsNullOrEmpty(apiKey));
    }

    public void Speak(string text) {
        DebugLogger.Log("GeminiTTS Speak(): " + text);
        StartCoroutine(RequestSpeech(text));
    }

    private IEnumerator RequestSpeech(string text) {
        DebugLogger.Log("GeminiTTS: Preparing TTS JSON...");

        // --- Manual JSON (Quest-safe, matches Gemini docs exactly) ---
        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{
                            ""text"": ""{EscapeJson(text)}""
                        }}
                    ]
                }}
            ],
            ""generationConfig"": {{
                ""responseModalities"": [""AUDIO""],
                ""speechConfig"": {{
                    ""voiceConfig"": {{
                        ""prebuiltVoiceConfig"": {{
                            ""voiceName"": ""{voiceName}""
                        }}
                    }}
                }}
            }}
        }}";
        // --------------------------------------------------------------

        DebugLogger.Log("GeminiTTS JSON:\n" + jsonBody);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new (apiUrl, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            DebugLogger.Log("GeminiTTS: Sending request...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                DebugLogger.Log($"GeminiTTS ERROR: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }

            DebugLogger.Log("GeminiTTS: Response received. Parsing...");

            string jsonResponse = request.downloadHandler.text;
            DebugLogger.Log("GeminiTTS RAW RESPONSE:\n" + jsonResponse);

            // Extract Base64 audio manually
            string base64Audio = ExtractBase64Audio(jsonResponse);

            if (string.IsNullOrEmpty(base64Audio)) {
                DebugLogger.Log("GeminiTTS: ERROR — No audio found in response.");
                yield break;
            }

            DebugLogger.Log("GeminiTTS: Base64 audio extracted.");

            byte[] audioBytes = Convert.FromBase64String(base64Audio);

            AudioClip clip = CreateClipFromPCM(audioBytes);

            if (clip != null && voiceSource != null) {
                voiceSource.clip = clip;
                voiceSource.Play();
                DebugLogger.Log("GeminiTTS: Audio playback started.");
            }
        }
    }

    // ---------------------------
    // Extracts inlineData.data manually
    // ---------------------------
    private string ExtractBase64Audio(string json) {
        const string key = "\"data\":\"";

        int start = json.IndexOf(key);
        if (start < 0) return null;

        start += key.Length;

        int end = json.IndexOf("\"", start);
        if (end < 0) return null;

        return json.Substring(start, end - start);
    }

    // ---------------------------
    // PCM 16-bit → AudioClip
    // ---------------------------
    private AudioClip CreateClipFromPCM(byte[] pcmData) {
        int sampleCount = pcmData.Length / 2; // 16-bit PCM
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++) {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            samples[i] = sample / 32768f;
        }

        AudioClip clip = AudioClip.Create(
            "GeminiTTS_Clip",
            sampleCount,
            1,
            24000,   // Gemini always sends 24kHz PCM
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    // Safe text escaping
    private string EscapeJson(string s) {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
