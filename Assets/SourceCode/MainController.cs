using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controller for  the main game logic, including player movement and camera control.
/// </summary>
public class MainController : MonoBehaviour
{
    public Camera MainCamera;
    public CameraController cameraController;
    public TheWorld theWorld;
    public ThePlayer thePlayer;

    public float moveSpeed = 5f;       // Speed at which the player moves
    public float smoothTime = 0.1f;    // Time for movement smoothing

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;


    // Start is called before the first frame update
    void Awake()
    {
        theWorld = FindObjectOfType<TheWorld>();
        thePlayer = FindObjectOfType<ThePlayer>();
        cameraController = FindObjectOfType<CameraController>();
        MainCamera = Camera.main;

        targetPosition = thePlayer.transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        ProcessMouseEvents();

        // If Space is pressed, move camera to player
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cameraController.TeleportCamera(thePlayer.transform.position);

        }
        // Check if any key is pressed
        else if (Input.anyKeyDown)
        {
            // Check input type
            // for player movement when every time WSAD is pressed, 
            // the player moves 1 unit in the direction
            // Check for continuous movement in the WSAD directions
            if (Input.GetKey(KeyCode.W))
            {
                thePlayer.MovePlayer(Vector3.up);
                cameraController.MoveCamera(Vector3.up);
                theWorld.RespondToMovement(0);
                theWorld.RespondToMovement(2);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                thePlayer.MovePlayer(Vector3.down);
                cameraController.MoveCamera(Vector3.down);
                theWorld.RespondToMovement(2);
                theWorld.RespondToMovement(0);

            }
            else if (Input.GetKey(KeyCode.A))
            {
                thePlayer.MovePlayer(Vector3.left);
                cameraController.MoveCamera(Vector3.left);
                theWorld.RespondToMovement(3);
                theWorld.RespondToMovement(1);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                thePlayer.MovePlayer(Vector3.right);
                cameraController.MoveCamera(Vector3.right);
                theWorld.RespondToMovement(1);
                theWorld.RespondToMovement(3);
            }


            // Register that input was received
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                theWorld.InputReceived();
            }
        }
    }


    bool MouseSelectObjectAt(out GameObject g, out Vector3 p, int layerMask)
    {
        Vector2 mousePos = MainCamera.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, layerMask);
        Debug.Log("MouseSelect:" + layerMask + " Hit=" + hit);
        if (hit.collider != null)
        {
            g = hit.transform.gameObject;
            p = hit.point;
            return true;
        }
        else
        {
            g = null;
            p = Vector3.zero;
            return false;
        }
    }

    void ProcessMouseEvents()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;  // do not attempt to do anything if over UI elements
                     // UI includes buttons, sliders, etc.

        else if (Input.GetMouseButtonDown(0))
        {
            GameObject g;
            Vector3 p;
            // if (MouseSelectObjectAt(out g, out p, LayerMask.GetMask("WFGridLayer")))
            // {

            //     Debug.Log("Clicked on WFGridLayer");
            // }
            if (MouseSelectObjectAt(out g, out p, LayerMask.GetMask("TileLayer")))
            {
                Tile t = g.GetComponent<Tile>();
                if (t != null)
                {
                    thePlayer.TeleportPlayer(p);
                    theWorld.InputReceived();
                }
            }
        }
        // If rightclick drag, move camera
        else if (Input.GetMouseButton(1))
        {
            Vector3 move = new Vector3(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0);
            cameraController.TranslateCamera(move);
            return;
        }

        // If mouse scroll, zoom in/out
        else if (Input.mouseScrollDelta.y != 0)
        {
            cameraController.ZoomCamera(Input.mouseScrollDelta.y);
        }
        // let up arrow be equivalent to zoom in
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            cameraController.ZoomCamera(1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            cameraController.ZoomCamera(-1);
        }
    }
}
