using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mover : MonoBehaviour
{

    #region VARIABLES
    //Variables
    [SerializeField] float moveSpeed = 10.0f;
    #endregion

     //--------------------------------------------------------------------------------------

    #region START & UPDATE
    void Start()
    {
        PrintInstructions();
    }

    // Update is called once per frame
    void Update()
    {
        MovePlayer();
    }
    #endregion

    //--------------------------------------------------------------------------------------

    #region METHODS
    
    void PrintInstructions()
    {
        Debug.Log("Welcome to the game");
        Debug.Log("Move your player with WASD or arrow keys");
        Debug.Log("Don't hit the walls!");
    }

    void MovePlayer()
    {
        //Input
        float xValue = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float yValue=0;
        float zValue = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        //Move
        transform.Translate(xValue,yValue,zValue);
    }
    #endregion

     //--------------------------------------------------------------------------------------
}
