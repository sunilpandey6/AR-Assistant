using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AppPanel : MonoBehaviour
{
    [Header("References")]
    public MeshRenderer videoSurface;  // Assign the Quad’s MeshRenderer
    public TextMeshProUGUI appNameText;
    public Button minimizeButton;
    public Button closeButton;

    [HideInInspector] public string appId;

    private bool isMinimized = false;
    private Material runtimeMat;
    private Texture currentTexture;

    void Start() {
        if (videoSurface != null) {
            runtimeMat = new Material(videoSurface.sharedMaterial);
            videoSurface.material = runtimeMat;
        }

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(ToggleMinimize);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => UIManager.Instance.CloseVRPanel(appId));
    }

    public void SetAppInfo(string appName, Texture videoTex) {
        if (appNameText != null)
            appNameText.text = appName;

        currentTexture = videoTex;
        //if (runtimeMat != null && currentTexture != null)
        //    runtimeMat.mainTexture = currentTexture;
        runtimeMat.mainTexture = currentTexture;
        var webrtc = gameObject.AddComponent<WebRTCReceiver>();
        webrtc.Init(UIManager.Instance.serverIP, appId, videoSurface);
    }

    void ToggleMinimize() {
        isMinimized = !isMinimized;
        videoSurface.enabled = !isMinimized;

        // You can also hide the title bar if you want:
        // appNameText.transform.parent.gameObject.SetActive(!isMinimized);
    }

}
