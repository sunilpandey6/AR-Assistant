using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskbarManager : MonoBehaviour
{
    public Transform taskbarContainer;
    public GameObject taskbarButtonPrefab;
    private Dictionary<string, GameObject> activeApps = new();

    public void AddApp(string appId, string appName, GameObject panel) {
        if (activeApps.ContainsKey(appId)) return;

        var button = Instantiate(taskbarButtonPrefab, taskbarContainer);
        button.GetComponentInChildren<TextMeshProUGUI>().text = appName;

        button.GetComponent<Button>().onClick.AddListener(() => {
            bool isActive = panel.activeSelf;
            panel.SetActive(!isActive);
        });

        activeApps[appId] = button;
    }

    public void RemoveApp(string appId) {
        if (activeApps.TryGetValue(appId, out GameObject btn)) {
            Destroy(btn);
            activeApps.Remove(appId);
        }
    }
}
