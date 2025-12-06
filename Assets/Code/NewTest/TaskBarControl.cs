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
using ReadyPlayerMe.Core;

public class TaskBarControl : MonoBehaviour
{
    [Header("TaskBar Position To Show In Top")]
    [SerializeField] private Transform taskbar;
    [SerializeField] private float topOffset = 2.25f;

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
    public string serverIP;
    [SerializeField] private int port = 5000;

    [Header("App List Loader")]
    public GetAppList appListLoader; // Drag your GetAppList script here in the Inspector
    public GameObject appListPanel;
    public Button closeAppList;

    [Header("Character")]
    public GameObject avatar;
    [SerializeField] private float  yOffset = 0.25f;
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
        avatar.SetActive(true);
        PlayAudio();
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
        Debug.Log("Voice toggled: " + isVoice);
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
            appListLoader.DisplayAppList(serverIP);

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
        string path = Path.Combine(Application.dataPath, "My_res/Test scene/Dominic - British, Dark, Brooding, Intense_cNl7zmADcPo3qlH6Sl5o.wav");
        recordAudio.SendExistingAudioFile(path, (transcription) =>
        {
            if (!string.IsNullOrEmpty(transcription)) {
                Debug.Log("Transcription received: " + transcription);
                // You can also store it in a variable or display it in UI
            } else {
                Debug.LogError("Failed to get transcription.");
            }
        });
    }

    public void PlayAudio() {
   
        AudioClip clip = Resources.Load<AudioClip>("Audio/Dominic - British, Dark, Brooding, Intense_cNl7zmADcPo3qlH6Sl5o");
        if (clip == null) { Debug.LogError("Error audio not found"); }
        voiceSource.clip = clip;
        voiceSource.Play();
        Debug.Log("playing");

    }

}
