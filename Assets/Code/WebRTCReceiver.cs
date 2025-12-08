using UnityEngine;
using Unity.WebRTC;
using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

public class WebRTCReceiver : MonoBehaviour
{
    private RTCPeerConnection peerConnection;
    private VideoStreamTrack videoTrack;
    private Renderer videoRenderer;
    private string serverIP;
    private string appId;
    private ClientWebSocket ws;

    public void Init(string ip, string appId, Renderer renderer) {
        this.serverIP = ip;
        this.appId = appId;
        this.videoRenderer = renderer;

        StartCoroutine(SetupConnection());
    }

    private IEnumerator SetupConnection() {
        // 1️⃣ Create peer connection
        var config = new RTCConfiguration {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };
        peerConnection = new RTCPeerConnection(ref config);

        // 2️⃣ Handle incoming video tracks
        peerConnection.OnTrack = e => {
            if (e.Track is VideoStreamTrack track) {
                videoTrack = track;
                track.OnVideoReceived += tex => {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        if (videoRenderer != null)
                            videoRenderer.material.mainTexture = tex;
                    });
                };
            }
        };

        // 3️⃣ Connect to signaling server
        yield return StartCoroutine(ConnectToSignaling());
    }

    private IEnumerator ConnectToSignaling() {
        ws = new ClientWebSocket();
        var uri = new Uri($"ws://{serverIP}:5000/signaling");
        var connectTask = ws.ConnectAsync(uri, CancellationToken.None);
        while (!connectTask.IsCompleted) yield return null;

        UnityEngine.Debug.Log($"✅ Connected to signaling server for {appId}");

        // Send "ready" message
        string readyMsg = $"{{\"type\":\"ready\",\"appId\":\"{appId}\"}}";
        var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(readyMsg));
        var sendTask = ws.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        while (!sendTask.IsCompleted) yield return null;

        // Start listening for messages
        StartCoroutine(ReceiveSignalingMessages());
    }

    private IEnumerator ReceiveSignalingMessages() {
        var buffer = new byte[4096];

        while (ws.State == WebSocketState.Open) {
            var resultTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!resultTask.IsCompleted) yield return null;

            var result = resultTask.Result;
            string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
            HandleSignalingMessage(msg);

            yield return null;
        }
    }

    private void HandleSignalingMessage(string msg) {
        UnityEngine.Debug.Log($"📩 Received signaling: {msg}");
        var json = JsonUtility.FromJson<SignalMsg>(msg);

        if (json.type == "offer") {
            StartCoroutine(HandleOffer(json));
        } else if (json.type == "ice") {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit {
                candidate = json.candidate,
                sdpMid = json.sdpMid,
                sdpMLineIndex = json.sdpMLineIndex
            });
            peerConnection.AddIceCandidate(candidate);
        }
    }

    private IEnumerator HandleOffer(SignalMsg json) {
        // 1️⃣ Set remote description
        var desc = new RTCSessionDescription {
            type = RTCSdpType.Offer,
            sdp = json.sdp
        };
        var setRemoteOp = peerConnection.SetRemoteDescription(ref desc);
        yield return setRemoteOp;

        // 2️⃣ Create answer
        var createAnswerOp = peerConnection.CreateAnswer();
        yield return createAnswerOp;

        // 3️⃣ Copy Desc to a variable before using 'ref'
        var localDesc = createAnswerOp.Desc;
        var setLocalOp = peerConnection.SetLocalDescription(ref localDesc);
        yield return setLocalOp;

        // 4️⃣ Send answer back
        string answerMsg = $"{{\"type\":\"answer\",\"sdp\":\"{localDesc.sdp}\",\"appId\":\"{appId}\"}}";
        var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(answerMsg));
        var sendTask = ws.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        while (!sendTask.IsCompleted) yield return null;
    }


    private void OnDestroy() {
        videoTrack?.Dispose();
        peerConnection?.Close();
    }

    [Serializable]
    private class SignalMsg
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string appId;
    }
}
