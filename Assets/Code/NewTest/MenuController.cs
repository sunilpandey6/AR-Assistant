using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Task Bar")]
    [SerializeField] protected OVRCameraRig ovrRig;
    [SerializeField] protected GameObject panelPrefab;
    [SerializeField] protected float panelDistance;
    [SerializeField] protected float distanceY;
    [SerializeField] protected float ttl = 60f;
    private float idleTimer = 0f;

    CanvasGroup cg;

    private void Start() {
        panelPrefab.SetActive(true);
       StartCoroutine(ShowPanelWithDelay());
        //cg = panelPrefab.GetComponent<CanvasGroup>();
    }

    private IEnumerator ShowPanelWithDelay() {
        float waitTime = Random.Range(5f, 8f);
        yield return new WaitForSeconds(waitTime);

        ShowPanel();
    }


    protected void Update() {
        if (OVRInput.GetDown(OVRInput.Button.Start)) {
            Debug.Log("Start button pressed! Showing panel.");
            ShowPanel();
        } 
        else 
        {
            if (panelPrefab.activeSelf) {
                idleTimer += Time.deltaTime;
                if (idleTimer >= ttl) {
                    Debug.Log("TTL expired. Hiding panel.");
                    HidePanel();
                    idleTimer = 0f;
                } 
                else {
                    Debug.Log("Panel is inactive, waiting for input.");
                }
            }
        }
    }

    public void ShowPanel() {
        
        Transform cam = ovrRig.centerEyeAnchor;
        Vector3 spawnPos = cam.position + cam.forward * panelDistance;
        spawnPos.y = cam.position.y + distanceY;
        Quaternion spawnRot = Quaternion.LookRotation(cam.forward, cam.up);
        panelPrefab.transform.SetPositionAndRotation(spawnPos, spawnRot);
        idleTimer = 0f; // reset TTL timer while user is pointing

        //cg.alpha = 1f;        // visible
        //cg.interactable = true;
        //cg.blocksRaycasts = true;
    }

    protected void HidePanel() {
        cg.alpha = 0f;        // invisible
        cg.interactable = false;
        cg.blocksRaycasts = false;  // important so it doesn’t block raycasts
    }
}
