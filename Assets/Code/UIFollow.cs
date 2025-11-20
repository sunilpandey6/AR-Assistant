using UnityEngine;

public class UIFollow : MonoBehaviour
{
    [Header("Camera Follow")]
    public Transform cameraTransform;  // assign CenterEyeAnchor

    [Tooltip("Offset from camera in local space. Editable in Play Mode.")]
    public Vector3 offset = new Vector3(0, -0.2f, 0.6f);

    [Tooltip("Rotation tilt in degrees. Editable in Play Mode.")]
    public Vector3 rotationEuler = Vector3.zero;

    public float speed = 5f;

    [Header("Features")]
    public bool followStart = true;

    void Start() {
        if (followStart && cameraTransform != null) {
            SpawnInFront();
        }
    }

    void LateUpdate() {
        if (cameraTransform == null) return;

        // Compute target position in front of camera using offset
        Vector3 targetPos = cameraTransform.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);

        // Apply rotation from inspector
        Quaternion targetRot = Quaternion.Euler(rotationEuler);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * speed);
    }

    void SpawnInFront() {
        Vector3 targetPos = cameraTransform.TransformPoint(offset);
        transform.position = targetPos;

        // Optional initial rotation to face camera horizontally
        Vector3 lookDir = cameraTransform.position - transform.position;
        lookDir.y = 0; // keep upright
        if (lookDir.sqrMagnitude > 0.001f) {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
