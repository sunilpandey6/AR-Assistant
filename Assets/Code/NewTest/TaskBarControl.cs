using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;

public class TaskBarControl : MonoBehaviour
{
    [Header("TaskBar Position To Show In Top")]
    [SerializeField] public Transform taskbar;
    [SerializeField] public float topOffset = 2.25f;

    [Header("TaskBar Button")]
    [SerializeField] Button setting;
    [SerializeField] Button showApp;
    [SerializeField] Button showAvatar;
    [SerializeField] Button voice;
    [SerializeField] Button showChat;

    [Header("Setting Panel")]
    [SerializeField] private GameObject settingPanel;
    public static bool isVoice = false;

    [Header("Connection Panel")]
    public GameObject connectionPanel;
    public TMP_InputField IPInput;
    public Button submitButton;
    public TextMeshProUGUI status;
    [SerializeField] private bool connectionDone = false;
    public static string serverIP;
    [SerializeField] private int port = 5000;

    [Header("App List Loader")]
    public GetAppList appListLoader; // Drag your GetAppList script here in the Inspector
    public GameObject appListPanel;
    public Button closeAppList;

    [Header("Character")]
    public GameObject avatar;
    [SerializeField] private float  yOffset = 0.4f;
    [SerializeField] private float  disOffset = 0.25f;
    public AvatarAnimatorControl animatorControl;
    [SerializeField] private float animatorChangeStateTime = 20f;
    [SerializeField] private float avaRotate = 125f;
    private float idleTimer;
    private int newIdle;
    private int lastIdle;

    [Header("Chat UI References")]
    public GameObject panelUI;
    [SerializeField] private float horizontalSpacing = 0.35f;
    [SerializeField] private float rotatepanel = 25f;
    [SerializeField] private ChatBoxManager chatBoxManager;
    

    [Header("Voice Recording Settings")]
    [SerializeField] private RecordAudio recordAudio;
    [SerializeField] private AudioSource voiceSource;

    // Display panel in top of task bar
    void ShowPanelInTop(GameObject panel) {
        if (panel == null || taskbar == null) return;

        Vector3 pos = taskbar.position + Vector3.up * topOffset;
        Quaternion rot = taskbar.rotation;

        panel.transform.SetPositionAndRotation(pos, rot);

        panel.SetActive(true);
    }

    //Toogle Panel to hide
    public void ToggleClose(GameObject panel) {
        if(panel == null) return;
        bool isActive = panel.activeSelf;

        if (isActive) {
            panel.SetActive(false);
        }
    }

    bool PanelsOverlap(GameObject a, GameObject b, float threshold = 0.2f) {
        if (!a.activeSelf || !b.activeSelf) return false;

        float dist = Vector3.Distance(a.transform.position, b.transform.position);
        return dist < threshold;   // smaller distance means overlap
    }


    private void Start() {
        connectionPanel.SetActive(false);
        appListPanel.SetActive(false);
        avatar.SetActive(false);
        panelUI.SetActive(false);
        settingPanel.SetActive(false);
        // taskbar
        // setting.onClick.AddListener(OnClickSetting);
        // showApp.onClick.AddListener(OnClickAppList);
        // showAvatar.onClick.AddListener(OnCLickShowAvatar);  
        // showChat.onClick.AddListener(() => OnClickShowChatUI(panelUI));
        //voice Button
        //voice.onClick.AddListener(OnPointerDown);
        var eventTrigger = voice.gameObject.AddComponent<EventTrigger>();
        // Pointer Down
        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => { StartRecording(); });
        eventTrigger.triggers.Add(pointerDown);

        // Pointer Up
        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => { StopRecording(); });
        eventTrigger.triggers.Add(pointerUp);

        // connectionPanel
        submitButton.onClick.AddListener(OnConnectClick);
        // AppListPanel
        closeAppList.onClick.AddListener(() => ToggleClose(appListPanel));

    }

    void Update() {

        idleTimer += Time.deltaTime;            // count idle time
        animatorControl.SetIdleTimer(idleTimer); // send value to animator

        if (idleTimer > animatorChangeStateTime)                     // if idle for 25 seconds
        {
            do {
                newIdle = UnityEngine.Random.Range(0, 2);

            } while (newIdle == lastIdle);
            animatorControl.SetIdleTimer(idleTimer);
            animatorControl.SetRandomIdleState(newIdle);
            lastIdle = newIdle;
            idleTimer = 0;                      // reset timer
        }
    }
    #region Setting
    public void OnClickSetting() {
        if (settingPanel.activeSelf) {
            ToggleClose(settingPanel);
            return;
        }

        ShowPanelInTop(settingPanel);
    }

    public void SetVoiceEnabled(bool value) {
        isVoice = value;
        DebugLogger.Log("Voice toggled: " + isVoice);
    }


    #endregion

    #region AppList Button
    // when Applist button is pressed
    public void OnClickAppList() {
        if (!connectionDone) {
            ShowPanelInTop(connectionPanel);
            return;
        }
        if (appListPanel.activeSelf) {
            ToggleClose(appListPanel);
            return ;
        } 
        
        ShowPanelInTop(appListPanel);
        appListLoader.DisplayAppList(serverIP);
        if (panelUI.activeSelf) { 
            if (PanelsOverlap(appListPanel,panelUI))
                    ShowPanelRightOf(appListPanel, panelUI);
        }    
    }

    // Connection Panel Connection
    void OnConnectClick() {
        serverIP = IPInput.text.Trim();

        if (string.IsNullOrEmpty(serverIP)) {
            status.text = "Please enter a valid IP address.";
            return;
        }

        StartCoroutine(ConnectToServer());
    }

    IEnumerator ConnectToServer() {
        string url = $"http://{serverIP}:{port}/ping";
        status.text = $"Connecting to {url}...";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.text.Trim().ToLower() == "pong") {
            status.text = "Connection OK";
            yield return new WaitForSeconds(1f);
            
            yield return StartCoroutine(appListLoader.FetchAppList(serverIP));
            ToggleClose(connectionPanel);
            connectionDone = true;
            ShowPanelInTop(appListPanel);
        } else {
            status.text = $"Connection failed: {req.error}";
        }
    }
    #endregion

    #region Avatar Load
    public void OnCLickShowAvatar() {
        // load the avatar at left of the task bar
        if (avatar != null && taskbar != null ) {
            //toogle avatar visibility
            bool newstate = !avatar.activeSelf;
            avatar.SetActive(newstate);

            if (!newstate) return;
            
            Vector3 pos = taskbar.position;
            pos += -taskbar.right * disOffset;
            pos -= taskbar.up * yOffset;

            avatar.transform.position = pos;
            Quaternion rot = taskbar.rotation * Quaternion.Euler(0f, avaRotate, 0f);
            avatar.transform.SetPositionAndRotation(pos, rot);
            animatorControl.anim.Play("Wave", 0, 0f);
        }
    }
    #endregion

    #region Voice control
    public void StartRecording() {
        animatorControl.PlayNod();
        recordAudio.StartRecording();
    }

    public void StopRecording() {
        recordAudio.StopRecording((result) => {
            animatorControl.PlayNod();
            chatBoxManager.AddMessage(result, true);
            var payload = new N8nRequest {
                sessionId = "1001",
                message = result,
                voice = TaskBarControl.isVoice
            };
            StartCoroutine(chatBoxManager.SendToN8N(payload));
        });
    }
    #endregion

    #region Chat

    void ShowPanelRightOf(GameObject basePanel, GameObject panelToShow) {
        Vector3 pos = basePanel.transform.position + taskbar.right * horizontalSpacing;
        Quaternion rot = basePanel.transform.rotation * Quaternion.Euler(0f, rotatepanel, 0f);

        panelToShow.transform.SetPositionAndRotation(pos, rot);
    }


    public void OnClickShowChatUI(GameObject panel) {
        if (panel == null) return;
        bool newState = !panel.activeSelf;
        if (!newState) {
            ToggleClose(panel);
            return;
        }

        
        // If AppList is active
        if (appListPanel.activeSelf) 
            ShowPanelRightOf(appListPanel, panel);
        else
            ShowPanelInTop(panel);  

        panel.SetActive(true);
    }

    #endregion

    public void CheckTTS() {

        StartCoroutine(TestAudioPlay());

        //Save clip and send for transcription
        AudioClip clip = Resources.Load<AudioClip>("Audio/test_audio");

        string tempPath = Path.Combine(Application.temporaryCachePath, "tts.wav");
        DebugLogger.Log("Saving temp TTS file to: " + tempPath);

        WavUtility.Save(tempPath, clip);

        DebugLogger.Log("Sending audio for transcription...");

        // Directly start the transcription coroutine
        StartCoroutine(recordAudio.TranscribeAudio(tempPath, (transcription) => {
            DebugLogger.Log("Transcription returned: " + transcription);
        }));

        DebugLogger.Log("===== CheckTTS() SETUP COMPLETE =====");
    }

    private IEnumerator TestAudioPlay() {
        DebugLogger.Log("===== CheckTTS PLAY TEST =====");

        // 1. Load audio
        AudioClip clip = Resources.Load<AudioClip>("Audio/test_audio");
        if (clip == null) {
            DebugLogger.Log("NO AUDIO FOUND in Resources/Audio/test_audio");
            yield break;
        }

        DebugLogger.Log("Clip loaded: " + clip.name + ", length = " + clip.length);

        // 2. Ensure avatar is active
        DebugLogger.Log("Avatar active before: " + avatar.activeSelf);
        if (!avatar.activeSelf) {
            DebugLogger.Log("Avatar was inactive. Activating...");
            avatar.SetActive(true);
        }
        DebugLogger.Log("Avatar active after: " + avatar.activeSelf);
        animatorControl.StartTalking();
        // 3. Check AudioListeners
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        DebugLogger.Log("AudioListeners found: " + listeners.Length);

        for (int i = 0; i < listeners.Length; i++) {
            DebugLogger.Log("Listener[" + i + "] on object: " + listeners[i].gameObject.name);
        }

        //// 4. Log AudioSource details
        //DebugLogger.Log("AudioSource object activeInHierarchy: " + voiceSource.gameObject.activeInHierarchy);
        //DebugLogger.Log("AudioSource enabled: " + voiceSource.enabled);
        //DebugLogger.Log("AudioSource volume: " + voiceSource.volume);
        //DebugLogger.Log("AudioSource spatialBlend BEFORE: " + voiceSource.spatialBlend);

        // 5. Force audio to 2D
        voiceSource.spatialBlend = 0f;
        voiceSource.rolloffMode = AudioRolloffMode.Linear;
        voiceSource.minDistance = 0.1f;
        voiceSource.maxDistance = 100f;

        DebugLogger.Log("AudioSource spatialBlend AFTER: " + voiceSource.spatialBlend);

        // 6. Set clip
        voiceSource.clip = clip;
        voiceSource.playOnAwake = false;

        // 7. Wait one frame (required on Quest)
        yield return null;

        DebugLogger.Log("Calling Play() on AudioSource: " + voiceSource.gameObject.name);
        voiceSource.Play();

        DebugLogger.Log("Waiting for clip to finish...");
        yield return new WaitForSeconds(clip.length);
        animatorControl.StopTalking();
        DebugLogger.Log("Finished playing.");
    }


}
