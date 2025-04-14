using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Collections;

public class WallOpacityController : MonoBehaviour
{
    private Transform player;
    private CinemachineCamera virtualCamera;

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private List<GameObject> transparentWalls = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(FindRequiredObjects());
    }

    private IEnumerator FindRequiredObjects()
    {
        while (true)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();

            if (player != null && virtualCamera != null)
            {
                Debug.Log("Player and CinemachineCamera found or re-found. Wall opacity control active.");
                break;
            }

            if (player == null)
            {
                Debug.Log("Waiting for Player to respawn...");
            }

            if (virtualCamera == null)
            {
                Debug.Log("Waiting for CinemachineCamera to become available...");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (player == null || virtualCamera == null)
        {
            StartCoroutine(FindRequiredObjects());
            return;
        }

        AdjustWallOpacity();
    }

    private void AdjustWallOpacity()
    {
        // Reset previously transparent walls
        for (int i = transparentWalls.Count - 1; i >= 0; i--)
        {
            if (transparentWalls[i] == null)
            {
                transparentWalls.RemoveAt(i);
            }
            else
            {
                ResetObjectOpacity(transparentWalls[i]);
            }
        }
        transparentWalls.RemoveAll(item => item == null);
        transparentWalls.Clear();

        if (player != null && virtualCamera != null)
        {
            Vector3 direction = player.position - virtualCamera.transform.position;
            RaycastHit[] hits = Physics.RaycastAll(virtualCamera.transform.position, direction, direction.magnitude);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    Transform parent = hit.collider.transform.parent;
                    if (parent != null)
                    {
                        SetParentOpacity(parent.gameObject, 0.2f);
                    }
                    else
                    {
                        SetSingleObjectOpacity(hit.collider.gameObject, 0.2f); // Separate function for single objects
                    }
                }
            }
        }
    }

    private void SetParentOpacity(GameObject parentObj, float opacity)
    {
        Renderer[] renderers = parentObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            SetObjectOpacity(renderer.gameObject, opacity);
            if (!transparentWalls.Contains(renderer.gameObject))
            {
                transparentWalls.Add(renderer.gameObject);
            }
        }
    }

    private void SetSingleObjectOpacity(GameObject obj, float opacity)
    {
        SetObjectOpacity(obj, opacity);
        if(!transparentWalls.Contains(obj))
        {
            transparentWalls.Add(obj);
        }
    }

    private void SetObjectOpacity(GameObject obj, float opacity)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalMaterials.ContainsKey(obj))
            {
                originalMaterials[obj] = renderer.material;
            }

            Material transparentMaterial = new Material(originalMaterials[obj]);
            renderer.material = transparentMaterial;

            if (transparentMaterial.GetFloat("_Surface") != 1.0f)
            {
                transparentMaterial.SetFloat("_Surface", 1.0f);
                transparentMaterial.SetFloat("_Blend", 0.0f);
                transparentMaterial.SetFloat("_ZWrite", 0.0f);
                transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                transparentMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            Color color = transparentMaterial.color;
            color.a = opacity;
            transparentMaterial.color = color;
        }
    }

    private void ResetObjectOpacity(GameObject obj)
    {
        if (obj != null && originalMaterials.ContainsKey(obj))
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = originalMaterials[obj];
            }
            originalMaterials.Remove(obj);
        }
    }
}