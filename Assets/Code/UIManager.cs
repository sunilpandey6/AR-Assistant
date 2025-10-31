using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Connection Panel")]
    public GameObject connectionPanel;
    public TMP_InputField IPInput;
    public Button submitButton;
    public TextMeshProUGUI status;

    [Header("Task Bar")]
    public GameObject appsButton;

    [Header("Clock")]
    public TextMeshProUGUI timeText;

    [Header("App Loader")]
    public GetAppList appListLoader; // Drag your GetAppList script here in the Inspector
    public GameObject appListPanel;
    public Button CloseAppList;

    private string serverIP;
    private int port = 5000;

    void Start() {
        connectionPanel.SetActive(true);
        appsButton.SetActive(false);
        appListPanel.SetActive(false);

        submitButton.onClick.AddListener(OnConnectClick);
        appsButton.GetComponent<Button>().onClick.AddListener(ToggleAppPanel);
        CloseAppList.GetComponent<Button>().onClick.AddListener(ToggleAppPanel);
    }

    void Update() {
        timeText.text = DateTime.Now.ToString("HH:mm:ss / yyyy-MM-dd");
        
    }

    void OnConnectClick() {
        serverIP = IPInput.text.Trim();

        if (string.IsNullOrEmpty(serverIP)) {
            status.text = "Please enter a valid IP address.";
            return;
        }

        StartCoroutine(ConnectToServer());
    }

    IEnumerator ConnectToServer() {
        string url = $"http://{serverIP}:{port}/ping";
        status.text = $"Connecting to {url}...";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
            status.text = $"Connection error: {request.error}";
        } else {
            string response = request.downloadHandler.text.Trim().ToLower();
            if (response == "pong") {
                status.text = "Connection OK";
                yield return new WaitForSeconds(2f);

                connectionPanel.SetActive(false);
                appsButton.SetActive(true);

                //fetch the app list
                yield return StartCoroutine(appListLoader.FetchAppList(serverIP));
            } else {
                status.text = $"Unexpected response: {response}";
            }
        }
    }

    void ToggleAppPanel() {
        bool isActive = appListPanel.activeSelf;
        appListPanel.SetActive(!isActive);

        if (!isActive) { // Only reload when opening
            appListLoader.DisplayAppList(serverIP);
        }
    }

}
