using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using Oculus.Platform;


public class SpawnChar : MonoBehaviour
{
    [Header("Avatar - Character")]
    public GameObject avatarPrefab;
    public float spawnDis = 1.0f;

    [Header("OVR Camera")]
    public Transform cameraTransform;

    [Header("Chat Manager Reference")]
    public ChatBoxManager chatBoxManager;   // Reference to your chat UI manager

    [SerializeField] private string n8nUrl = "http://localhost:5678/webhook/unity-ai";

    private GameObject spawnChar;
    public float timeToLive = 60f;
    public bool interactionProgress = false;
    private float timer;


    void Update() {
        if (OVRInput.GetDown(OVRInput.Button.Start) || (Input.GetKey(KeyCode.LeftWindows) && Input.GetKeyDown(KeyCode.Space))) {
            if (spawnChar == null) {
                SpawnAvatar();
            }
        }

        if (spawnChar != null && (OVRInput.GetDown(OVRInput.Button.One) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space)))) {
            if (!interactionProgress) {
                StartInteraction();
            }
        }

        if (spawnChar != null && !interactionProgress) {
            timer += Time.deltaTime;
            if (timer >= timeToLive) {
                Destroy(spawnChar);
                spawnChar = null;
            }
        }
    }

    private void SpawnAvatar() {
        if (cameraTransform == null || avatarPrefab == null) return;

        Vector3 spawnPos = cameraTransform.position + cameraTransform.forward * spawnDis;
        spawnPos.y = cameraTransform.position.y;

        spawnChar = Instantiate(avatarPrefab, spawnPos, Quaternion.LookRotation(-cameraTransform.forward));
        timer = 0f;
    }

    public void StartInteraction() {
        interactionProgress = true;
        timer = 0f;

        // Enable chat box input
        if (chatBoxManager != null) {
            chatBoxManager.EnableChatInput(true);
        }
    }

    public void SendUserMessage(string message) {
        if (!interactionProgress) return;
        StartCoroutine(SendMessageToN8N(message));
    }

    IEnumerator SendMessageToN8N(string message) {

        var payload = new N8nRequest{
            sessionId = "1001",   // You can make this dynamic per player/session
            message = message
        };

        string jsonData = JsonUtility.ToJson(payload);

        using (var www = new UnityWebRequest(n8nUrl, "POST")) {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending to n8n: {jsonData}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) {
                string responseJson = www.downloadHandler.text;
                Debug.Log($"Received from n8n: {responseJson}");

                // Parse n8n’s JSON response (expects { "output": "..." })
                try {
                    N8nResponse response = JsonUtility.FromJson<N8nResponse>(responseJson);

                    if (chatBoxManager != null && response != null && !string.IsNullOrEmpty(response.output)) {
                        chatBoxManager.AddMessage(response.output, false);
                    } else {
                        // Fallback: show raw text if JSON doesn't parse cleanly
                        chatBoxManager.AddMessage(responseJson, false);
                    }
                } catch {
                    if (chatBoxManager != null)
                        chatBoxManager.AddMessage(responseJson, false);
                }
            } else {
                Debug.LogError($"N8N request failed: {www.error}");
                if (chatBoxManager != null)
                    chatBoxManager.AddMessage($"Error: {www.error}", false);
            }
        }
        interactionProgress = false;
        if (chatBoxManager != null) {
            chatBoxManager.EnableChatInput(false);
        }
        timer = 0f;
    }
}

[System.Serializable]
public class N8nRequest
{
    public string sessionId;
    public string message;
}


[System.Serializable]
public class N8nResponse
{
    public string output;
}
