using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkTester : MonoBehaviour
{
    void Start() {
        StartCoroutine(TestInternet());
        StartCoroutine(TestLocalServer());
    }

    IEnumerator TestInternet() {
        string testUrl = "https://google.com";
        DebugLogger.Log("Testing internet connection...");

        UnityWebRequest req = UnityWebRequest.Get(testUrl);
        req.timeout = 5;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            DebugLogger.Log("✔ Internet OK: Quest can access google.com");
        } else {
            DebugLogger.Log("❌ Internet FAILED: No internet or DNS blocked");
        }
    }

    IEnumerator TestLocalServer() {
        string localUrl = "http://192.168.0.193:5678/webhook/ping";
        DebugLogger.Log("Testing local server at: " + localUrl);

        UnityWebRequest req = UnityWebRequest.Get(localUrl);
        req.timeout = 5;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) {
            DebugLogger.Log("✔ Local OK: Quest can reach your Mac: " + req.downloadHandler.text);
        } else {
            DebugLogger.Log("❌ Local FAILED: " + req.error);
        }
    }
}
