//using System;
//using System.Collections;
//using System.Text;
//using UnityEngine;
//using WebSocketSharp;

//public class ScreenShareClient : MonoBehaviour
//{
//    public static ScreenShareClient Instance;

//    [Header("Refs")]
//    public H264Decoder decoder;
//    public AppPanel appPanel;
//    public string serverUrl;

//    private WebSocket ws;
//    private string pendingAppName;
//    private string pendingBundleId;



//    void Awake()
//    {
//        Instance = this;
//    }

//    public void Connect()
//    {
//        serverUrl = $"ws://{TaskBarControl.serverIP}:5000/";
//        ws = new WebSocket(serverUrl);

//        ws.OnOpen += (sender, e) =>
//        {
//            DebugLogger.Log("WS connected");
//            ws.Send("{\"role\":\"unity\"}");
//        };

//        ws.OnMessage += OnMessage;

//        ws.OnClose += (sender, e) =>
//        {
//            DebugLogger.Log("WS closed: " + e.Reason);
//        };

//        ws.OnError += (sender, e) =>
//        {
//            DebugLogger.Log("WS error: " + e.Message);
//        };

//        ws.ConnectAsync();
//    }


//    public void StartCapture(string appName, string bundleId)
//    {
//        pendingAppName = appName;
//        pendingBundleId = bundleId;

//        if (ws == null || !ws.IsAlive)
//            Connect();

//        var msg = new
//        {
//            to = "helper",
//            type = "start_capture",
//            bundleId = bundleId,
//            preferredWidth = 1280,
//            preferredHeight = 720
//        };

//        ws.Send(JsonUtility.ToJson(msg));
//    }

//    private void OnMessage(object sender, MessageEventArgs e)
//    {
//        if (e.IsText)
//            HandleText(e.Data);
//        else if (e.IsBinary)
//            decoder.EnqueueNAL(e.RawData);
//    }

//    private void HandleText(string json)
//    {
//        if (json.Contains("\"type\":\"ready\""))
//        {
//            appPanel.Initialize(
//                pendingAppName,
//                decoder.OutputTexture,
//                pendingBundleId
//            );
//        }
//    }

//    public void StopCapture()
//    {
//        ws.Send("{\"to\":\"helper\",\"type\":\"stop\"}");
//    }
//}
