using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

[RequireComponent(typeof(CinemachineCamera))]
public class SmoothCinemachineFollow : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineThirdPersonFollow followComponent;
    private CinemachineHardLookAt lookAtComponent;

    private Transform target;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        followComponent = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
        lookAtComponent = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim) as CinemachineHardLookAt;
    }

    private void OnEnable()
    {
        StartCoroutine(FindPlayerTarget());
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (followComponent != null)
        {
            transform.position = target.position + followComponent.ShoulderOffset;
        }
        else
        {
            transform.position = target.position;
        }

        if (lookAtComponent != null && virtualCamera.LookAt != null)
        {
            Vector3 lookDirection = (virtualCamera.LookAt.position - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        else
        {
            transform.rotation = target.rotation;
        }
    }

    private IEnumerator FindPlayerTarget()
    {
        float maxWaitTime = 5f;
        float startTime = Time.time;

        while (target == null && Time.time - startTime < maxWaitTime)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                virtualCamera.Follow = target;
                virtualCamera.LookAt = target;
            }
            else
            {
                Debug.Log("Waiting for Player to respawn...");
                yield return new WaitForSeconds(0.01f);
            }
        }

        if (target == null)
        {
            Debug.LogError("Player not found after " + maxWaitTime + " seconds.");
        }
    }
}