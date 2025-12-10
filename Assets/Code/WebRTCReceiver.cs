using UnityEngine;
using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebRTCReceiver : MonoBehaviour
{
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationToken;
    private string serverIP;
    private string appId;
    private MeshRenderer videoSurface;
    private Texture2D videoTexture;

    private bool isReceivingFrames = false;

    // For H.264 decoding (you'll need a decoder library)
    // private H264Decoder decoder; // Add your decoder here

    public void Init(string serverIP, string appId, MeshRenderer surface) {
        this.serverIP = serverIP;
        this.appId = appId;
        this.videoSurface = surface;

        // Create initial texture
        videoTexture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        videoSurface.material.mainTexture = videoTexture;

        StartCoroutine(ConnectWebSocket());
    }

    private IEnumerator ConnectWebSocket() {
        webSocket = new ClientWebSocket();
        cancellationToken = new CancellationTokenSource();

        string wsUrl = $"ws://{serverIP}:5000";
        Debug.Log($"Connecting to WebSocket: {wsUrl}");

        Task connectTask = webSocket.ConnectAsync(new Uri(wsUrl), cancellationToken.Token);

        // Wait for connection
        while (!connectTask.IsCompleted) {
            yield return null;
        }

        if (webSocket.State == WebSocketState.Open) {
            Debug.Log("✅ WebSocket connected!");

            // Identify as Unity client
            SendMessage(new {
                role = "unity"
            });

            // Request helper to start capturing this app
            SendMessage(new {
                to = "helper",
                type = "start_capture",
                appId = appId,
                processId = int.Parse(appId) // Convert appId back to processId
            });

            // Start receiving messages
            StartCoroutine(ReceiveMessages());
        } else {
            Debug.LogError($"Failed to connect WebSocket: {webSocket.State}");
        }
    }

    private void SendMessage(object data) {
        if (webSocket == null || webSocket.State != WebSocketState.Open) {
            Debug.LogError("WebSocket not connected!");
            return;
        }

        string json = JsonUtility.ToJson(data);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        Task sendTask = webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken.Token
        );
    }

    private IEnumerator ReceiveMessages() {
        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer for frames

        while (webSocket.State == WebSocketState.Open) {
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer);
            Task<WebSocketReceiveResult> receiveTask = webSocket.ReceiveAsync(segment, cancellationToken.Token);

            // Wait for message
            while (!receiveTask.IsCompleted) {
                yield return null;
            }

            WebSocketReceiveResult result = receiveTask.Result;

            if (result.MessageType == WebSocketMessageType.Text) {
                // Handle text messages (control/signaling)
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleTextMessage(message);
            } else if (result.MessageType == WebSocketMessageType.Binary) {
                // Handle binary messages (video frames)
                HandleBinaryFrame(buffer, result.Count);
            } else if (result.MessageType == WebSocketMessageType.Close) {
                Debug.Log("WebSocket closed by server");
                break;
            }
        }
    }

    private void HandleTextMessage(string message) {
        Debug.Log($"Received text message: {message}");

        try {
            // Parse JSON message
            var data = JsonUtility.FromJson<WebSocketMessage>(message);

            switch (data.type) {
                case "ready":
                    Debug.Log("✅ Helper is ready to stream!");
                    isReceivingFrames = true;
                    break;

                case "frame":
                    Debug.Log($"Frame metadata received: {message}");
                    break;

                case "error":
                    Debug.LogError($"Helper error: {message}");
                    break;
            }
        } catch (Exception e) {
            Debug.LogError($"Failed to parse message: {e.Message}");
        }
    }

    private void HandleBinaryFrame(byte[] data, int length) {
        if (!isReceivingFrames) return;

        // This is where you'd decode H.264 NAL units
        // For now, just log that we received a frame
        Debug.Log($"Received binary frame: {length} bytes");

        // Example: If you have a decoder
        // decoder.DecodeFrame(data, length, (decodedFrame) => {
        //     videoTexture.LoadRawTextureData(decodedFrame);
        //     videoTexture.Apply();
        // });

        // For testing: Create a random colored frame
        // UpdateTestFrame();
    }

    // Test method to show the texture is updating
    private void UpdateTestFrame() {
        Color randomColor = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        Color[] pixels = new Color[videoTexture.width * videoTexture.height];
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = randomColor;
        }

        videoTexture.SetPixels(pixels);
        videoTexture.Apply();
    }

    public void StopCapture() {
        SendMessage(new {
            to = "helper",
            type = "stop",
            appId = appId
        });
    }

    private void OnDestroy() {
        StopCapture();

        if (webSocket != null && webSocket.State == WebSocketState.Open) {
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unity closing", CancellationToken.None);
        }

        cancellationToken?.Cancel();
        cancellationToken?.Dispose();
    }
}

// Helper classes for JSON parsing
[Serializable]
public class WebSocketMessage
{
    public string to;
    public string type;
    public string appId;
    public int processId;
}