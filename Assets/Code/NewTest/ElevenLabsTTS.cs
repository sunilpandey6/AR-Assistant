using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ReadyPlayerMe.Core;

public class ElevenLabsTTS : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey;
    [SerializeField] private string voiceId = "JBFqnCBsd6RMkjVDRZzb"; // Default from your example
    [SerializeField] private string modelId = "eleven_multilingual_v2"; // Matches your curl

    [Header("Avatar Components")]
    [SerializeField] private AudioSource voiceSource;

    private const string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech/";

    // 1. Structure matches your curl JSON body exactly
    [System.Serializable]
    private class ElevenLabsRequest
    {
        public string text;
        public string model_id;
    }

    private void Start() {
        apiKey = Keys.LoadEleven();
    }
    public void Speak(string text) {
        StartCoroutine(RequestSpeech(text));
    }

    private IEnumerator RequestSpeech(string text) {
        // 2. URL matches your curl params
        string url = $"{apiUrl}{voiceId}?output_format=mp3_44100_128";

        // 3. Prepare JSON Body
        var requestData = new ElevenLabsRequest {
            text = text,
            model_id = modelId
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // 4. Construct Request (Manually to ensure raw JSON body)
        using (var request = new UnityWebRequest(url, "POST")) {
            // Attach Body
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // Attach Download Handler (Stream directly to AudioClip, skip disk saving)
            // Important: We force AudioType.MPEG because the URL lacks .mp3 extension
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

            // 5. Headers match your curl -H flags
            request.SetRequestHeader("xi-api-key", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.Log($"Sending TTS Request to: {url}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                UnityEngine.Debug.LogError($"TTS Error: {request.error}");
                // Note: If this fails, DownloadHandlerAudioClip might not contain the text error.
                // You usually check headers here, but typically 401/400 is the issue.
            } else {
                // 6. Get content directly from memory
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

                if (clip != null && voiceSource != null) {
                    // RPM expects the provider to be set to AudioClip
                    voiceSource.clip = clip;
                    voiceSource.Play();
                    UnityEngine.Debug.Log("TTS playing successfully.");
                } else {
                    UnityEngine.Debug.LogError("Downloaded data could not be converted to AudioClip.");
                }
            }
        }
    }
}