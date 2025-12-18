using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using System.Collections;

public class AppPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage videoImage;
    [SerializeField] private TextMeshProUGUI appNameText;
    [SerializeField] private Button closeButton;

    public ActivePanelManager aPM;
    [HideInInspector] public string appId;

    private WebSocket ws;
    [SerializeField]private H264Decoder decoder;

    private Texture videoTexture;

    void Awake()
    {
        //decoder = GetComponent<H264Decoder>();
        //if (decoder == null)
        //    decoder = gameObject.AddComponent<H264Decoder>();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>
    /// Called when app is launched & stream is ready
    /// </summary>
    public void Initialize(string appName, string bundleId)
    {
        appId = bundleId;
        appNameText.text = appName;
        StartScreenShare();
    }

    public void StartScreenShare()
    {
        ws = new WebSocket($"ws://{TaskBarControl.serverIP}:5000/");

        ws.OnOpen += (sender, e) =>
        {
            DebugLogger.Log("WS connected for " + appId);

            ws.Send("{\"role\":\"unity\"}");

            ws.Send(JsonUtility.ToJson(new
            {
                to = "helper",
                type = "start_capture",
                bundleId = appId,
                preferredWidth = 1280,
                preferredHeight = 720
            }));
        };

        ws.OnMessage += OnMessage;

        ws.OnClose += (sender, e) =>
        {
            DebugLogger.Log("WS closed: " + e.Reason);
        };

        ws.OnError += (sender, e) =>
        {
            DebugLogger.Log("WS error: " + e.Message);
        };

        ws.ConnectAsync();
        StartCoroutine(SetVideoTexture());
    }


    IEnumerator SetVideoTexture()
    {
        yield return new WaitUntil(() => decoder.OutputTexture != null);
        videoImage.texture = decoder.OutputTexture;
    }

    private void OnCloseClicked()
    {
        DebugLogger.Log("Closing app panel: " + appId);

        StopScreenShare();
        aPM.TogglePanel(gameObject);
        Destroy(gameObject);
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            // Feed binary NAL units into decoder
            decoder.EnqueueNAL(e.RawData);
        }
        else if (e.IsText)
        {
            DebugLogger.Log("WS message (text): " + e.Data);
            // Optional: handle JSON text messages from helper
        }
    }


    public void StopScreenShare()
    {
        ws.Close();
    }

}
