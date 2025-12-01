using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using ReadyPlayerMe.Core;

public class ElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs Settings")]
    [SerializeField] public string apiKey;
    [SerializeField] public string voiceId;
    public string modelId = "eleven_multilingual_v2";

    //[Header("Unity Components")]
    //[SerializeField] public AudioSource audioSource;

    [Header("Avatar Components")]
    [SerializeField] private ReadyPlayerMe.Core.VoiceHandler voiceHandler;


    private const string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech/";

    public void Speak(string text) {
        StartCoroutine(RequestSpeech(text));
    }

    private IEnumerator RequestSpeech(string text) {
        string url = $"{apiUrl}{voiceId}?output_format=mp3_44100_128";

        var body = new {
            text = text,
            model_id = modelId
        };

        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("xi-api-key", apiKey);
        request.SetRequestHeader("Content-Type", "application/json");

        UnityEngine.Debug.Log("Sending TTS request...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            UnityEngine.Debug.LogError("TTS Error: " + request.error);
            yield break;
        }

        byte[] audioData = request.downloadHandler.data;
        UnityEngine.Debug.Log("Received audio bytes: " + audioData.Length);

        string tempPath = Path.Combine(Application.temporaryCachePath, "temp.mp3");
        File.WriteAllBytes(tempPath, audioData);

        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file:///" + tempPath, AudioType.MPEG)) {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result != UnityWebRequest.Result.Success) {
                UnityEngine.Debug.LogError("Failed to load audio: " + audioRequest.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            //audioSource.clip = clip;
            //audioSource.Play();
            if (voiceHandler != null) {
                voiceHandler.AudioProvider = AudioProviderType.AudioClip;
                voiceHandler.PlayAudioClip(clip);
            }

            UnityEngine.Debug.Log("Playing speech!");
        }

        // Optionally delete the temporary file
        File.Delete(tempPath);
    }
}
