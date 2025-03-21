using System.Collections;
using UnityEngine;
/// <summary>
/// Controls the main camera, including movement, zooming, and boundary handling.
/// </summary>
public class CameraController : MonoBehaviour
{
    public Camera MainCamera;
    public GameObject upBoundary, downBoundary, leftBoundary, rightBoundary;
    public TheWorld theWorld;
    public float defaultZ = -8;
    public int defaultSize = 5;
    private float viewWidth, viewHeight;
    int dimension = 0;
    void Awake()
    {
        MainCamera = Camera.main;
        InitializeCamera();
        UpdateToWorld();
    }

    /// <summary>
    /// Initializes the camera based on the world's dimensions.
    /// </summary>
    public void InitializeCamera()
    {
        dimension = theWorld.GetDimension();
        float axis = dimension / 2.0f;
        MainCamera.transform.position = new Vector3(axis, axis, defaultZ);
        SetCameraViewWidth(axis * 9); // Adjust view based on world size
    }

    /// <summary>
    /// Moves the camera by a given direction.
    /// </summary>
    public void MoveCamera(Vector3 direction)
    {
        MainCamera.transform.position += direction;
    }

    /// <summary>
    /// Instantly moves the camera to a specified position, optionally resetting zoom.
    /// </summary>
    public void TeleportCamera(Vector3 position, bool resetZoom = true)
    {
        MainCamera.transform.position = new Vector3(position.x, position.y, defaultZ);
        if (resetZoom) { MainCamera.orthographicSize = defaultSize; }
    }

    /// <summary>
    /// Adjusts the zoom level while maintaining a minimum size limit.
    /// </summary>
    public void ZoomCamera(float deltaV)
    {
        float currentWidth = MainCamera.orthographicSize * 2 * MainCamera.aspect;
        float newWidth = Mathf.Max(currentWidth - deltaV, 1f); // Prevents zooming in too much
        SetCameraViewWidth(newWidth);

        UpdateToWorld();
    }

    /// <summary>
    /// Moves the camera smoothly in a given direction.
    /// </summary>
    public void TranslateCamera(Vector3 move)
    {
        MainCamera.transform.Translate(move);
    }


    /// <summary>
    /// Sets the camera's view width in world units and updates boundary positions.
    /// </summary>
    public void SetCameraViewWidth(float newWidth)
    {
        viewWidth = newWidth;
        float aspectRatio = MainCamera.aspect;
        MainCamera.orthographicSize = newWidth / (2 * aspectRatio);

        viewHeight = CalcHeightFromWidth(newWidth);

        // Update boundary positions
        upBoundary.transform.localPosition = new Vector3(0, viewHeight / 2 + dimension, 0);
        downBoundary.transform.localPosition = new Vector3(0, -viewHeight / 2 - dimension, 0);
        leftBoundary.transform.localPosition = new Vector3(-viewWidth / 2 - dimension, 0, 0);
        rightBoundary.transform.localPosition = new Vector3(viewWidth / 2 + dimension, 0, 0);

    }

    private void UpdateToWorld()
    {
        theWorld.SetW(viewWidth);
        theWorld.SetV(viewHeight);
        for (int i = 0; i < 4; i++)
        {
            theWorld.RespondToMovement(i);
        }
        theWorld.UpdateUI();
    }

    /// <summary>
    /// Calculates the view height based on the given view width and aspect ratio.
    /// </summary>
    public float CalcHeightFromWidth(float viewWidth)
    {
        return viewWidth / MainCamera.aspect;
    }
}
