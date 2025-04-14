using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraBlockerDeathZone : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private Transform playerTransform;
    private Vector3 lastPlayerPosition;
    private Transform fixedPositionTarget;

    void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineCameraBlockerDeathZone must be attached to a CinemachineCamera."); //Corrected Log
        }

        fixedPositionTarget = new GameObject("FixedPositionTarget").transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameObject.CompareTag("deathbox")) // Changed tag to deathbox
        {
            playerTransform = other.transform;
            lastPlayerPosition = playerTransform.position;

            fixedPositionTarget.position = lastPlayerPosition;
            virtualCamera.Follow = fixedPositionTarget;
            virtualCamera.LookAt = fixedPositionTarget;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && gameObject.CompareTag("deathbox")) // Changed tag to deathbox
        {
            virtualCamera.Follow = playerTransform;
            virtualCamera.LookAt = playerTransform;
            playerTransform = null;
        }
    }

    void OnDestroy()
    {
        if (fixedPositionTarget != null)
        {
            Destroy(fixedPositionTarget.gameObject);
        }
    }
}