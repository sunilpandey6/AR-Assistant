using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Unity_test : MonoBehaviour
{
    private string webhookUrl = "http://localhost:5678/webhook-test/unity-path";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCorutine(PostData());
    }


    
    private IEnumerator PostData()
    {
        // Create a simple JSON data to send to n8n

        var data = new { message = "Hello from Unity!", timestamp = System.DateTime.Now.ToString() };

        string jsonData = JsonUtility.ToJson(data);

 

        // Convert the JSON string to a byte array

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

 

        // Set up the UnityWebRequest

        UnityWebRequest request = new UnityWebRequest(webhookUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

 

        // Send the request and wait for a response

        yield return request.SendWebRequest();

 

        // Check for errors and log the response

        if (request.result == UnityWebRequest.Result.Success)

        {

            Debug.Log("Data sent successfully! Response: " + request.downloadHandler.text);

        }

        else

        {

            Debug.LogError("Error sending data to n8n: " + request.error);

        }
    }
}
