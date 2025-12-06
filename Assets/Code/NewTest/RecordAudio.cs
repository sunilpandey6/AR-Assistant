using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RecordAudio : MonoBehaviour
{
    [Header("Gemini Settings")]
    [Tooltip("Get an API key from https://aistudio.google.com/")]
    [SerializeField] private string GEMINI_API_KEY = "YOUR_API_KEY_HERE";
    [SerializeField] private string modelName = "gemini-1.5-flash";

    private AudioClip recordedClip;
    private float startTime;
    private float recordingLength;
    private string tempFilePath;
    private string currentDevice; // Store the device name

    // --- JSON Serialization Classes ---
    [Serializable]
    private class GeminiRequest
    {
        public Content[] contents;
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
        public InlineData inlineData;
    }

    [Serializable]
    private class InlineData
    {
        public string mimeType;
        public string data;
    }

    [Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
        public PromptFeedback promptFeedback; // Useful for debugging safety blocks
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
        public string finishReason;
    }

    [Serializable]
    private class PromptFeedback
    {
        public BlockReason blockReason;
    }

    [Serializable]
    private class BlockReason
    {
        public string message;
    }
    // ----------------------------------

    private void Awake() {
        tempFilePath = Path.Combine(Application.temporaryCachePath, "recording.wav");
    }

    public void StartRecording() {
        if (Microphone.devices.Length == 0) {
            Debug.LogError("No microphone found!");
            return;
        }

        currentDevice = Microphone.devices[0];
        int sampleRate = 44100;
        int lengthSec = 60;

        // Start recording
        recordedClip = Microphone.Start(currentDevice, false, lengthSec, sampleRate);
        startTime = Time.realtimeSinceStartup;

        Debug.Log($"Recording started on {currentDevice}...");
    }

    public void StopRecording(Action<string> onComplete) {
        if (!Microphone.IsRecording(currentDevice)) {
            Debug.LogWarning("Not recording.");
            return;
        }

        Microphone.End(currentDevice);
        recordingLength = Time.realtimeSinceStartup - startTime;

        // Trim the clip to actual length
        recordedClip = TrimClip(recordedClip, recordingLength);

        // Save using your WavUtility
        SaveTempRecording();

        // Send to API
        StartCoroutine(HandleAfterRecording(onComplete));
    }

    public void SendExistingAudioFile(string filePath, Action<string> onComplete) {
        if (!File.Exists(filePath)) {
            Debug.LogError("Audio file not found: " + filePath);
            onComplete?.Invoke("Error: File not found");
            return;
        }

        StartCoroutine(TranscribeAudio(filePath, onComplete));
    }

    private void SaveTempRecording() {
        if (recordedClip != null) {
            WavUtility.Save(tempFilePath, recordedClip);
            Debug.Log("Saved temp recording at " + tempFilePath);
        } else {
            Debug.LogError("No recording found to save.");
        }
    }

    private IEnumerator HandleAfterRecording(Action<string> onComplete) {
        yield return StartCoroutine(TranscribeAudio(tempFilePath, onComplete));

        // Cleanup
        if (File.Exists(tempFilePath)) {
            File.Delete(tempFilePath);
        }
    }

    private IEnumerator TranscribeAudio(string filePath, Action<string> onComplete) {
        // 0. Safety Check
        if (GEMINI_API_KEY.Contains("YOUR_API_KEY")) {
            Debug.LogError("Gemini API Key is missing!");
            onComplete?.Invoke("Error: API Key is missing in Inspector.");
            yield break;
        }

        // 1. Get Base64 string
        string base64Audio = WavUtility.GetAudioBase64(filePath);

        if (string.IsNullOrEmpty(base64Audio)) {
            Debug.LogError("Failed to convert audio to base64.");
            onComplete?.Invoke("Error: Audio conversion failed.");
            yield break;
        }

        // 2. Prepare JSON Payload
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={GEMINI_API_KEY}";

        var requestData = new GeminiRequest {
            contents = new Content[]
            {
                new Content
                {
                    parts = new Part[]
                    {
                        new Part { text = "Transcribe this audio. If there is no speech, reply with [Silence]." },
                        new Part
                        {
                            inlineData = new InlineData
                            {
                                mimeType = "audio/wav",
                                data = base64Audio
                            }
                        }
                    }
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        // 3. Send Web Request
        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending to Gemini...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Gemini API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                onComplete?.Invoke($"Error: API Request Failed ({request.responseCode})");
            } else {
                // 4. Parse Response
                string responseText = request.downloadHandler.text;
                Debug.Log("Raw Response: " + responseText); // Check Console for this!

                try {
                    GeminiResponse responseObj = JsonUtility.FromJson<GeminiResponse>(responseText);

                    if (responseObj != null && responseObj.candidates != null && responseObj.candidates.Length > 0) {
                        var candidate = responseObj.candidates[0];
                        if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0) {
                            string transcription = candidate.content.parts[0].text;
                            onComplete?.Invoke(transcription);
                        } else {
                            Debug.LogWarning($"Response Valid but empty parts. Finish Reason: {candidate.finishReason}");
                            onComplete?.Invoke("[No text detected]");
                        }
                    } else {
                        Debug.LogWarning("No candidates found in response. Possible safety block?");
                        onComplete?.Invoke("[Transcription Blocked or Empty]");
                    }
                } catch (Exception e) {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    onComplete?.Invoke("Error: JSON Parse Failed");
                }
            }
        }
    }

    private AudioClip TrimClip(AudioClip clip, float length) {
        if (length <= 0) return clip;

        int samples = (int)(clip.frequency * length);
        if (samples <= 0) samples = 1; // Prevent 0 size

        float[] data = new float[samples];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create(
            clip.name,
            samples,
            clip.channels,
            clip.frequency,
            false
        );

        trimmedClip.SetData(data, 0);
        return trimmedClip;
    }
}