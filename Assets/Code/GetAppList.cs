using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEditor.Experimental.GraphView;

[System.Serializable]
public class AppData
{
    public string name;
    public Sprite icon;
}
//just store name of the app in the list better than storing all the details, server will handle all this details.

[System.Serializable]
public class AppListResponse
{
    public List<AppData> apps;
}

[System.Serializable]
public class LaunchResponse
{
    public bool success;
    public string message;
    public string appId;
}

public class GetAppList : MonoBehaviour
{
    public TaskBarControl taskBarControl;
    public Transform appContainer; // Parent object to hold all app buttons
    public GameObject appButtonPrefab; // Prefab with Image + Text

    private List<AppData> cachedApps = new ();

    [Header("VR App Panels")]
    public GameObject panelPrefab; // Prefab with Quad + AppPanel script + XRInteractable
    public Texture videoTex;
    // to hold app opened already?
    private readonly Dictionary<String, GameObject> activePanels = new ();

    public IEnumerator FetchAppList(string serverIP) {
        yield return StartCoroutine(GetAppsFromServer(serverIP));
    }

    public IEnumerator GetAppsFromServer(string serverIP) {
        string url = $"http://{serverIP}:5000/applist";
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
            Debug.LogError("Failed to get app list: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        AppListResponse appList = JsonUtility.FromJson<AppListResponse>(json);

        cachedApps = appList.apps;

        foreach (AppData app in appList.apps) {
            // Call coroutine to download icon
            yield return StartCoroutine(DownloadIcon(app, serverIP));
        }
    }

    private IEnumerator DownloadIcon(AppData app, string serverIP) {
        // Use the app name to fetch the icon URL (assuming the server gives the icon URL based on the app name)
        string getIcon = $"http://{serverIP}:5000/getIcon?name={UnityWebRequest.EscapeURL(app.name)}";  // URL to fetch the icon based on the app name
        UnityWebRequest iconRequest = UnityWebRequestTexture.GetTexture(getIcon);

        yield return iconRequest.SendWebRequest();

        if (iconRequest.result == UnityWebRequest.Result.Success) {
            Texture2D texture = DownloadHandlerTexture.GetContent(iconRequest);
            app.icon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        } else { Debug.LogWarning("Failed To Load Image: " + getIcon); }

    }

    public void DisplayAppList(string serverIP) {
        foreach (Transform child in appContainer) {
            Destroy(child.gameObject);
        }

        foreach (AppData app in cachedApps) {
            GameObject buttonObj = Instantiate(appButtonPrefab, appContainer);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = app.name;

            Image iconImage = buttonObj.transform.Find("Icon").GetComponent<Image>();

            // Use the cached icon directly
            if (app.icon != null) {
                iconImage.sprite = app.icon;
            } else {
                Debug.LogWarning($"No cached icon for {app.name}, setting default icon.");
                // Set a default or placeholder icon
                iconImage.sprite = Resources.Load<Sprite>("default_icon");  // Placeholder image in Resources folder
            }

            // Add the app button click listener
            buttonObj.GetComponent<Button>().onClick.AddListener(() => {
                StartCoroutine(LaunchApp(serverIP, app.name));
                taskBarControl.OnClickAppList();
            });
        }
    }


    IEnumerator LaunchApp(string serverIP, string appName) {
        string launchUrl = $"http://{serverIP}:5000/launch?name={UnityWebRequest.EscapeURL(appName)}";
        Debug.Log($"Launching {appName} via {launchUrl}");

        UnityWebRequest request = UnityWebRequest.Get(launchUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            LaunchResponse response = JsonUtility.FromJson<LaunchResponse>(json);

            if (response.success) {
                Debug.Log($"Successfully launched {appName} with AppID: {response.appId}");

                //Have my corutine to start the connection and if conenction sucess the assign the stream data to AppPanelView
                CreateAppPanel(appName,response.appId,videoTex);
            } else {
                Debug.LogError($"Server failed to launch {appName}: {response.message}");
            }
        } else {
            Debug.LogError($"Failed to launch {appName}: {request.error}");
        }
    }
    public void CreateAppPanel(string appName, string appId, Texture videoTex) {
        GameObject panel = Instantiate(panelPrefab, TaskBarControl.taskbar);
        TaskBarControl.ShowPanelInTop(panel);
        panel.name = $"{appName}";
        panel.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.2f, 1.5f);
        panel.transform.localRotation = Quaternion.identity;

        AppPanel appPanel = panel.GetComponent<AppPanel>();
        appPanel.appId = appId;
        appPanel.SetAppInfo(appName, videoTex);
        appPanel.closeButton.onClick.AddListener(() => CloseAppPanel(appId));

    }

    public void CloseAppPanel(string appId) {
        if (!activePanels.ContainsKey(appId)) return;

        StartCoroutine(CloseAppOnServer(appId));
    }

    private IEnumerator CloseAppOnServer(string appId) {
        string url = $"http://{TaskBarControl.serverIP}:5000/close?appId={UnityWebRequest.EscapeURL(appId)}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log($"App {appId} closed successfully.");

            // Destroy panel
            Destroy(activePanels[appId]);
            activePanels.Remove(appId);

            // Show in AppList again
            DisplayAppList(TaskBarControl.serverIP);
        } else {
            Debug.LogError($"Failed to close app {appId}: {request.error}");
        }
    }

}
