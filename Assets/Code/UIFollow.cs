using UnityEngine;
using System.Collections;


public class UIFollow : MonoBehaviour
{
    [Header("Task Bar")]
    [SerializeField] protected OVRCameraRig ovrRig;
    [SerializeField] protected GameObject panelPrefab;
    [SerializeField] protected float panelDistance;
    [SerializeField] protected float ttl = 60f;
    private float idleTimer = 0f;
 
    private void Start() {
        panelPrefab.SetActive(false);
    }
    protected void Update() {
        if (OVRInput.GetDown(OVRInput.Button.Start)) {
            ShowPanel();
            idleTimer = 0f; // reset TTL timer while user is pointing
        } else if (panelPrefab.activeSelf) {
            idleTimer += Time.deltaTime;
            if (idleTimer >= ttl) {
                HidePanel();
                idleTimer = 0f;
            }
        }
    }

    protected void ShowPanel() {
        panelPrefab.SetActive(true);
        Transform cam = ovrRig.centerEyeAnchor;
        Vector3 spawnPos = cam.position + cam.forward * panelDistance;
        Quaternion spawnRot = Quaternion.LookRotation(cam.forward, cam.up);
        panelPrefab.transform.SetPositionAndRotation(spawnPos, spawnRot);
        ttl = 0f;
    }

    protected void HidePanel() {
        if (panelPrefab != null) {
            panelPrefab.SetActive(false);
        }
    }
}
