using UnityEngine;
using System.Collections.Generic;

public class GhostlyLightsSpawner : MonoBehaviour
{
    [Header("General Settings")]
    public int numberOfLights = 5;
    public float minSpawnRadius = 5f;
    public float maxSpawnRadius = 20f;
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 5f;

    [Header("Respawn Settings")]
    public bool enableRespawn = true;
    public float minLightLifetime = 5f;
    public float maxLightLifetime = 15f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.8f;

    [Header("Light Flicker Settings")]
    public float lightBaseIntensity = 0.8f;
    [Range(0f, 1f)] public float lightMinIntensityFactor = 0.1f;
    [Range(1f, 2f)] public float lightMaxIntensityFactor = 1.5f;
    public float lightMinFlickerSpeed = 0.02f;
    public float lightMaxFlickerSpeed = 0.15f;
    public float lightChangeSpeed = 8f;
    public LightType lightType = LightType.Point;
    public Color lightColor = new Color(0.7f, 0.8f, 1f, 1f);
    public float lightRange = 8f;
    [Range(1f, 179f)] public float spotAngle = 45f;
    public LightShadows shadows = LightShadows.Soft;
    public float lightInitialIntensityMultiplier = 0.7f;

    private List<GameObject> _activeLights = new List<GameObject>();
    private float _nextSpawnTime;

    void Start()
    {
        SpawnInitialLights();
        SetNextSpawnTime();
    }

    void Update()
    {
        if (enableRespawn)
        {
            if (Time.time >= _nextSpawnTime)
            {
                SpawnNewLight();
                SetNextSpawnTime();
            }

            for (int i = _activeLights.Count - 1; i >= 0; i--)
            {
                GameObject lightObject = _activeLights[i];
                if (lightObject == null)
                {
                    _activeLights.RemoveAt(i);
                    continue;
                }

                GhostlyLightLifetime lifetimeScript = lightObject.GetComponent<GhostlyLightLifetime>();
                Light lightComponent = lightObject.GetComponent<Light>();

                if (lifetimeScript != null && lightComponent != null)
                {
                    if (lifetimeScript.IsFadingOut())
                    {
                        float fadeProgress = (Time.time - lifetimeScript.fadeOutStartTime) / fadeOutDuration;
                        lightComponent.intensity = Mathf.Lerp(lifetimeScript.initialIntensityAtFadeOut, 0f, fadeProgress);
                        if (fadeProgress >= 1f)
                        {
                            Destroy(lightObject);
                            _activeLights.RemoveAt(i);
                        }
                    }
                    else if (lifetimeScript.HasReachedLifetime())
                    {
                        lifetimeScript.StartFadeOut(Time.time, fadeOutDuration, lightComponent.intensity);
                    }
                    else if (lifetimeScript.IsFadingIn())
                    {
                        float fadeProgress = (Time.time - lifetimeScript.fadeInStartTime) / fadeInDuration;
                        lightComponent.intensity = Mathf.Lerp(0f, lifetimeScript.targetFadeInIntensity, fadeProgress);
                        if (fadeProgress >= 1f)
                        {
                            lightComponent.intensity = lifetimeScript.targetFadeInIntensity;
                            lifetimeScript.isFadingIn = false;
                        }
                    }
                    else
                    {
                        FlickerLight(lightComponent, lightObject.GetComponent<GhostlyLightData>());
                    }
                }
            }

            if (_activeLights.Count < numberOfLights)
            {
                SpawnNewLight();
                SetNextSpawnTime();
            }
        }
        else
        {
            foreach (GameObject lightObject in _activeLights)
            {
                FlickerLight(lightObject.GetComponent<Light>(), lightObject.GetComponent<GhostlyLightData>());
            }
        }
    }

    void SpawnInitialLights()
    {
        for (int i = 0; i < numberOfLights; i++)
        {
            SpawnNewLight();
        }
    }

    void SpawnNewLight()
    {
        Vector3 randomPosition = GetRandomPositionAroundSpawner();
        GameObject lightObject = new GameObject("GhostlyLight");
        lightObject.transform.position = randomPosition;
        lightObject.transform.SetParent(transform);

        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = lightType;
        lightComponent.color = lightColor;
        lightComponent.range = lightRange;
        lightComponent.spotAngle = spotAngle;
        lightComponent.shadows = shadows;
        lightComponent.intensity = 0f;

        GhostlyLightData lightData = lightObject.AddComponent<GhostlyLightData>();
        lightData.baseIntensity = lightBaseIntensity * lightInitialIntensityMultiplier;
        lightData.minIntensityFactor = lightMinIntensityFactor;
        lightData.maxIntensityFactor = lightMaxIntensityFactor;
        lightData.minFlickerSpeed = lightMinFlickerSpeed;
        lightData.maxFlickerSpeed = lightMaxFlickerSpeed;
        lightData.changeSpeed = lightChangeSpeed;
        lightData.targetIntensity = lightData.baseIntensity * Random.Range(lightData.minIntensityFactor, lightData.maxIntensityFactor);
        lightData.nextFlickerTime = Time.time + Random.Range(lightData.minFlickerSpeed, lightData.maxFlickerSpeed);

        GhostlyLightLifetime lifetimeScript = lightObject.AddComponent<GhostlyLightLifetime>();
        lifetimeScript.lifetime = Random.Range(minLightLifetime, maxLightLifetime);
        lifetimeScript.StartFadeIn(Time.time, fadeInDuration, lightComponent, lightData.baseIntensity * lightInitialIntensityMultiplier);

        _activeLights.Add(lightObject);
    }

    Vector3 GetRandomPositionAroundSpawner()
    {
        Vector3 randomDirection = Random.insideUnitSphere;
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
        return transform.position + randomDirection * randomDistance;
    }

    void FlickerLight(Light lightComponent, GhostlyLightData lightData)
    {
        if (lightComponent == null || lightData == null) return;

        if (Time.time >= lightData.nextFlickerTime)
        {
            lightData.targetIntensity = lightData.baseIntensity * Random.Range(lightData.minIntensityFactor, lightData.maxIntensityFactor);
            lightData.nextFlickerTime = Time.time + Random.Range(lightData.minFlickerSpeed, lightData.maxFlickerSpeed);
        }

        lightComponent.intensity = Mathf.Lerp(lightComponent.intensity, lightData.targetIntensity, Time.deltaTime * lightData.changeSpeed);
    }

    void SetNextSpawnTime()
    {
        _nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}

public class GhostlyLightLifetime : MonoBehaviour
{
    [HideInInspector] public float lifetime;
    private float _startTime;
    private bool _isFadingOut = false;
    [HideInInspector] public float fadeOutStartTime;
    [HideInInspector] public float initialIntensityAtFadeOut;
    [HideInInspector] public bool isFadingIn = false; // Made public
    [HideInInspector] public float fadeInStartTime;
    [HideInInspector] public float targetFadeInIntensity;

    public void StartFadeOut(float time, float duration, float initialIntensity)
    {
        _isFadingOut = true;
        fadeOutStartTime = time;
        initialIntensityAtFadeOut = initialIntensity;
    }

    public bool IsFadingOut()
    {
        return _isFadingOut;
    }

    public bool HasReachedLifetime()
    {
        return Time.time - _startTime >= lifetime && !_isFadingOut && !isFadingIn;
    }

    public bool IsFadingIn() // Added this public method
    {
        return isFadingIn;
    }

    public void StartFadeIn(float time, float duration, Light light, float targetIntensity)
    {
        isFadingIn = true;
        fadeInStartTime = time;
        targetFadeInIntensity = targetIntensity;
    }
}

public class GhostlyLightData : MonoBehaviour
{
    [HideInInspector] public float baseIntensity;
    [HideInInspector] public float minIntensityFactor;
    [HideInInspector] public float maxIntensityFactor;
    [HideInInspector] public float minFlickerSpeed;
    [HideInInspector] public float maxFlickerSpeed;
    [HideInInspector] public float changeSpeed;
    [HideInInspector] public float targetIntensity;
    [HideInInspector] public float nextFlickerTime;
}