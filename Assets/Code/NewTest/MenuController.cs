using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] protected Transform handTransform;
    [SerializeField] protected Transform controllerTransform;
    [SerializeField] protected float minAngle;
    [SerializeField] protected float maxAngle;
    [SerializeField] protected float panelDistance;
    [SerializeField] protected GameObject panelPrefab;
    [SerializeField] protected float ttl = 60f;

    private GameObject currentPanel;
    private float idleTimer = 0f;

    protected void Update() {
        Transform tracked = handTransform != null ? handTransform : controllerTransform;
        if (tracked == null) return;

        float angle = Vector3.Angle(tracked.up, Vector3.up);
        bool shouldShow = angle >= minAngle && angle <= maxAngle;


        if (shouldShow) {
            ShowPanel(tracked);
            idleTimer = 0f; // reset TTL timer while user is pointing
        } else if (currentPanel != null) {
            idleTimer += Time.deltaTime;
            if (idleTimer >= ttl) {
                HidePanel();
                idleTimer = 0f;
            }
        }
    }

    protected void ShowPanel(Transform tracked) {
        if (currentPanel == null) {
            currentPanel = Instantiate(panelPrefab);
        }

        currentPanel.SetActive(true);
        Vector3 panelPos = tracked.position + tracked.up * panelDistance;
        Quaternion panelRot = Quaternion.LookRotation(tracked.forward);
        currentPanel.transform.SetPositionAndRotation(panelPos, panelRot);
    }

    protected void HidePanel() {
        if (currentPanel != null) {
            currentPanel.SetActive(false);
        }
    }
}
