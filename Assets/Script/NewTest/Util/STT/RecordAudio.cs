using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RecordAudio : MonoBehaviour
{
    [Header("Gemini Settings")]
    [Tooltip("API Key loaded securely via Keys.cs")]
    private string GEMINI_API_KEY;
    [SerializeField] private string modelName = "gemini-2.5-flash";

    private AudioClip recordedClip;
    private float startTime;
    private string tempFilePath;
    private string currentDevice;

    // --- JSON Response Classes ---
    [Serializable] 
    private class GeminiResponse { public Candidate[] candidates; }
    [Serializable]
    private class Candidate { public Content content; public string finishReason; }
    [Serializable]
    private class Content { public Part[] parts; }
    [Serializable]
    private class Part { public string text; }

    private void Awake() {
        tempFilePath = Path.Combine(Application.temporaryCachePath, "recording.wav");
        DebugLogger.Log("RecordAudio Awake: tempFilePath = " + tempFilePath);
    }

    private void Start() {
        GEMINI_API_KEY = Keys.LoadGemini();
        DebugLogger.Log("RecordAudio Start: GEMINI_API_KEY loaded? " + !string.IsNullOrEmpty(GEMINI_API_KEY));
    }

    public void StartRecording() {
        if (Microphone.devices.Length == 0) {
            DebugLogger.Log("StartRecording: No microphone found!");
            return;
        }

        currentDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(currentDevice, false, 60, 44100);
        startTime = Time.realtimeSinceStartup;

        DebugLogger.Log($"StartRecording: Recording started on {currentDevice}...");
    }

    public void StopRecording(Action<string> onComplete) {
        if (!Microphone.IsRecording(currentDevice)) {
            DebugLogger.Log("StopRecording: Not currently recording.");
            return;
        }

        Microphone.End(currentDevice);
        float recordingLength = Time.realtimeSinceStartup - startTime;
        DebugLogger.Log($"StopRecording: Recording stopped. Length = {recordingLength:F2} sec");

        // Trim audio to actual length
        recordedClip = TrimClip(recordedClip, recordingLength);
        DebugLogger.Log("StopRecording: Clip trimmed to length.");

        // Save to disk
        WavUtility.Save(tempFilePath, recordedClip);
        DebugLogger.Log("StopRecording: Clip saved to " + tempFilePath);

        // Send to Gemini
        StartCoroutine(HandleAfterRecording(onComplete));
    }

    private IEnumerator HandleAfterRecording(Action<string> onComplete) {
        DebugLogger.Log("HandleAfterRecording: Starting transcription...");
        yield return StartCoroutine(TranscribeAudio(tempFilePath, onComplete));

        // Cleanup temp file
        if (File.Exists(tempFilePath)) {
            File.Delete(tempFilePath);
            DebugLogger.Log("HandleAfterRecording: Temp file deleted.");
        } else {
            DebugLogger.Log("HandleAfterRecording: Temp file not found for deletion.");
        }


    }

    //change to private after working
    public IEnumerator TranscribeAudio(string filePath, Action<string> onComplete) {
        DebugLogger.Log("TranscribeAudio: Starting...");

        if (string.IsNullOrEmpty(GEMINI_API_KEY)) {
            DebugLogger.Log("TranscribeAudio: GEMINI_API_KEY is missing!");
            onComplete?.Invoke("Error: API Key missing.");
            yield break;
        }

        string base64Audio = WavUtility.GetAudioBase64(filePath);
        if (string.IsNullOrEmpty(base64Audio)) {
            DebugLogger.Log("TranscribeAudio: Failed to convert audio to Base64.");
            onComplete?.Invoke("Error: Audio conversion failed.");
            yield break;
        }
        DebugLogger.Log("TranscribeAudio: Audio converted to Base64.");

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={GEMINI_API_KEY}";

        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{
                            ""text"": ""Transcribe this audio. If there is no speech, reply with [Silence].""
                        }},
                        {{
                            ""inlineData"": {{
                                ""mimeType"": ""audio/wav"",
                                ""data"": ""{base64Audio}""
                            }}
                        }}
                    ]
                }}
            ]
        }}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            DebugLogger.Log("TranscribeAudio: Sending request to Gemini...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                DebugLogger.Log($"TranscribeAudio: Gemini Error: {request.error}\n{request.downloadHandler.text}");
                onComplete?.Invoke($"Error: {request.responseCode}");
            } else {
                DebugLogger.Log("TranscribeAudio: Request successful. Parsing response...");
                string responseText = request.downloadHandler.text;

                try {
                    GeminiResponse responseObj = JsonUtility.FromJson<GeminiResponse>(responseText);

                    if (responseObj?.candidates != null && responseObj.candidates.Length > 0) {
                        string text = responseObj.candidates[0].content.parts[0].text;
                        DebugLogger.Log("TranscribeAudio: Transcription received: " + text);
                        onComplete?.Invoke(text);
                    } else {
                        DebugLogger.Log("TranscribeAudio: No text detected in response.");
                        onComplete?.Invoke("[No text detected]");
                    }
                } catch (Exception e) {
                    DebugLogger.Log("TranscribeAudio: JSON Parse Error: " + e.Message);
                    onComplete?.Invoke("Error: JSON Parse Failed");
                }
            }
        }
    }

    private AudioClip TrimClip(AudioClip clip, float length) {
        if (length <= 0) return clip;
        int samples = (int)(clip.frequency * length);
        if (samples <= 0) samples = 1;

        float[] data = new float[samples];
        clip.GetData(data, 0);

        AudioClip trimmed = AudioClip.Create(clip.name, samples, clip.channels, clip.frequency, false);
        trimmed.SetData(data, 0);
        DebugLogger.Log($"TrimClip: Created new clip with {samples} samples.");
        return trimmed;
    }
}
