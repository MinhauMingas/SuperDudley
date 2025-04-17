using UnityEngine;

public class TriggerCameraChange : MonoBehaviour
{
    public CinemachineOffsetYModifier cameraModifier;
    public Vector3 targetOffset = new Vector3(0f, 2f, 0f);
    public float transitionSpeed = 1.0f;
    public bool resetOnExit = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (cameraModifier != null)
            {
                cameraModifier.StartOffsetChange(targetOffset, transitionSpeed);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (cameraModifier != null && resetOnExit)
            {
                cameraModifier.ResetOffset();
            }
        }
    }
}