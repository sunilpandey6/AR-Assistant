using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using ReadyPlayerMe.Core;

public class GeminiTTS : MonoBehaviour
{
    [Header("Gemini Settings")]
    private string apiKey;
    [SerializeField] private string model = "gemini-2.5-flash-preview-tts";
    [SerializeField] private string voiceName = "Schedar";

    [Header("Avatar Components")]
    [SerializeField] private AudioSource voiceSource;

    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    private void Start() {
        apiKey = Keys.LoadGemini();
    }

    // --- JSON WRAPPERS ---
    // These classes map strictly to the Gemini API JSON structure
    [Serializable]
    private class GeminiRequest
    {
        public Content[] contents;
        public GenerationConfig generationConfig;
    }

    [Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [Serializable]
    private class Part
    {
        public string text;
    }

    [Serializable]
    private class GenerationConfig
    {
        public string[] responseModalities;
        public SpeechConfig speechConfig;
    }

    [Serializable]
    private class SpeechConfig
    {
        public VoiceConfig voiceConfig;
    }

    [Serializable]
    private class VoiceConfig
    {
        public PrebuiltVoiceConfig prebuiltVoiceConfig;
    }

    [Serializable]
    private class PrebuiltVoiceConfig
    {
        public string voiceName;
    }

    // --- RESPONSE WRAPPERS ---
    [Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [Serializable]
    private class Candidate
    {
        public ResponseContent content;
    }

    [Serializable]
    private class ResponseContent
    {
        public ResponsePart[] parts;
    }

    [Serializable]
    private class ResponsePart
    {
        public InlineData inlineData;
    }

    [Serializable]
    private class InlineData
    {
        public string data; // The Base64 encoded audio
    }


    public void Speak(string text) {
        StartCoroutine(RequestSpeech(text));
    }

    private IEnumerator RequestSpeech(string text) {
        string url = $"{ApiUrl}{model}:generateContent?key={apiKey}";

        // 1. Construct the complex JSON payload
        GeminiRequest requestBody = new GeminiRequest {
            contents = new Content[]
            {
                new Content { parts = new Part[] { new Part { text = text } } }
            },
            generationConfig = new GenerationConfig {
                responseModalities = new string[] { "AUDIO" },
                speechConfig = new SpeechConfig {
                    voiceConfig = new VoiceConfig {
                        prebuiltVoiceConfig = new PrebuiltVoiceConfig { voiceName = voiceName }
                    }
                }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer(); // We must download Text/JSON, not Audio directly
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.Log("Sending Gemini TTS Request...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                UnityEngine.Debug.LogError($"Gemini TTS Error: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }

            // 2. Parse the JSON response
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);

            if (response?.candidates == null || response.candidates.Length == 0) {
                UnityEngine.Debug.LogError("Gemini returned no candidates.");
                yield break;
            }

            // 3. Extract Base64 string
            string base64Audio = response.candidates[0].content.parts[0].inlineData.data;
            byte[] audioBytes = Convert.FromBase64String(base64Audio);

            // 4. Convert Raw PCM bytes to Unity AudioClip
            // Gemini sends 24kHz, 16-bit PCM, Mono
            AudioClip clip = CreateClipFromPCM(audioBytes);

            if (clip != null && voiceSource != null) {
                clip.name = "Gemini_Audio";
                voiceSource.clip = clip;
                voiceSource.Play();
                UnityEngine.Debug.Log("Playing Gemini Audio!");
            }
        }
    }

    // Helper: Converts Raw PCM 16-bit Data to Unity Float Data
    private AudioClip CreateClipFromPCM(byte[] pcmData) {
        int sampleCount = pcmData.Length / 2; // 2 bytes per sample (16-bit)
        float[] floatData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++) {
            // Convert 2 bytes to 1 short (Int16)
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            // Normalize to float range [-1.0f, 1.0f]
            floatData[i] = sample / 32768f;
        }

        // Create the clip: 24000Hz is the fixed rate from Gemini TTS
        AudioClip clip = AudioClip.Create("GeminiTTS", sampleCount, 1, 24000, false);
        clip.SetData(floatData, 0);
        return clip;
    }
}