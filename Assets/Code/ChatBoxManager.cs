using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;



public class ChatBoxManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform contentArea;
    public GameObject userMessagePrefab;
    public GameObject assistantMessagePrefab;

    [Header("SpawnChar Reference")]
    public SpawnChar spawnChar;  // Reference to the avatar spawner

    private bool chatEnabled = false;

  

    void Start() {
        sendButton.onClick.AddListener(OnSendMessage);
        EnableChatInput(false); // disable at start
    }

    public void EnableChatInput(bool enable) {
        chatEnabled = enable;
        inputField.interactable = enable;
        sendButton.interactable = enable;
    }

    void OnSendMessage() {
        if (!chatEnabled) return;

        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        AddMessage(userMessage, true);
        inputField.text = "";

        // Send the message to SpawnChar for N8N
        if (spawnChar != null) {
            spawnChar.SendUserMessage(userMessage);
        }
    }

    public void AddMessage(string message, bool isUser) {
        GameObject prefab = isUser ? userMessagePrefab : assistantMessagePrefab;
        GameObject newMsg = Instantiate(prefab, contentArea);
        newMsg.GetComponentInChildren<TMP_Text>().text = message;

        Canvas.ForceUpdateCanvases();
        contentArea.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
    }
}
