using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

// Data classes (keep it simple inside GetAppList)
[System.Serializable]
public class AppData
{
    public string name;
    public string appPath;
    public string iconPath; // For JSON parsing
    public string bundleId;
    [System.NonSerialized]
    public Sprite icon;       // Converted Sprite
}

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
}

public class GetAppList : MonoBehaviour
{
    [Header("UI References")]
    public Transform appContainer;         // Parent object for app buttons
    public GameObject appButtonPrefab;     // Button prefab with Text + Icon

    [Header("UI Prefab")]
    public AppPanel appPanelPrefab;
    [Header("Active Panel Manager")]
    public ActivePanelManager aPM;

    private readonly List<AppData> cachedApps = new ();

    // Fetch app list from Mac server
    public IEnumerator FetchAppList(string serverIP) {
        string url = $"http://{serverIP}:5000/applist";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Failed to get app list: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        AppListResponse response = JsonUtility.FromJson<AppListResponse>(json);

        cachedApps.Clear();

        //assign the icons
        foreach (var app in response.apps) {
            if (!string.IsNullOrEmpty(app.iconPath)) {
                yield return StartCoroutine(DownloadIcon(serverIP,app));
            }
            cachedApps.Add(app);
        }

        DisplayAppList(serverIP);
    }

    private IEnumerator DownloadIcon(string serverIP, AppData app) {
        string getIcon = $"http://{serverIP}:5000/getIcon?name={UnityWebRequest.EscapeURL(app.name)}";
        UnityWebRequest iconRequest = UnityWebRequestTexture.GetTexture(getIcon);

        yield return iconRequest.SendWebRequest();

        if (iconRequest.result == UnityWebRequest.Result.Success) {
            Texture2D texture = DownloadHandlerTexture.GetContent(iconRequest);
            app.icon = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        } else { Debug.LogWarning("Failed To Load Image: " + getIcon); }
    }

    // Display apps in UI
    public void DisplayAppList(string serverIP) {
        foreach (Transform child in appContainer)
            Destroy(child.gameObject);

        foreach (AppData app in cachedApps) {
            GameObject buttonObj = Instantiate(appButtonPrefab, appContainer);

            // Set app name
            TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            text.text = app.name;

            // Set icon
            Image iconImage = buttonObj.transform.Find("Icon").GetComponent<Image>();
            iconImage.sprite = app.icon != null ? app.icon : Resources.Load<Sprite>("default_icon");

            // Launch app on click
            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => StartCoroutine(LaunchApp(serverIP,app.appPath)));
        }
    }

    // Launch app via server
    private IEnumerator LaunchApp(string serverIP,string appPath) {
        string url = $"http://{serverIP}:5000/launch?path={UnityWebRequest.EscapeURL(appPath)}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            LaunchResponse response = JsonUtility.FromJson<LaunchResponse>(json);

            if (response.success)
            {
                Debug.Log($"Launched {appPath} successfully.");

                AppData launchedApp = cachedApps.Find(a => a.appPath == appPath);
                if (launchedApp != null && !string.IsNullOrEmpty(launchedApp.bundleId))
                {
                    Debug.Log($"Waiting for app to open... (Target: {launchedApp.bundleId})");

                    // Wait a moment for the app to open and Helper to be ready
                    yield return new WaitForSeconds(2.0f);

                    AppPanel newPanel = Instantiate(appPanelPrefab);
                    newPanel.aPM = aPM; // assign your ActivePanelManager
                    newPanel.Initialize(launchedApp.name, launchedApp.bundleId);
                    newPanel.StartScreenShare();
                    
                }
                else
                {
                    Debug.LogError("Could not find Bundle ID for launched app.");
                }
            }
            else
                Debug.LogError($"Server failed to launch app: {response.message}");
        }
        else
        {
            Debug.LogError($"Failed to contact server to launch app: {request.error}");
        }
    }
}
