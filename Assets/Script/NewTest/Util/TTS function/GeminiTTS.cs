using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiTTS : MonoBehaviour
{
    private string apiKey;
    [SerializeField] private string model = "gemini-2.5-flash-preview-tts";
    [SerializeField] private string voiceName = "Schedar";

    [SerializeField] private AudioSource voiceSource;

    private string apiUrl;

    private void Start() {
        DebugLogger.Log("Unity gemini tts loading");
        apiKey = Keys.LoadGemini();
        apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        DebugLogger.Log(apiKey == null ? "Unity gemini tts not loaded" : "Unity gemini tts loaded");

    }

    public void Speak(string text) {
        StartCoroutine(RequestSpeech(text));
    }

    private IEnumerator RequestSpeech(string text) {
        DebugLogger.Log("Request corutine start");
        string jsonBody = $@"
        {{
            ""model"": ""{model}"",
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
        DebugLogger.Log("Sending JSON body: " + jsonBody);  // Log JSON body

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        DebugLogger.Log("sending");
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError("GeminiTTS ERROR: " + request.error);
                DebugLogger.Log("GeminiTTS ERROR: " + request.error);
                
                yield break;
            }

            string json = request.downloadHandler.text;
            DebugLogger.Log("API Response: " + json);  // Log full response

            string base64Audio = ExtractBase64Audio(json);
            DebugLogger.Log("Base64 audio: " + base64Audio);  // Log base64 audio data

            if (string.IsNullOrEmpty(base64Audio)) {
                DebugLogger.Log("GeminiTTS: No audio found in response.");
                Debug.LogError("GeminiTTS: No audio found in response.");
                yield break;
            }

            byte[] pcmBytes = Convert.FromBase64String(base64Audio);

            AudioClip clip = CreateClipFromPCM(pcmBytes);

            if (clip && voiceSource) {
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }
    }

    private string ExtractBase64Audio(string json) {
        const string dataKey = "\"data\":\"";
        int start = json.IndexOf(dataKey);
        if (start < 0) return null;

        start += dataKey.Length;
        int end = json.IndexOf("\"", start);
        if (end < 0) return null;

        return json.Substring(start, end - start);
    }

    private AudioClip CreateClipFromPCM(byte[] pcmData) {
        int sampleCount = pcmData.Length / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++) {
            short s = BitConverter.ToInt16(pcmData, i * 2);
            samples[i] = s / 32768f;
        }

        AudioClip clip = AudioClip.Create(
            "GeminiTTS_Clip",
            sampleCount,
            1,
            24000,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    private string EscapeJson(string s) {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
