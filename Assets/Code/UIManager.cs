using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

//using UnityEngine.XR.Interaction.Toolkit;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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

    [Header("VR App Panels")]
    public GameObject panelPrefab; // Prefab with Quad + AppPanel script + XRInteractable
    public Transform spawnRoot;
    private Dictionary<String,GameObject> activePanels = new Dictionary<String, GameObject>();

    public string serverIP;
    private int port = 5000;

    [Serializable]
    public class ServerMessage
    {
        public string type;
        public string appId;
    }
    private ClientWebSocket ws;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional if you want UIManager to persist
        } else {
            Destroy(gameObject);
        }
    }


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
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.text.Trim().ToLower() == "pong") {
            status.text = "Connection OK";
            yield return new WaitForSeconds(1f);
            connectionPanel.SetActive(false);
            appsButton.SetActive(true);
            yield return StartCoroutine(appListLoader.FetchAppList(serverIP));

            // connect WebSocket
            _ = ConnectWebSocket();
        } else {
            status.text = $"Connection failed: {req.error}";
        }
    }

    async Task ConnectWebSocket() {
        ws = new ClientWebSocket();
        Uri uri = new Uri($"ws://{serverIP}:{port}/signaling");
        await ws.ConnectAsync(uri, CancellationToken.None);
        Debug.Log("✅ Connected to WebSocket signaling server");

        _ = Task.Run(async () => {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open) {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleServerMessage(msg);
            }
        });
    }

    void HandleServerMessage(string msg) {
        Debug.Log($" Message from server: {msg}");
        try {
            var json = JsonUtility.FromJson<ServerMessage>(msg);
            if (json.type == "close" && json.appId != null) {
                UnityMainThreadDispatcher.Instance().Enqueue(() => CloseVRPanel(json.appId));
            }
        } catch (Exception e) {
            Debug.LogWarning($"Invalid server message: {msg} ({e.Message})");
        }
    }

    void ToggleAppPanel() {
        bool isActive = appListPanel.activeSelf;
        appListPanel.SetActive(!isActive);

        if (!isActive) { // Only reload when opening
            appListLoader.DisplayAppList(serverIP);
        }
    }

    #region VR Panels
    public void CreateVRPanel(string appId, string appName, Texture videoTexture) {
        if (activePanels.ContainsKey(appId)) {
            Debug.Log($"{appName} is already open.");
            return;
        }

        GameObject panel = Instantiate(panelPrefab, spawnRoot);
        panel.name = $"AppPanel_{appName}";
        panel.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.2f, 1.5f);
        panel.transform.localRotation = Quaternion.identity;

        // Setup AppPanel
        AppPanel appPanel = panel.GetComponent<AppPanel>();
        appPanel.appId = appId;
        appPanel.SetAppInfo(appName, videoTexture);
        appPanel.closeButton.onClick.AddListener(() => CloseVRPanel(appId));


        activePanels.Add(appId, panel);


        // Hide app from AppList
        appListLoader.DisplayAppList(serverIP);
    }

    public void CloseVRPanel(string appId) {
        if (!activePanels.ContainsKey(appId)) return;

        StartCoroutine(CloseAppOnServer(appId));
    }

    private IEnumerator CloseAppOnServer(string appId) {
        string url = $"http://{serverIP}:{port}/close?appId={UnityWebRequest.EscapeURL(appId)}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log($"App {appId} closed successfully.");

            // Destroy panel
            Destroy(activePanels[appId]);
            activePanels.Remove(appId);

            // Show in AppList again
            appListLoader.DisplayAppList(serverIP);
        } else {
            Debug.LogError($"Failed to close app {appId}: {request.error}");
        }
    }
    #endregion
}
