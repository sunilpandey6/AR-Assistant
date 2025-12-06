using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Task Bar")]
    [SerializeField] private OVRCameraRig ovrRig;
    [SerializeField] private GameObject panelPrefab;
    [SerializeField] private float panelDistance = 1.5f;
    [SerializeField] private float distanceY = -0.2f;
    [SerializeField] private float ttl = 60f;

    private float idleTimer = 0f;
    private CanvasGroup cg;

    private void Start() {
        panelPrefab.SetActive(true);
        cg = panelPrefab.GetComponent<CanvasGroup>();
        StartCoroutine(ShowPanelWithDelay());
    }

    private IEnumerator ShowPanelWithDelay() {
        yield return new WaitForSeconds(Random.Range(5f, 8f));
        ShowPanel();
    }

    private void Update() {
        if (OVRInput.GetDown(OVRInput.Button.Start)) {
            Debug.Log("Start button pressed! Showing panel.");
            ShowPanel();
        } else {
            if (cg.alpha > 0f) // panel visible
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= ttl) {
                    Debug.Log("TTL expired. Hiding panel.");
                    HidePanel();
                    idleTimer = 0f;
                }
            }
        }
    }

    public void ShowPanel() {
        Transform cam = ovrRig.centerEyeAnchor;

        Vector3 spawnPos = cam.position + cam.forward * panelDistance;
        spawnPos.y = cam.position.y + distanceY;

        panelPrefab.transform.SetPositionAndRotation(
            spawnPos,
            Quaternion.LookRotation(cam.forward, cam.up)
        );

        idleTimer = 0f;

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void HidePanel() {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}
