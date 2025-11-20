using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class LaunchResponsetry
{
    public bool success;
    public string appId;
    public string message;
}

public class TestWeb : MonoBehaviour
{
    [Header("Server")]
    public string serverIP = "130.235.213.192";
    public string appName = "Google Chrome";

    [Header("AppPanel References")]
    public AppPanel appPanel;        // Drag your prefab instance here

    void Start() {
        StartCoroutine(LaunchAppCoroutine());
    }

    IEnumerator LaunchAppCoroutine() {
        string launchUrl = $"http://{serverIP}:5000/launch?name={UnityWebRequest.EscapeURL(appName)}";

        Debug.Log($"Launching {appName} via {launchUrl}");
        UnityWebRequest request = UnityWebRequest.Get(launchUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            LaunchResponsetry response = JsonUtility.FromJson<LaunchResponsetry>(json);

            if (response.success) {
                Debug.Log($"Successfully launched {appName} with AppID: {response.appId}");

                if (appPanel != null) {
                    // Set the app ID
                    appPanel.appId = response.appId;

                    // Create a simple gray placeholder texture dynamically
                    Texture2D placeholder = new Texture2D(128, 128);
                    Color[] pixels = new Color[128 * 128];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.gray;
                    placeholder.SetPixels(pixels);
                    placeholder.Apply();

                    // This will assign the texture AND start WebRTC inside AppPanel
                    appPanel.SetAppInfo(appName, placeholder);
                } else {
                    Debug.LogError("AppPanel reference not set in TestWeb!");
                }
            } else {
                Debug.LogError($"Server failed to launch {appName}: {response.message}");
            }
        } else {
            Debug.LogError($"Failed to launch {appName}: {request.error}");
        }
    }


}
