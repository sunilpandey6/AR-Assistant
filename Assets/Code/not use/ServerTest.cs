using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerConnector : MonoBehaviour
{
    // Replace with your actual IP address
    private string serverIP = "130.235.213.192";
    private int port = 5000;

    void Start()
    {
        StartCoroutine(ConnectToServer());
    }

    IEnumerator ConnectToServer()
    {
        string url = $"http://{serverIP}:{port}/ping";
        Debug.Log($"Connecting to {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Connection error: {request.error}");
        }
        else
        {
            Debug.Log($"Server response: {request.downloadHandler.text}");
        }
    }
}