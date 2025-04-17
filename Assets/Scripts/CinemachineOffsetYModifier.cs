using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CinemachineOffsetYModifier : MonoBehaviour
{
    private CinemachineComponentBase componentBase;
    private CinemachineFollow transposer;
    private Vector3 initialOffset;
    private Coroutine currentTransition;

    public float defaultResetSpeed = 1f;

    void Start()
    {
        CinemachineCamera virtualCamera = GetComponent<CinemachineCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("Cinemachine Camera component not found on this GameObject!");
            return;
        }

        componentBase = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);

        if (componentBase is CinemachineFollow)
        {
            transposer = (CinemachineFollow)componentBase;
            initialOffset = transposer.FollowOffset;
        }
        else
        {
            Debug.LogError("Cinemachine Camera does not have a Cinemachine Follow component!");
        }
    }

    public void StartOffsetChange(Vector3 targetOffset, float transitionSpeed)
    {
        if (transposer != null)
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
            currentTransition = StartCoroutine(SmoothTransition(transposer.FollowOffset, targetOffset, transitionSpeed));
        }
    }

    public void ResetOffset()
    {
        if (transposer != null)
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
            currentTransition = StartCoroutine(SmoothTransition(transposer.FollowOffset, initialOffset, defaultResetSpeed));
        }
    }

    // New method specifically for respawn reset (direct and immediate)
    public void ResetOffsetImmediate()
    {
        if (transposer != null)
        {
            transposer.FollowOffset = initialOffset;
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
        }
    }

    private IEnumerator SmoothTransition(Vector3 startOffset, Vector3 endOffset, float speed)
    {
        float timeElapsed = 0f;
        float duration = 1f / speed;

        while (timeElapsed < duration)
        {
            if (transposer != null)
            {
                transposer.FollowOffset = Vector3.Lerp(startOffset, endOffset, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            else
            {
                yield break;
            }
        }

        if (transposer != null)
        {
            transposer.FollowOffset = endOffset;
        }
        currentTransition = null;
    }
}
