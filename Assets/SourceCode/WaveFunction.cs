using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using System.IO;

/// <summary>
/// Implements the Wave Function Collapse algorithm for procedural map generation.
/// </summary>
public class WaveFunction : MonoBehaviour
{
    [SerializeField] private Grid _grid;
    public float gridStartTime = -1f, gridEndTime = -1f, gridGenMillisecs;
    public bool gridComplete = false, neighborsComplete = false, usingBacktrack = false, usingDelay = false;

    public int collapsedCount = 0, totalCellCount = 0, fallBackIterations = 0, backtrackLimit = 10;
    private int backtrackCoroutineCalled = 0;
    public int gridX, gridY;

    // backtracking tree
    public DecisionTreeNode decisionTreeRoot;
    public DecisionTreeNode currentNode;

    public Cell currentProblematicCell;

    //Hashset of Problematic cells and their regenerated grid count
    public Dictionary<Cell, int> problematicCells = new Dictionary<Cell, int>();

    public TheWorld theWorld;
    public ThePlayer thePlayer;
    public float distanceFromPlayer, extendDistance;
    private GameObject CellCollect, DumpCollect;
    // private GameObject TileCollect;
    // public GameObject WFPrefab;
    public float secPerStep;
    public int dimension, posOffset;
    public Tile[] tileOptionsAll;// possible tiles
    // public GameObject thisWFGridObj;
    public Cell[,] theGrid; // the grid static positions
    public List<Cell> gridComponents; // the grid waiting to be populated
    public Cell cellObj; // the cell prefab

    private System.Random rng; //  Random number generator

    public WaveFunction ParentGrid;

    public LayerMask gridLayer;

    [Header("Neighbor Grid Information")]
    // Now we have 8 directions: up, right, down, left, Top-right // Bottom-right // Bottom-left// Top-left
    public WaveFunction[] neighborsWFCTopCW = new WaveFunction[8];
    // The two edges for neighbor grids to read from
    [Header("Neighbor Edges")]
    public Cell[] Nbor_CornerCells = null; // NBor from Top-right, Bottom-right, Bottom-left, Top-left
    public Cell[] NBTop = null, NBRight = null;
    public Cell[] NBBottom = null, NBLeft = null;
    public Cell[][] Nbor_EdgeCells = new Cell[4][]; // NBor from Top, Right, Bottom, Left   
    [Header("My Edge Cells")]
    public Cell[] myCornerCells = new Cell[4]; // Top-right, Bottom-right, Bottom-left, Top-left
    public Cell[] myTopRow, myRightColumn, myBottomRow, myLeftColumn;



    public WaveFunction(int dimensions, Tile[] tileObjects, Cell cellObj, TheWorld theWorld = null)
    {
        this.dimension = dimensions;
        this.tileOptionsAll = tileObjects;
        this.cellObj = cellObj;
        this.theWorld = theWorld;
    }
    public class CellHistoryState
    {
        public Cell cell;
        public Tile[] previousOptions;  // Options before the collapse
        public Tile selectedTile;       // Option selected during collapse

        public CellHistoryState(Cell cell, Tile[] previousOptions, Tile selectedTile)
        {
            this.cell = cell;
            this.previousOptions = previousOptions;
            this.selectedTile = selectedTile;
        }

        public void RemoveSelectedTileFromOptions()
        {
            List<Tile> temp = new List<Tile>(previousOptions);
            try
            {
                temp.Remove(selectedTile);
            }
            catch (Exception e)
            {
                Debug.LogError("Error removing tile: " + e.Message);
            }
            previousOptions = temp.ToArray();
        }
    }

    // public List<CellHistoryState> stateHistory = new List<CellHistoryState>();
    private Coroutine generationCoroutine;


    void Awake()
    {
        // Set layer to layer "WFGridLayer"
        gameObject.layer = LayerMask.NameToLayer("WFGridLayer");
        // Debug.Log("Layer set to " + gameObject.layer);
        gridLayer = LayerMask.GetMask("WFGridLayer");

        // Load at Assets/Resources/Prefabs/MapGen
        // WFPrefab = Resources.Load<GameObject>("Prefabs/MapGen");

        thePlayer = FindObjectOfType<ThePlayer>();
        Debug.Assert(thePlayer != null, "ThePlayer script not found");

        neighborsWFCTopCW = new WaveFunction[8];
        // thisWFGridObj = this.gameObject;
        // stateHistory = new List<CellHistoryState>();


        Vector3 tempInt3D = _grid.transform.position;
        // Debug.Log($"Grid transform position: {tempInt3D.x}, {tempInt3D.y}");
        gridX = (int)tempInt3D.x;
        gridY = (int)tempInt3D.y;
        // Debug.Log($"Grid transform Int position: {gridX}, {gridY}");
        Vector2Int tempInt2D = (Vector2Int)_grid.WorldToCell(transform.position);
        // Debug.Log($"Grid WorldToCell: {tempInt2D.x}, {tempInt2D.y}");

        Vector3 tempLocal3D = _grid.CellToLocal((Vector3Int)tempInt2D);
        // Debug.Log($"Grid CellToLocal: {tempLocal3D.x}, {tempLocal3D.y}");


        gridStartTime = -1f;
    }

    void GetWorldInfo()
    {
        if (theWorld == null)
        {
            theWorld = FindObjectOfType<TheWorld>();
            Debug.Log("TheWorld script not found");
        }
        Debug.Assert(theWorld != null, "TheWorld script not found");
        dimension = theWorld.GetDimension();
        posOffset = dimension / 2;
        backtrackLimit = theWorld.GetBacktrackLimit();
        usingBacktrack = theWorld.GetUseBacktrack();
        tileOptionsAll = theWorld.GetTileOptions();
        usingDelay = theWorld.GetIsStepByStep();
        // Check if seeding is enabled in the world and set up the RNG
        if (theWorld.IsSeedingEnabled())
        {
            int seed = theWorld.GetSeed();
            rng = new System.Random(seed);
        }
        else
        {
            rng = new System.Random(); // Use default random if seeding is not enabled
        }

    }


    [ContextMenu("Initialize")]
    public void Initialize()
    {
        decisionTreeRoot = new DecisionTreeNode(null, tileOptionsAll, null);
        currentNode = decisionTreeRoot;

        InitializeGrid();
        NeighborInteractions();
    }

    // Initialize with a reference to TheWorld and grid position
    public void InitializeWithGridPosition(TheWorld world, Vector2Int gridPosition)
    {
        this.theWorld = world;
        this.gridX = gridPosition.x;
        this.gridY = gridPosition.y;

        // Perform additional setup for your WaveFunction

    }

    public void InitializeGridProperties(int dimensions, Cell cellObj, float secToWait, TheWorld theWorld = null)
    {
        this.theWorld = theWorld;
        GetWorldInfo();
        this.dimension = dimensions;
        this.cellObj = cellObj;
        this.secPerStep = secToWait;
        totalCellCount = dimension * dimension;

        // thisWFGridObj = this.gameObject;

        SetCollections();
        InitializeNeighborEdgesNulls();
    }

    void InitializeGrid()
    {
        CreateEmptyGrid();
        StoreMyEdgeCells();
        CreateNxNCells(dimension);
    }

    public void NeighborInteractions()
    {


        dimension = theWorld.GetDimension();
        DetectNeighbors();

        GridOverWriteWithEdges();
        StoreMyEdgeCells();

    }

    [ContextMenu("RunWFC")]
    public void RunWFC()
    {
        if (gridStartTime == -1f)
        {
            gridStartTime = Time.realtimeSinceStartup;
        }
        // Debug.Log("Started " + gridStartTime.ToString());
        InsertCurrentGridToWFCList();
        ValidateAllUnCollapsedCells();
        StartCellSelection();
    }


    [ContextMenu("StartCellSelection")]
    void StartCellSelection()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);

        }
        // Start the entropy check and potential collapse
        generationCoroutine = StartCoroutine(CheckEntropy());
    }

    // Detect neighbors in all eight directions using grid positions
    public void DetectNeighbors()
    {
        WaveFunction[] temp = new WaveFunction[8];
        temp = theWorld.GetNeighborWaveFunctions(transform.position);
        for (int i = 0; i < temp.Length; i++)
        {
            if (neighborsWFCTopCW[i] != temp[i])
            {
                neighborsWFCTopCW[i] = temp[i];
                temp[i].neighborsWFCTopCW[(i + 4) % 8] = this;
                LinkNeighbor(i, temp[i]);
            }
        }
    }

    [ContextMenu("FindNeighbors")]
    private void FindNeighbors()
    {
        // Define cardinal and diagonal directions
        Vector3[] directions = new Vector3[] {
            Vector3.up,                            // 0: Up
            Vector3.up + Vector3.right,            // 1: Up-Right
            Vector3.right,                         // 2: Right
            Vector3.down + Vector3.right,          // 3: Down-Right
            Vector3.down,                          // 4: Down
            Vector3.down + Vector3.left,           // 5: Down-Left
            Vector3.left,                          // 6: Left
            Vector3.up + Vector3.left              // 7: Up-Left
        };

        float[] rayLengths = new float[] {
            (dimension) , // Up
            (dimension) * (Mathf.Sqrt(2)) , // Up-Right
        };

        // Check all directions
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];
            RaycastHit hitInfo;
            Vector3 startPos = transform.position + (direction * posOffset);

            // Debug.DrawRay(startPos, direction * rayLengths[i % 2], Color.blue, 10f);
            bool hit = Physics.Raycast(startPos, direction, out hitInfo, rayLengths[i % 2], gridLayer);
            if (hit)
            {
                WaveFunction neighbor = hitInfo.collider.gameObject.GetComponent<WaveFunction>();
                // Debug.Log(this.name + " Neighbor found: " + neighbor.name);
                if (neighbor == this)
                {
                    continue;
                }
                if (neighborsWFCTopCW[i] == neighbor)
                {
                    // Debug.Log("Neighbor already set.");
                    continue;
                }
                neighborsWFCTopCW[i] = neighbor;
                if (neighbor != null)
                {
                    // Update the opposite direction neighbor
                    neighbor.neighborsWFCTopCW[(i + 4) % 8] = this;
                    LinkNeighbor(i, neighbor);
                    // ReplaceMyCellsWithNeighbors(i, neighbor);
                }
            }
        }
    }

    // Link the neighbors to each other
    // Put the edge cells of the neighbors in the corresponding opposite arrays
    // put the corner cells of the neighbors in the corresponding opposite arrays
    public void LinkNeighbor(int direction, WaveFunction neighbor)
    {
        // Debug.Log("Linking neighbor: " + neighbor.name + " from " + this.name + " in direction: " + direction);
        // For edges, if they have cells, we will take their cells and put them in our edge arrays, grid and list
        // if they don't have cells, we will provide our cell to their array only
        // For corners, if they have cells, we will take their cells and put them in our corner arrays, grid and list
        switch (direction)
        {
            case 0:
                NBTop = neighbor.myBottomRow;
                Nbor_CornerCells[0] = NBTop[dimension - 1];
                Nbor_CornerCells[3] = NBTop[0];
                break;
            case 2:
                NBRight = neighbor.myLeftColumn;
                Nbor_CornerCells[0] = NBRight[dimension - 1];
                Nbor_CornerCells[1] = NBRight[0];
                break;
            case 4:
                NBBottom = neighbor.myTopRow;
                Nbor_CornerCells[1] = NBBottom[dimension - 1];
                Nbor_CornerCells[2] = NBBottom[0];
                break;
            case 6:
                NBLeft = neighbor.myRightColumn;
                Nbor_CornerCells[2] = NBLeft[0];
                Nbor_CornerCells[3] = NBLeft[dimension - 1];
                break;

            // For corners, we will take their corner cells and put them in our corner arrays, grid and list
            case 1: // my top right, their bottom left
                if (Nbor_CornerCells[0] == null)
                    Nbor_CornerCells[0] = neighbor.myCornerCells[2];
                break;
            case 3: // my bottom right[1], their top left[3]
                if (Nbor_CornerCells[1] == null)
                    Nbor_CornerCells[1] = neighbor.myCornerCells[3];
                break;
            case 5: // my bottom left [2], their top right[0]
                if (Nbor_CornerCells[2] == null)
                    Nbor_CornerCells[2] = neighbor.myCornerCells[0];
                break;
            case 7: // my top left[3], their bottom right[1]
                if (Nbor_CornerCells[3] == null)
                    Nbor_CornerCells[3] = neighbor.myCornerCells[1];
                break;
            default:
                // Debug.LogError("Unknown direction: " + direction);
                break;
        }
    }


    // if there is any NeighborEdge, we overwrite our edge with their edge, 
    //remove the our existing cell from grid && cellCollect and add their cell to our grid && cellCollect, 
    //update the gridComponents in the end

    // if there is no any NeighborEdge, we check NeighborCorner
    // if there is any NeighborCorner, we overwrite our corner with their corner,
    // remove the our existing cell from grid && cellCollect and add their cell to our grid && cellCollect,
    // update the gridComponents in the end

    // if there no any NeighborCorner, do nothing
    // Check and overwrite edges with neighbor's edge cells if available
    void GridOverWriteWithEdges()
    {
        if (NBTop == null && NBRight == null && NBBottom == null && NBLeft == null)
        {
        }
        else
        {
            if (NBTop != null)
            {
                // Debug.Log("Overwriting top edge with neighbor's top edge.");
                for (int i = 0; i < dimension; i++)
                {
                    RemoveMyOverlappingCellinGrid(NBTop[i], myTopRow[i], i, dimension - 1);
                    myTopRow[i] = NBTop[i];

                }
                // NBTop = null;
                myCornerCells[0] = myTopRow[dimension - 1];
                myCornerCells[3] = myTopRow[0];
            }

            if (NBRight != null)
            {
                // Debug.Log("Overwriting right edge with neighbor's right edge.");
                for (int i = 0; i < dimension; i++)
                {
                    RemoveMyOverlappingCellinGrid(NBRight[i], myRightColumn[i], dimension - 1, i);
                    myRightColumn[i] = NBRight[i];
                }
                // NBRight = null;
                myCornerCells[0] = myRightColumn[0];
                myCornerCells[1] = myRightColumn[dimension - 1];
            }

            if (NBBottom != null)
            {
                // Debug.Log("Overwriting bottom edge with neighbor's bottom edge.");
                for (int i = 0; i < dimension; i++)
                {
                    RemoveMyOverlappingCellinGrid(NBBottom[i], myBottomRow[i], i, 0);
                    myBottomRow[i] = NBBottom[i];
                }
                // NBBottom = null;
                myCornerCells[1] = myBottomRow[0];
                myCornerCells[2] = myBottomRow[dimension - 1];
            }

            if (NBLeft != null)
            {
                // Debug.Log("Overwriting left edge with neighbor's left edge.");
                for (int i = 0; i < dimension; i++)
                {
                    RemoveMyOverlappingCellinGrid(NBLeft[i], myLeftColumn[i], 0, i);
                    myLeftColumn[i] = NBLeft[i];
                }
                // NBLeft = null;
                myCornerCells[2] = myLeftColumn[0];
                myCornerCells[3] = myLeftColumn[dimension - 1];
            }
        }


        // If no edges to overwrite, check for corners
        if (Nbor_CornerCells[0] != null && myCornerCells[0] == null)
        {
            RemoveMyOverlappingCellinGrid(Nbor_CornerCells[0], myCornerCells[0], dimension - 1, dimension - 1);
        }

        if (Nbor_CornerCells[1] != null && myCornerCells[1] == null)
        {
            RemoveMyOverlappingCellinGrid(Nbor_CornerCells[1], myCornerCells[1], dimension - 1, 0);
        }

        if (Nbor_CornerCells[2] != null && myCornerCells[2] == null)
        {
            RemoveMyOverlappingCellinGrid(Nbor_CornerCells[2], myCornerCells[2], 0, 0);
        }

        if (Nbor_CornerCells[3] != null && myCornerCells[3] == null)
        {
            RemoveMyOverlappingCellinGrid(Nbor_CornerCells[3], myCornerCells[3], 0, dimension - 1);
            // Debug.Log("Overwriting top-left corner with neighbor's top-left corner.");
            // RemoveMyOverlappingCell(myCornerCells[3], Nbor_CornerCells[3], 0, dimension - 1);
            // myCornerCells[3] = Nbor_CornerCells[3];
        }

        RemoveDumpCollect();
        // Update the gridComponents list after modifications
        InsertCurrentGridToWFCList();
    }


    // Check if nulls
    // x and y are the positions of the cell in the my grid
    // this method welcomes the old cell as theGrid[x,y] and move newly generated cell to DumpCollect
    void RemoveMyOverlappingCellinGrid(Cell oldCell, Cell myRedundentCell, int x, int y)
    {
        if (oldCell == null)
        {
            // Debug.LogError("Old cell is null.");
            return;
        }

        if (myRedundentCell == null)
        {
            // Debug.LogError("My redundant cell is null.");
        }

        if (oldCell == myRedundentCell)
        {
            // Debug.LogError("Old cell and my redundant cell are the same." + oldCell.name + this.name);
            return;
        }

        // Remove my redundant cell from the grid and the list
        theGrid[x, y] = oldCell;
        gridComponents.Remove(myRedundentCell);
        myRedundentCell.transform.parent = DumpCollect.transform;
        oldCell.transform.parent = CellCollect.transform;

        // Debug.Log($"{oldCell.name} at position ({x},{y}) has local position {oldCell.transform.localPosition}");

    }


    //Ask neighbors for their edge cells
    private void AskNeighborsForEdges()
    {
        for (int i = 0; i < 4; i++)
        {
            if (neighborsWFCTopCW[i * 2] != null)
            {
                // Debug.Log("Asking neighbor for edge: " + i);
                neighborsWFCTopCW[i * 2].ProvideMyEdgeToNeighbor(i * 2, this);
            }
        }
    }

    private void ProvideMyEdgeToNeighbor(int direction, WaveFunction newNeighbor)
    {
        switch (direction)
        {
            case 0:
                newNeighbor.AcceptNewRow(myBottomRow, 0);
                break;
            case 1:
                newNeighbor.AcceptNewCorner(myCornerCells[0], 1);
                break;
            case 2:
                newNeighbor.AcceptNewRow(myLeftColumn, 2);
                break;
            case 3:
                newNeighbor.AcceptNewCorner(myCornerCells[3], 3);
                break;
            case 4:
                newNeighbor.AcceptNewRow(myTopRow, 4);
                break;
            case 5:
                newNeighbor.AcceptNewCorner(myCornerCells[2], 5);
                break;
            case 6:
                newNeighbor.AcceptNewRow(myRightColumn, 6);
                break;
            case 7:
                newNeighbor.AcceptNewCorner(myCornerCells[1], 7);
                break;
            default:
                Debug.LogError("Unknown direction: " + direction);
                break;
        }

    }


    private void SetCollections()
    {
        // Create an empty gameobject and call it CellCollect
        CellCollect = new GameObject();
        CellCollect.name = "CellCollect";
        DumpCollect = new GameObject();
        DumpCollect.name = "DumpCollect";
        // Set the parent of the CellCollect to the WaveFunction object
        CellCollect.transform.parent = this.transform;
        DumpCollect.transform.parent = this.transform;

        // posOffset = theWorld.GetDimension() / 2;
        posOffset = 0;
        // Place them with negative offset from the WaveFunction object
        CellCollect.transform.localPosition = new Vector3(-posOffset, -posOffset, 0);
        DumpCollect.transform.localPosition = CellCollect.transform.localPosition;
    }

    void InitializeNeighborEdgesNulls()
    {
        Nbor_EdgeCells = new Cell[4][];
        NBBottom = null;
        NBLeft = null;
        NBRight = null;
        NBTop = null;
        Nbor_CornerCells = new Cell[4];
    }

    private void CreateEmptyGrid()
    {
        theGrid = new Cell[dimension, dimension];
        myCornerCells = new Cell[4];
        for (int i = 0; i < dimension; i++)
        {
            for (int j = 0; j < dimension; j++)
            {
                theGrid[i, j] = null;
            }
        }
        myTopRow = new Cell[dimension];
        myBottomRow = new Cell[dimension];
        myLeftColumn = new Cell[dimension];
        myRightColumn = new Cell[dimension];
        NBTop = null;
        NBRight = null;
        NBBottom = null;
        NBLeft = null;
    }

    // Store the edge cells in corresponding arrays: myTopRow, myBottomRow, myLeftColumn, myRightColumn
    // Store the corner cells in myCornerCells array: Top-right, Bottom-right, Bottom-left, Top-left
    private void StoreMyEdgeCells()
    {
        for (int i = 0; i < dimension; i++)
        {
            myTopRow[i] = theGrid[i, dimension - 1];
            myBottomRow[i] = theGrid[i, 0];
            myLeftColumn[i] = theGrid[0, i];
            myRightColumn[i] = theGrid[dimension - 1, i];
        }
        myCornerCells[0] = theGrid[dimension - 1, dimension - 1]; // Top-right
        myCornerCells[1] = theGrid[dimension - 1, 0]; // Bottom-right
        myCornerCells[2] = theGrid[0, 0]; // Bottom-left
        myCornerCells[3] = theGrid[0, dimension - 1]; // Top-left
    }

    // strictly create NxN cells, stores edge and corner cells for future use
    private void CreateNxNCells(int n)
    {
        if (theGrid == null || CellCollect == null)
        {
            Debug.LogError("theGrid or CellCollect is not initialized.");
            return;
        }

        // create populate grid with new cells
        for (int i = 0; i < dimension; i++)
        {
            for (int j = 0; j < dimension; j++)
            {
                Cell newCell = Instantiate(cellObj, new Vector3(i, j, 0), quaternion.identity);
                if (newCell == null)
                {
                    Debug.LogError("Failed to instantiate cell at position (" + i + ", " + j + ").");
                    continue;
                }
                newCell.CreateCell(false, tileOptionsAll);
                newCell.name = "Cell" + theWorld.cellCount;
                theWorld.cellCount++;
                theGrid[i, j] = newCell;
                newCell.transform.parent = CellCollect.transform;
                newCell.transform.localPosition = new Vector3(i, j, 0);
            }
        }

        // store the edge cells in corresponding arrays: myTopRow, myBottomRow, myLeftColumn, myRightColumn
        StoreMyEdgeCells();
    }

    public void InsertCurrentGridToWFCList()
    {
        collapsedCount = 0;
        fallBackIterations = 0;
        backtrackCoroutineCalled = 0;
        // add the complete grid to the gridComponents list
        if (gridComponents == null)
        {
            gridComponents = new List<Cell>();
        }
        else
        {
            gridComponents.Clear();
        }
        for (int i = 0; i < dimension; i++)
        {
            for (int j = 0; j < dimension; j++)
            {
                if (theGrid[i, j] != null)
                {
                    gridComponents.Add(theGrid[i, j]);
                }
            }
        }
    }



    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = gridComponents.Where(x => !x.isCollapsed).ToList();
        if (tempGrid.Count == 0) // If all cells are collapsed, return
        {
            gridComplete = true;
            // CheckNeighborCompleteness();
            // Debug.Log("All cells are collapsed on " + this.name);
            yield break;
        }

        // get the cells with the smallest number of options
        // Cell cellToCollapse = ChooseCellWithLeastEntropy(tempGrid);
        Cell cellToCollapse = ChooseRandomWithLeastEntropy(tempGrid);

        if (cellToCollapse != null)
        {
            if (usingDelay)
            {
                yield return new WaitForSeconds(secPerStep);
            }
            CollapseCell(cellToCollapse);
        }

        if (cellToCollapse.tileOptions.Length == 0)
        {
            currentProblematicCell = cellToCollapse;
            BacktrackWithStack();
        }
    }

    private void SortByTileOptionsLength(List<Cell> tempGrid)
    {
        // Sort the list by the length of the tileOptions array, with least options first
        tempGrid.Sort((x, y) => x.tileOptions.Length.CompareTo(y.tileOptions.Length));
    }

    private void SortByDiagnal(List<Cell> tempGrid)
    {
        tempGrid.Sort((x, y) => (x.transform.position.x + x.transform.position.y).CompareTo(y.transform.position.x + y.transform.position.y));
    }

    void CollapseCell(Cell cell)
    {
        // Check if the cell is null or already collapsed
        if (cell == null)
        {
            Debug.LogError("CollapseCell: Cell is null.");
            return;
        }

        if (cell.isCollapsed)
        {
            // Debug.LogError($"CollapseCell: Cell {cell.name} is already collapsed.");
            StartCellSelection();
            return;
        }


        // Get a tile based on weight and set it as the only option
        Tile selectedTile = SelectTileByWeight(cell);

        // Use Fallback if all options have weight of 0
        if (selectedTile == null)
        {
            // Debug.LogError($"CollapseCell: Cell {cell.name} has no tile options.");
            currentProblematicCell = cell;
            BacktrackWithStack();
            backtrackCount = 0;
            StartCellSelection();
            return;
        }

        //Record options before apply

        // Get the available options for the cell and store them in the state
        List<Tile> availableOptions = new List<Tile>(cell.tileOptions);

        // Create a new state and push it onto the stack
        CellState cellState = new CellState(cell, availableOptions);
        if (backtrackStack == null)
        {
            backtrackStack = new Stack<CellState>();
        }
        backtrackStack.Push(cellState);


        ApplyTileChoice(cell, selectedTile);
        cellState.SelectedTile = selectedTile;


        cell.isCollapsed = true;
        // Add the cell to the history list
        currentNode.ChosenOption = selectedTile;

        collapsedCount++;


        // Instantiate the selected tile visually
        InstantiateTile(selectedTile, cell);

        // Continue effect propagation
        UpdateGeneration();
    }

    void StartBackTrack()
    {

    }

    bool IsGridFullyCollapsed()
    {
        foreach (Cell cell in gridComponents)
        {
            if (!cell.isCollapsed)
            {
                return false;
            }
        }

        return true;
    }

    void LogGenerationTime()
    {
        if (gridEndTime == -1f) { gridEndTime = Time.realtimeSinceStartup; }
        // Debug.Log(gridEndTime);
        gridGenMillisecs = (gridEndTime - gridStartTime) * 1000;
        // Debug.Log($"{this.name} Logging time {gridGenMillisecs} = {gridEndTime} - {gridStartTime}");
        theWorld.LogGridCompletionTime(gridGenMillisecs, backtrackCoroutineCalled);
    }

    public float GetGenTime() { return gridGenMillisecs; }
    public int GetUseBacktrack() { return backtrackCoroutineCalled; }

    void ApplyTileChoice(Cell cell, Tile tile)
    {
        cell.selectedTile = tile;
    }

    //These methods chooses the cell which has the least number of possible tiles left,
    //Reducing the chance of making a premature or incorrect collapse
    Cell ChooseCellWithLeastEntropy(List<Cell> cells)
    {
        return cells.OrderBy(c => c.tileOptions.Length).FirstOrDefault();
    }
    Cell ChooseRandomWithLeastEntropy(List<Cell> cells)
    {
        List<Cell> leastEntropyCells = cells.Where(c => c.tileOptions.Length == cells.Min(x => x.tileOptions.Length)).ToList();
        int randomIndex = rng.Next(0, leastEntropyCells.Count);
        return leastEntropyCells[randomIndex];
    }

    Tile SelectTileRandomly(Cell cell)
    {
        int randomTileIndex = rng.Next(0, cell.tileOptions.Length);
        return cell.tileOptions[randomTileIndex];
    }
    Tile SelectTileByWeight(Cell cell)
    {
        if (cell == null || cell.tileOptions == null || cell.tileOptions.Length == 0)
        {
            // Debug.LogError(cell.name + " Cell or tileOptions is null or empty.");
            return null;
        }

        // Debug.Log($"SelectTileByWeight: Cell {cell.name} has {cell.tileOptions.Length} tile options.");
        // Log each tile and its weight
        // foreach (Tile tile in cell.tileOptions)
        // {
        //     Debug.Log($"Tile: {tile.name}, Weight: {tile.weight}");
        // }


        List<Tile> weightedOptions = ListTileByWeight(cell.tileOptions);

        if (weightedOptions == null || weightedOptions.Count == 0)
        {
            // Debug.LogError($"No weighted options available for Cell {cell.name}.");
            return null;
        }
        // Log how many weighted options were created
        // Debug.Log($"SelectTileByWeight: {weightedOptions.Count} weighted options found.");

        // Select a random tile from the weighted list
        int randomTileIndex = rng.Next(0, weightedOptions.Count);
        // Debug.Log($"SelectTileByWeight: Randomly selected tile at index {randomTileIndex}");

        return weightedOptions[randomTileIndex];
    }

    List<Tile> ListTileByWeight(Tile[] tileOptions)
    {
        List<Tile> returnList = new List<Tile>();

        if (tileOptions == null || tileOptions.Length == 0)
        {
            Debug.LogError("ListTileByWeight: Tile options array is null or empty.");
            return returnList;
        }

        foreach (Tile tile in tileOptions)
        {
            if (tile != null)
            {
                if (tile.weight <= 0)
                {
                    // Debug.LogWarning($"Tile {tile.name} has invalid weight ({tile.weight}). Skipping.");
                    continue;
                }
                for (int i = 0; i < tile.weight; i++)
                {
                    returnList.Add(tile);
                }
                // Debug.Log($"Added tile {tile.name} to the return list {tile.weight} times.");
            }
            else
            {
                // Debug.LogError("ListTileByWeight: Found a null tile in tile options.");
            }
        }
        // Final check if returnList is populated
        if (returnList.Count == 0)
        {
            // Debug.LogError("ListTileByWeight: No tiles added to return list.");
        }
        else
        {
            // Debug.Log($"ListTileByWeight: {returnList.Count} tiles added to the return list.");
        }

        return returnList;
    }

    void InstantiateTile(Tile tile, Cell cell) { cell.CreateTile(tile); }
    void DestroyLastInstantiatedTile(Cell cell) { cell.DestroyCellChildren(); }

    void UseFallBackTile(Cell cell)
    {
        Tile fallbackTile = theWorld.GetFallBackFromWorld();
        if (fallbackTile == null)
        {
            Debug.LogError("Fallback tile is not set in theWorld.");
            return;
        }

        cell.SetFallBackTile(fallbackTile);

        collapsedCount++;

        InstantiateTile(fallbackTile, cell);

        // Debug.Log("Fallback tile applied to " + cell.name);
        cell.isCollapsed = true;

        // Check if all cells have collapsed
        if (IsGridFullyCollapsed())
        {
            gridComplete = true;
            CheckNeighborCompleteness();
        }
    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOptions)
    {
        if (validOptions.Count <= 1) { return; }

        //remove all tiles that are not in the validOptions list
        optionList.RemoveAll(x => !validOptions.Contains(x));
    }

    void ValidateAllUnCollapsedCells()
    {
        foreach (Cell cell in gridComponents)
        {
            if (!cell.isCollapsed)
            {
                List<Tile> options = new List<Tile>(tileOptionsAll);

                Vector3 pos = _grid.WorldToLocal(cell.transform.position);
                // Debug.Log($"Validating {cell.name} at position: " + pos + " in " + this.name);
                // Check neighbors and refine options
                ValidateOptionsBasedOnNeighbors((int)pos.x + posOffset, (int)pos.y + posOffset, cell, options);

            }
        }
    }

    void UpdateGeneration()
    {
        ValidateAllUnCollapsedCells();

        // Check if the grid is fully collapsed after the update
        if (IsGridFullyCollapsed())
        {
            PostCompleteProcess();
        }
        else if (!gridComplete)
        {
            if (fallBackIterations > totalCellCount + backtrackLimit)
            {
                if (fallBackIterations % 5 == 0)  // Log only every 5 iterations
                {
#if UNITY_EDITOR
                    // Debug.LogWarning("Iterations exceeded without full collapse on " + this.name
                    // + " Iteration " + fallBackIterations + " CollapseCount " + collapsedCount + " TotalCellCount " + totalCellCount);
#endif
                }
                ProblematicReset();
                StartCellSelection(); // Ensure collapse process restarts
            }
            fallBackIterations++;
            StartCellSelection();
        }
    }

    private void ProblematicReset()
    {

        if (currentProblematicCell != null)
        {
            UseFallBackTile(currentProblematicCell);
            StartCellSelection(); // Ensure collapse process restarts
        }
        else { Debug.LogError("Problematic cell is null on " + this.name); }


    }

    private int AddProblemCellToDictionary()
    {
        if (problematicCells == null)
        {
            problematicCells = new Dictionary<Cell, int>();
        }
        if (currentProblematicCell == null)
        {
            Debug.LogError("Problematic cell is null.");
            return 0;
        }
        int toRT;
        // get the int from problemCell in problematicCells
        // if it is not in the hashset, add it
        // if it is in the hashset, increment the count
        if (problematicCells.ContainsKey(currentProblematicCell))
        {
            toRT = problematicCells.GetValueOrDefault(currentProblematicCell) + 1;
            problematicCells[currentProblematicCell] = toRT;
        }
        else
        {
            problematicCells.Add(currentProblematicCell, 1);
            toRT = 1;
        }
        return toRT;
    }


    void PostCompleteProcess()
    {
        gridComplete = true;
        gridEndTime = Time.realtimeSinceStartup;
        // Debug.Log("Ended");
        CheckNeighborCompleteness();

        // Clean up the stack
        if (backtrackStack != null)
        {
            backtrackStack.Clear();
        }
        // Clean up the tree
        if (decisionTreeRoot != null)
        {
            decisionTreeRoot = null;
        }

        LogGenerationTime();

    }

    private void RestartGeneration()
    {
        // Debug.Log("Restarting generation on " + this.name);
        // Clear the grid and start over
        ClearGridList();
        InsertCurrentGridToWFCList();
        ResetAllCellOptions();
        StartCellSelection();
    }

    private void ClearGridList()
    {
        if (gridComponents != null)
        {
            foreach (Cell cell in gridComponents)
            {
                cell.DestroyCellChildren();
            }
        }
        gridComponents.Clear();
    }

    void ResetCellOption(Cell cell)
    {
        cell.ReFillCell(tileOptionsAll);
        cell.DestroyCellChildren();
    }

    void ResetAllCellOptions()
    {
        foreach (Cell cell in gridComponents)
        {
            ResetCellOption(cell);
        }
    }


    public Cell GetMyCellAt(int x, int y)
    {
        return theGrid[x, y];
    }

    void ValidateOptionsBasedOnNeighbors(int x, int y, Cell cell, List<Tile> options)
    {
        // Implement the logic to remove invalid options based on the cell's neighbors.
        // This could involve checking the options against the up, right, down, and left neighbors.
        if (cell.isBeingValidated)
        {
            return;  // Prevent stack overflow due to cyclic validation.
        }
        cell.isBeingValidated = true;
        // Check the cell's neighbors and refine the options list
        try
        {
            // Create a list to store valid options based on neighbors
            List<Tile> validOptions = new List<Tile>(options);
            Cell cellAbove = null, cellToTheRight = null, cellBelow = null, cellToTheLeft = null;
            int xInGrid = x % dimension;
            int yInGrid = y % dimension;

            if (yInGrid < dimension - 1)
            {
                cellAbove = theGrid[xInGrid, yInGrid + 1];
            }
            else if (yInGrid == dimension - 1 && neighborsWFCTopCW != null && neighborsWFCTopCW[0] != null)
            {
                cellAbove = neighborsWFCTopCW[0].GetMyCellAt(xInGrid, 1);
            }

            if (cellAbove != null && cellAbove.isCollapsed)
            {
                // Get the valid tiles based on the neighbor above
                Tile neighborTile = cellAbove.selectedTile;
                List<Tile> upValidTiles = neighborTile.downNeighbours.ToList();
                validOptions = validOptions.Intersect(upValidTiles).ToList();

            }

            // Check the neighbor to the right
            if (xInGrid < dimension - 1)
            {
                cellToTheRight = theGrid[xInGrid + 1, yInGrid];
            }
            else if (xInGrid == dimension - 1 && neighborsWFCTopCW != null && neighborsWFCTopCW[2] != null)
            {
                cellToTheRight = neighborsWFCTopCW[2].GetMyCellAt(1, yInGrid);
            }

            if (cellToTheRight != null && cellToTheRight.isCollapsed)
            {
                Tile neighborTile = cellToTheRight.selectedTile;
                List<Tile> rightValidTiles = neighborTile.leftNeighbours.ToList();
                validOptions = validOptions.Intersect(rightValidTiles).ToList();

            }

            // Check the neighbor below (down)
            if (yInGrid > 0)
            {
                cellBelow = theGrid[xInGrid, yInGrid - 1];
            }
            else if (yInGrid == 0 && neighborsWFCTopCW != null && neighborsWFCTopCW[4] != null)
            {
                cellBelow = neighborsWFCTopCW[4].GetMyCellAt(xInGrid, dimension - 2);
            }

            if (cellBelow != null && cellBelow.isCollapsed)
            {
                Tile neighborTile = cellBelow.selectedTile;
                List<Tile> downValidTiles = neighborTile.upNeighbours.ToList();
                validOptions = validOptions.Intersect(downValidTiles).ToList();
            }

            // Check the neighbor to the left
            if (xInGrid > 0)
            {
                cellToTheLeft = theGrid[xInGrid - 1, yInGrid];
            }
            else if (xInGrid == 0 && neighborsWFCTopCW != null && neighborsWFCTopCW[6] != null)
            {
                cellToTheLeft = neighborsWFCTopCW[6].GetMyCellAt(dimension - 2, yInGrid);
            }

            if (cellToTheLeft != null && cellToTheLeft.isCollapsed)
            {
                Tile neighborTile = cellToTheLeft.selectedTile;
                List<Tile> leftValidTiles = neighborTile.rightNeighbours.ToList();
                validOptions = validOptions.Intersect(leftValidTiles).ToList();
            }


            // Debug.Log("Validating  " + cell.name + " with " + cellAbove);
            // Debug.Log($"Validating  {cell.name} at {xInGrid},{yInGrid} in {gameObject.name}");

            // Update the options for the cell based on valid options determined by neighbors
            options.Clear();
            options.AddRange(validOptions);

            // Update the cell's possible options
            cell.ReFillCell(options.ToArray());

        }
        finally
        {
            // Ensure the flag is always reset after validation
            cell.isBeingValidated = false;
        }

    }


    public void ResetAll()
    {
        RemoveAllChildren();
        fallBackIterations = 0;
        InitializeNeighborEdgesNulls();
        InitializeGrid();
    }

    private void ReFillCells()
    {
        foreach (Cell c in gridComponents)
        {
            c.ReFillCell(tileOptionsAll);
        }
    }

    public void RemoveAllChildren()
    {
        LogGenerationTime();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void RemoveAllTileObj()
    {
        //Destory child of cell in CellCollect
        foreach (Cell c in gridComponents)
        {
            foreach (Transform child in c.transform)
            {
                Destroy(child.gameObject);
            }
        }
        ResetCellOptions();

    }

    private void ResetCellOptions()
    {
        foreach (Cell c in gridComponents)
        {
            c.tileOptions = tileOptionsAll;
            c.isCollapsed = false;
        }
    }



    private void AcceptNewRow(Cell[] newRow, int direction)
    {
        // Ensure the incoming row is valid
        if (newRow == null || newRow.Length == 0)
        {
            Debug.LogError("newRow is null or empty for direction: " + direction);
            return;
        }

        switch (direction)
        {
            case 0:
                this.NBTop = newRow;
                break;
            case 2:
                this.NBRight = newRow;
                break;
            case 4:
                this.NBBottom = newRow;
                break;
            case 6:
                this.NBLeft = newRow;
                break;
            default:
                Debug.LogError("Unknown direction: " + direction);
                break;
        }
    }

    private void AcceptNewCorner(Cell neighborCornerCell, int direction)
    {
        if (neighborCornerCell == null)
        {
            Debug.LogError("cornerCell is null for direction: " + direction);
            return;
        }

        switch (direction)
        {
            case 1:
                this.Nbor_CornerCells[0] = neighborCornerCell;
                break;
            case 3:
                this.Nbor_CornerCells[1] = neighborCornerCell;
                break;
            case 5:
                this.Nbor_CornerCells[2] = neighborCornerCell;
                break;
            case 7:
                this.Nbor_CornerCells[3] = neighborCornerCell;
                break;
            default:
                Debug.LogError("Unknown direction: " + direction);
                break;
        }
    }



    // ----------------- Utility Functions -----------------

    private float GetDistance(Vector3 playerPos, Vector3 gridPos)
    {
        playerPos.z = 0;
        gridPos.z = 0;
        return Vector3.Distance(playerPos, gridPos);
    }



    // To remove system burden
    private void RemoveAllChildren(Transform parent)
    {
        // Remove collider
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }



    // ----------------- Extension Functions -----------------

    // up, up-right, right, down-right, down, down-left, left, up-left
    private Vector3 GetDirectionOffset(int direction)
    {
        switch (direction)
        {
            case 0: return new Vector3(0, dimension - 1, 0); // Up
            case 1: return new Vector3(dimension - 1, dimension - 1, 0); // Up-Right
            case 2: return new Vector3(dimension - 1, 0, 0); // Right
            case 3: return new Vector3(dimension - 1, -(dimension - 1), 0); // Down-Right
            case 4: return new Vector3(0, -(dimension - 1), 0); // Down
            case 5: return new Vector3(-(dimension - 1), -(dimension - 1), 0); // Down-Left
            case 6: return new Vector3(-(dimension - 1), 0, 0); // Left
            case 7: return new Vector3(-(dimension - 1), dimension - 1, 0); // Up-Left
            default: return Vector3.zero;
        }
    }



    // ----------------- Neighbor Functions -----------------

    // up, up-right, right, down-right, down, down-left, left, up-left
    public Cell[] GetMyEdge(int direction)
    {
        switch (direction)
        {
            case 0: return myTopRow;
            case 2: return myRightColumn;
            case 4: return myBottomRow;
            case 6: return myLeftColumn;
            default: return null;
        }
    }

    public Cell GetMyCorner(int direction)
    {
        switch (direction)
        {
            case 1: return myCornerCells[0]; // Top-right
            case 3: return myCornerCells[1]; // Bottom-right
            case 5: return myCornerCells[2]; // Bottom-left
            case 7: return myCornerCells[3]; // Top-left
            default: return null;
        }
    }

    public bool CheckNeighborCompleteness()
    {
        for (int i = 0; i < 8; i++)
        {
            if (neighborsWFCTopCW[i] == null)
            {
                return false;
            }
        }
        neighborsComplete = true;
        return true;
    }


    // -----Destrction Functions-----
    private void LightWeight()
    {
        // theWorld.UnboundWFCs.Remove(this);
        RemoveDumpCollect();
    }

    void RemoveDumpCollect()
    {
        foreach (Transform child in DumpCollect.transform)
        {
            Destroy(child.gameObject);
            theWorld.cellCount--;
        }
    }

    // on destroy =================
    private void OnDestroy()
    {
        // Debug.Log(gridEndTime);
        // LogGenerationTime();
        StopCoroutine(CheckEntropy());
        DestroyAllChildren();
    }

    void DestroyAllChildren()
    {
        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
    }



    // ================= Backtrack w/ tree =================


    public class DecisionTreeNode
    {
        public Cell Cell { get; private set; }
        public Tile[] AvailableOptions { get; private set; }
        public Tile ChosenOption { get; set; }
        public DecisionTreeNode Parent { get; private set; }
        public List<DecisionTreeNode> Children { get; private set; }

        public DecisionTreeNode(Cell cell, Tile[] availableOptions, DecisionTreeNode parent = null)
        {
            Cell = cell;
            AvailableOptions = (Tile[])availableOptions.Clone(); // Clone the options
            ChosenOption = null;
            Parent = parent;
            Children = new List<DecisionTreeNode>();
        }

        // Remove the chosen tile from available options to prevent selecting it again
        public bool RemoveChosenOption()
        {
            List<Tile> optionList = new List<Tile>(AvailableOptions);

            optionList.Remove(ChosenOption);
            AvailableOptions = optionList.ToArray();


            Cell.ReFillCell(AvailableOptions);
            return AvailableOptions.Length > 0;
        }


    }

    /*
    Current Node - The node that is currently being processed
    initial backtracking node is the leaf node
    Backtrack is called only when child node or grandchild node has no options left

    choices
    0- i have no choice, need parent
    1- i have one choice, need parent
    2+ i have multiple choices, need to remove old choice and try new one

    parent
    0- when i have no choice and no parent, need fallback
    0- when i have no choice, need parent
    1- when i have one choice, my child has problem, need my parent
    2- when i have multiple choices, my child has problem, I can try to solve it

    
    1. If the current node has no more than one options
        Remove my current option and refill my child options
        Move up to the parent and backtrack further
    2. If the current node has more than one option
        Revert current node tile and options
        Refill child options based on the newly selected tile for the current node
        Collapse the cell again
    3. If there's no parent, backtracking has failed
        Use fallback tile for the current cell
    */

    private int backtrackCount = 0;
    private const int maxBacktrackDepth = 50; // Or any reasonable number
    void BacktrackWithStack()
    {
        backtrackCount++;
        // collapsedCount++;
        // backtrackCoroutineCalled++;
        if (backtrackCount > maxBacktrackDepth)
        {
            Debug.LogWarning("Max backtrack depth reached. Applying fallback.");
            UseFallBackTile(currentProblematicCell);
            return;
        }
        if (backtrackStack == null || backtrackStack.Count == 0)
        {
            // Debug.LogError("Backtracking failed. No previous state to revert to.");
            UseFallBackTile(currentProblematicCell); // Use fallback if there's no state to revert

            StartCellSelection();
            return;
        }

        // Pop the last state from the stack
        CellState lastState = backtrackStack.Pop();
        Cell cell = lastState.Cell;
        // Debug.Log("Backtracking... on " + cell.name);
        // Remove the selected tile from available options
        lastState.RemoveSelectedTile();

        // for (int i = 0; i < lastState.AvailableOptions.Count; i++)
        // {
        //     Debug.Log("Option " + i + ": " + lastState.AvailableOptions[i].name);
        // }

        if (lastState.HasChoices())
        {

            // If there are remaining options, try collapsing the cell again
            lastState.Cell.DestroyCellChildren(); // Destroy the last instantiated tile
            lastState.Cell.ReFillCell(lastState.AvailableOptions.ToArray()); // Refill the cell with the available options
            CollapseCell(cell);
        }
        else
        {
            // If no options are left, backtrack further
            cell.DestroyCellChildren();
            // If no options are left, backtrack further, but only if we haven't reached a limit
            if (backtrackStack.Count == 0)
            {
                Debug.LogWarning("Backtracking exhausted all options. Applying fallback.");
                UseFallBackTile(cell);
                return;
            }
            // Continue backtracking
            BacktrackWithStack();
        }
    }

    // void BacktrackWithTree()
    // {
    //     if (currentNode == null)
    //     {
    //         Debug.LogError("Backtracking failed. No parent node.");
    //         UseFallBackTile(currentProblematicCell);
    //         return;
    //     }

    //     if (currentNode.AvailableOptions.Length <= 1)
    //     {
    //         // Remove the chosen option from the parent node
    //         currentNode.RemoveChosenOption();
    //         currentNode.Cell.DestroyCellChildren();
    //         // Move up to the parent node
    //         currentNode = currentNode.Parent;
    //         Backtrack();
    //     }
    //     else
    //     {
    //         // Revert the current node's tile and options
    //         currentNode.Cell.DestroyCellChildren();
    //         currentNode.Cell.ReFillCell(currentNode.AvailableOptions);

    //         // Refill child options based on the newly selected tile for the current node
    //         RefillChildOptions(currentNode);

    //         // Collapse the cell again
    //         CollapseCell(currentNode.Cell);
    //     }

    // }

    // void RefillChildOptions(DecisionTreeNode parentNode)
    // {
    //     foreach (DecisionTreeNode childNode in parentNode.Children)
    //     {
    //         Cell childCell = childNode.Cell;
    //         Tile parentTile = parentNode.ChosenOption;

    //         if (parentTile != null)
    //         {
    //             List<Tile> validOptions = new List<Tile>(childCell.tileOptions);

    //             // Update valid options based on the parent tile (constraints depending on neighbors)
    //             // Assume we are propagating neighbor constraints (e.g., matching edges/tiles)
    //             if (parentTile.upNeighbours != null && childCell.transform.localPosition.y < parentNode.Cell.transform.localPosition.y)
    //             {
    //                 validOptions = validOptions.Intersect(parentTile.upNeighbours).ToList();
    //             }
    //             if (parentTile.rightNeighbours != null && childCell.transform.localPosition.x > parentNode.Cell.transform.localPosition.x)
    //             {
    //                 validOptions = validOptions.Intersect(parentTile.rightNeighbours).ToList();
    //             }
    //             if (parentTile.downNeighbours != null && childCell.transform.localPosition.y > parentNode.Cell.transform.localPosition.y)
    //             {
    //                 validOptions = validOptions.Intersect(parentTile.downNeighbours).ToList();
    //             }
    //             if (parentTile.leftNeighbours != null && childCell.transform.localPosition.x < parentNode.Cell.transform.localPosition.x)
    //             {
    //                 validOptions = validOptions.Intersect(parentTile.leftNeighbours).ToList();
    //             }

    //             // Refill the cell with new valid options
    //             childCell.ReFillCell(validOptions.ToArray());
    //         }

    //         // Recursively refill child options further down the tree
    //         RefillChildOptions(childNode);
    //     }
    // }


    // ----- backtracking w/ stack -----
    // Stack to keep track of cell states during the collapse process
    private Stack<CellState> backtrackStack = new Stack<CellState>();

    public class CellState
    {
        public Cell Cell { get; private set; }
        public List<Tile> AvailableOptions { get; private set; }
        public Tile SelectedTile { get; set; }

        public CellState(Cell cell, List<Tile> availableOptions)
        {
            Cell = cell;
            AvailableOptions = availableOptions;
            SelectedTile = null;
        }

        // Remove the chosen tile from available options
        public void RemoveSelectedTile()
        {
            if (SelectedTile != null)
            {
                AvailableOptions.Remove(SelectedTile);
                // Debug.Log("Removed selected tile: " + SelectedTile.name);
            }
        }

        // Check if there are remaining options for this cell
        public bool HasChoices()
        {
            return AvailableOptions.Count > 1;
        }
    }




    // ----------------- End of WaveFunction.cs -----------------
}
