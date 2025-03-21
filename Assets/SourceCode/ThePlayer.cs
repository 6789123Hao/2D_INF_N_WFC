using UnityEngine;

/// <summary>
/// Controls player movement, rotation, and automated walking logic.
/// </summary>
public class ThePlayer : MonoBehaviour
{
    //Speed = 1 unit per second
    public int MaxSteps = 1000, totalSteps = 0;
    public float moveSpeed = 10f;       // Speed at which the player moves
    public float smoothTime = 0.1f;    // Time for movement smoothing
    [SerializeField] private float clippedZ = -4;
    [SerializeField] private GameObject myRotatingPiece;
    float rotationSpeed = 90f;

    //rigidbody
    private Rigidbody2D rb;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    // Reference to the world
    public TheWorld theWorld;
    public float overlapRadius = 0.5f; // Adjust based on the size of your tiles/player

    // LayerMask to ensure we only check for tiles
    public LayerMask tileLayerMask;


    // Start is called before the first frame update
    void Start()
    {
        // Tile script
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;  // Ensure tile colliders are set as triggers
        }

        rb = GetComponent<Rigidbody2D>();
        InitializePlayer();
    }

    /// <summary>
    /// Initializes player position and step counter.
    /// </summary>
    public void InitializePlayer()
    {
        transform.position = new Vector3(0, 0, clippedZ);
        targetPosition = transform.position;
        totalSteps = 0;
    }

    /// <summary>
    /// Moves the player to the center of the world based on its dimensions.
    /// </summary>
    public void MoveHalfDimension()
    {
        float axis = theWorld.GetDimension() / 2.0f;
        transform.position = new Vector3(axis, axis, clippedZ);
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        RotatePiece();
    }

    /// <summary>
    /// Rotates the assigned piece continuously.
    /// </summary>
    private void RotatePiece()
    {
        myRotatingPiece.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        if (myRotatingPiece.transform.eulerAngles.z >= 360)
        {
            myRotatingPiece.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }


    /// <summary>
    /// Moves the player in a specified direction.
    /// </summary>
    public void MovePlayer(Vector3 direction)
    {
        transform.position += direction;
    }

    /// <summary>
    /// Instantly moves the player to a specified position.
    /// </summary>
    public void TeleportPlayer(Vector3 pos)
    {
        pos.z = clippedZ; // Ensure player remains on the correct Z-axis level
        transform.position = pos;
        targetPosition = pos;
    }


    //===============AutoWalk================


    /// <summary>
    /// Handles automated movement based on direction and distance.
    /// </summary>
    public void WalkOnce(int dimension, int direction, float distance = 1)
    {
        totalSteps++;
        int distanceInt = (int)distance;
        (int x, int y) pos = ((int)transform.position.x, (int)transform.position.y);

        pos = direction switch
        {
            0 => (pos.x, pos.y + distanceInt), // Up
            1 => (pos.x + distanceInt, pos.y + distanceInt), // Up-right
            2 => (pos.x + distanceInt, pos.y), // Right
            3 => (pos.x + distanceInt, pos.y - distanceInt), // Down-right
            4 => (pos.x, pos.y - distanceInt), // Down
            5 => (pos.x - distanceInt, pos.y - distanceInt), // Down-left
            6 => (pos.x - distanceInt, pos.y), // Left
            7 => (pos.x - distanceInt, pos.y + distanceInt), // Up-left
            8 => SpiralMovement(totalSteps, dimension), // Spiral movement
            _ => pos
        };

        TeleportPlayer(new Vector3(pos.x, pos.y, clippedZ));
    }

    /// <summary>
    /// Calculates a spiral movement pattern.
    /// </summary>
    public (int, int) SpiralMovement(int n, int dimension)
    {
        /*@MISC {163101,
        TITLE = {On a two dimensional grid is there a formula I can use to spiral coordinates in an outward pattern?},
        AUTHOR = {lhf (https://math.stackexchange.com/users/589/lhf)},
        HOWPUBLISHED = {Mathematics Stack Exchange},
        NOTE = {URL:https://math.stackexchange.com/q/163101 (version: 2012-06-26)},
        EPRINT = {https://math.stackexchange.com/q/163101},
        URL = {https://math.stackexchange.com/q/163101}
}*/
        int distance = dimension / 2;
        int k = (int)Mathf.Ceil((Mathf.Sqrt(n) - 1) / 2);
        int t = 2 * k + 1;
        int m = t * t;
        t -= 1;
        int x, y;

        if (n >= m - t)
        {
            x = k - (m - n);
            y = -k;
        }
        else
        {
            m -= t;
            if (n >= m - t)
            {
                x = -k;
                y = -k + (m - n);
            }
            else
            {
                m -= t;
                if (n >= m - t)
                {
                    x = -k + (m - n);
                    y = k;
                }
                else
                {
                    x = k;
                    y = k - (m - n - t);
                }
            }
        }
        // Scale the coordinates by the radius to create spacing
        return (x * distance, y * distance);
    }


    //=============== Collision Handling ==================

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Tile"))
        {
            // Handle optional logic when player enters a tile
        }
    }
}
