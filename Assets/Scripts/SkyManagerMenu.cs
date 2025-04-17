using UnityEngine;

public class SkyManagerMenu : MonoBehaviour
{
    //Skybox velocity
    public float speed = 0.1f;

    void Update()
    {
        //Move the skybox
        
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * speed);
    }
}