using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;



public class ChatBoxManager : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform contentArea;
    public GameObject userMessagePrefab;
    public GameObject assistantMessagePrefab;

    [SerializeField] private string n8nUrl = "http://localhost:5678/webhook/unity-ai";
    public AvatarAnimatorControl animatorControl;

    void Start() {
        sendButton.onClick.AddListener(OnSendMessageTxt);
    }

    void OnSendMessageTxt() {
        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        AddMessage(userMessage, true);
        inputField.text = "";

        var payload = new N8nRequest {
            sessionId = "1001",
            message = userMessage,
            messageType = "text",
            voice = TaskBarControl.isVoice
        };
        
        SendToN8N(payload);
        

    }

    public void AddMessage(string message, bool isUser) {
        GameObject prefab = isUser ? userMessagePrefab : assistantMessagePrefab;
        GameObject newMsg = Instantiate(prefab, contentArea);
        newMsg.GetComponentInChildren<TMP_Text>().text = message;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    public IEnumerator SendToN8N(N8nRequest payload) {
        animatorControl.PlayNod();
        yield return StartCoroutine(SendMessageToN8N(payload));
        
    }

    IEnumerator SendMessageToN8N(N8nRequest payload) {

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

                    if (response != null && response.role == "user" &&!string.IsNullOrEmpty(response.output)) {
                        AddMessage(response.output, true);
                    } 
                    else if (response != null && response.role == "agent" && !string.IsNullOrEmpty(response.output)) {
                        AddMessage(response.output, false); 
                    }
                    else {
                        // Fallback: show raw text if JSON doesn't parse cleanly
                        AddMessage(responseJson, false);
                    }
                } catch {
                        AddMessage(responseJson, false);
                }
            } else {
                Debug.LogError($"N8N request failed: {www.error}");
                    AddMessage($"Error: {www.error}", false);
            }
        }
    }
}

[System.Serializable]
public class N8nRequest
{
    public string sessionId;
    public string message;
    public string messageType;
    public bool voice;
}


[System.Serializable]
public class N8nResponse
{
    public string role;
    public string output;
}

