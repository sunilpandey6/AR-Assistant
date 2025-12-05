using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RecordAudio : MonoBehaviour
{
    [Header("Gemini Settings")]
    [SerializeField] private string GEMINI_API_KEY = "YOUR_API_KEY_HERE";
    [SerializeField] private string modelName = "gemini-1.5-flash";

    private AudioClip recordedClip;
    private float startTime;
    private float recordingLength;
    private string tempFilePath;

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
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
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

        string device = Microphone.devices[0];
        int sampleRate = 44100;
        int lengthSec = 60; // Increased default buffer

        // Start recording
        recordedClip = Microphone.Start(device, false, lengthSec, sampleRate);
        startTime = Time.realtimeSinceStartup;

        Debug.Log("Recording started...");
    }

    public void StopRecording(Action<string> onComplete) {
        if (!Microphone.IsRecording(null)) {
            Debug.LogWarning("Not recording.");
            return;
        }

        Microphone.End(null);
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
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(TranscribeAudio(filePath, onComplete));
    }

    private void SaveTempRecording() {
        if (recordedClip != null) {
            // Using your existing WavUtility class
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
            Debug.Log("Temp audio deleted.");
        }
    }

    // Renamed from CallTTS to TranscribeAudio for clarity
    private IEnumerator TranscribeAudio(string filePath, Action<string> onComplete) {
        // 1. Get Base64 string using your WavUtility
        string base64Audio = WavUtility.GetAudioBase64(filePath);

        if (string.IsNullOrEmpty(base64Audio)) {
            Debug.LogError("Failed to convert audio to base64.");
            onComplete?.Invoke(null);
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
                        new Part { text = "Transcribe this audio." },
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
                onComplete?.Invoke(null);
            } else {
                // 4. Parse Response
                string responseText = request.downloadHandler.text;
                Debug.Log("Gemini Response: " + responseText);

                try {
                    GeminiResponse responseObj = JsonUtility.FromJson<GeminiResponse>(responseText);
                    if (responseObj != null && responseObj.candidates != null && responseObj.candidates.Length > 0) {
                        string transcription = responseObj.candidates[0].content.parts[0].text;
                        onComplete?.Invoke(transcription);
                    } else {
                        Debug.LogWarning("No candidates found in response.");
                        onComplete?.Invoke(null);
                    }
                } catch (Exception e) {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    onComplete?.Invoke(null);
                }
            }
        }
    }

    private AudioClip TrimClip(AudioClip clip, float length) {
        int samples = (int)(clip.frequency * length);
        if (samples == 0) return clip;

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