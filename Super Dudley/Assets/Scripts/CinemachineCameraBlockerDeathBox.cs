using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraBlockerDeathBox : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private Transform originalFollow;
    private Transform originalLookAt;

    void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineCameraBlockerDeathBox must be attached to a CinemachineVirtualCamera.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameObject.CompareTag("deathbox"))
        {
            if (virtualCamera != null)
            {
                originalFollow = virtualCamera.Follow;
                originalLookAt = virtualCamera.LookAt;

                virtualCamera.Follow = null;
                virtualCamera.LookAt = null;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && gameObject.CompareTag("deathbox"))
        {
            if (virtualCamera != null)
            {
                virtualCamera.Follow = originalFollow;
                virtualCamera.LookAt = originalLookAt;
            }
        }
    }
}