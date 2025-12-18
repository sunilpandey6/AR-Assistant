using System.Collections.Generic;
using UnityEngine;

public class ActivePanelManager : MonoBehaviour
{
    [Header("References")]
    public Transform taskbar;          // Taskbar reference
    public Transform cameraTransform;  // VR Camera reference (OVR rig or main camera)

    [Header("Panel Settings")]
    public float topoffsetbar = 0.35f;
    public float topOffset = 0.25f;         // Height for centre panel above taskbar
    public float horizontalSpacing = 0.85f; // Side spacing for left/right panels
    public float panelSpacing = 0.3f;       // Spacing for stacking multiple panels in same slot
    public float topTiltAngle = 30f;        // Downward tilt for top panels
    public float forwardOffset = 0.2f;

    public enum PanelSlot
    {
        Centre,
        Top,
        Left,
        Right
    }
    private List<GameObject> activePanels = new List<GameObject>();
    private GameObject centrePanel = null;

    #region Panel Control

    public void ShowPanel(GameObject panel)
    {
        if (panel == null || taskbar == null || cameraTransform == null) return;

        if (!activePanels.Contains(panel))
            activePanels.Add(panel);

        ReassignSlots();
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(false);
        activePanels.Remove(panel);

        if (panel == centrePanel)
            centrePanel = null;

        ReassignSlots();
    }

    public void TogglePanel(GameObject panel)
    {
        if (panel == null) return;

        if (activePanels.Contains(panel))
            ClosePanel(panel);
        else
            ShowPanel(panel);
    }

    #endregion

    #region Slot Assignment

    private void ReassignSlots()
    {
        if (activePanels.Count == 0 || taskbar == null) return;

        // Always make the newest panel the centre
        centrePanel = activePanels[activePanels.Count - 1];

        List<GameObject> leftPanels = new();
        List<GameObject> rightPanels = new();

        int sideIndex = 0; // 0 = right, 1 = left

        // Assign other panels (all except centre)
        for (int i = 0; i < activePanels.Count - 1; i++)
        {
            GameObject panel = activePanels[i];
            if (sideIndex == 0)
                rightPanels.Add(panel);
            else
                leftPanels.Add(panel);

            sideIndex = 1 - sideIndex; // toggle
        }

        // Place centre panel
        PlacePanel(centrePanel, PanelSlot.Centre);

        // Place right panels
        for (int i = 0; i < rightPanels.Count; i++)
            PlacePanel(rightPanels[i], PanelSlot.Right, centrePanel, i);

        // Place left panels
        for (int i = 0; i < leftPanels.Count; i++)
            PlacePanel(leftPanels[i], PanelSlot.Left, centrePanel, i);
    }


    #endregion

    #region Panel Placement

    private void PlacePanel(GameObject panel, PanelSlot slot, GameObject basePanel = null, int index = 0)
    {
        if (panel == null || taskbar == null || cameraTransform == null) return;

        Vector3 pos = taskbar.position;
        Quaternion rot = Quaternion.identity;

        switch (slot)
        {
            case PanelSlot.Centre:
                pos += Vector3.up * topoffsetbar;
                rot = taskbar.rotation;
                break;

            case PanelSlot.Right:
                Vector3 rightBase = basePanel != null ? basePanel.transform.position : taskbar.position;
                pos = rightBase + Vector3.right * (horizontalSpacing + index * panelSpacing)
                    + taskbar.forward * forwardOffset;
                break;

            case PanelSlot.Left:
                Vector3 leftBase = basePanel != null ? basePanel.transform.position : taskbar.position;
                pos = leftBase - Vector3.right * (horizontalSpacing + index * panelSpacing)
                    + taskbar.forward * forwardOffset;
                break;

            case PanelSlot.Top:
                Vector3 topBase = basePanel != null ? basePanel.transform.position : taskbar.position;
                pos = topBase + Vector3.up * (topOffset + index * panelSpacing);
                break;
        }

        panel.transform.position = pos;

        // Rotate panel to face user
        panel.transform.LookAt(cameraTransform.position);
        panel.transform.Rotate(0, 180f, 0); // correct forward direction

        // Apply downward tilt for top panels
        if (slot == PanelSlot.Top)
            panel.transform.Rotate(topTiltAngle, 0f, 0f);
    }

    #endregion
}