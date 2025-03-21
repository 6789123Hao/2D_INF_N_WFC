using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // Required for calculating average time


/// <summary>
/// Manages the procedural world generation, tile interactions, and rule application.
/// Partial class separated for readability and organization in TheWorld_Interactions.cs
/// </summary>
public partial class TheWorld : MonoBehaviour
{

    // parameters
    [Header("Parameters")]
    public int dimensions;
    public float secPerGen = 0.005f, spawnDistance = 0;
    public int tileSetdefault = 0, autoDirection = 0, cellCount = 0, gridCount = 0, backtrackLimit = 100;


    [Header("Options")]
    public bool useSeed = false; // Toggle to use seeding or not
    public TMP_InputField SeedInputField, dimensionField; // Input field to set seed value
    public int seed = 0; // Default seed value
    public bool checkPerUpdate = true, usingBacktrack = true, isCheckingDistance = true, isStepByStep = false;

    // Tile options and weights
    public Tile[] CurrentTiles;
    [Header("Tile Set Options")]
    [SerializeField] private List<TileSetOptions> tileSets;
    private TileSetOptions currentTileSet;
    public Tile fallBackTile;
    public List<TileData> globalAffectedTiles = new List<TileData>(); // Dynamically managed list


    [Header("Hooks")]
    [SerializeField] private Grid _worldGrid;
    public Toggle StepByStepToggle, checkDistanceToggle;


    [Header("Prefabs")]
    public WaveFunction waveFunctionPrefab;
    public Cell cellPrefab;
    Vector2Int[] offsets2D = {
            Vector2Int.up,                        // 0: Up
            Vector2Int.up + Vector2Int.right,      // 1: Up-Right
            Vector2Int.right,                     // 2: Right
            Vector2Int.down + Vector2Int.right,    // 3: Down-Right
            Vector2Int.down,                      // 4: Down
            Vector2Int.down + Vector2Int.left,     // 5: Down-Left
            Vector2Int.left,                      // 6: Left
            Vector2Int.up + Vector2Int.left        // 7: Up-Left
        };

    // runtime variables -- Initialized them in Awake to Avoid Inspector NullRefError.
    // [Header("Runtime Variables")]
    private Dictionary<Vector2Int, WaveFunction> waveFunctionMap = new Dictionary<Vector2Int, WaveFunction>();
    private List<float> gridCompletionTimesInMilliSec;
    private List<int> backedSteps;



    [Header("Zone Effect Type")]

    public TMP_Dropdown ruleDropdown;
    public EffectRule currentRule;



    [Header("Dropdowns")]
    public TMP_Dropdown tileDropdown;
    public TMP_Dropdown directionDropdown;


    private void Awake()
    {
        gridCompletionTimesInMilliSec = new List<float>();
        backedSteps = new List<int>();
    }

    void Start()
    {
        CheckAsserts();
        UpdateTileDropdownOptions();
        AddListeners();
        SetTileSet(tileSetdefault);
        tileDropdown.value = tileSetdefault;
        InitializeRuleDropdown();
        DropStartingGrids();
        UpdateUI();
    }

    void DropStartingGrids()
    {
        // If speed test is enabled, start the test
        if (runSpeedTest)
        {
            Initialize();
            CreateFirstWFC();
            StartCoroutine(BulkSpeedRun());
        }
        else
        {
            // get value L of max of cameraW or cameraV
            int L = Mathf.CeilToInt(Mathf.Max(cameraW, cameraV));
            int amount = CalculateGridLengthLfromK(L, dimensions);
            player.InitializePlayer();
            InputReceived();
            CreateLxL(amount);
            // Move the player to the center of the grid
            // Move the camera to the player
            // cameraController.InitializeCamera();
            cameraController.TeleportCamera(player.transform.position);

        }
    }

    private void CheckAsserts()
    {
        Debug.Assert(cellPrefab != null, "Cell prefab not set");
        Debug.Assert(waveFunctionPrefab != null, "Wave Function prefab not set");
        Debug.Assert(dimensions > 0, "Dimensions not set");
        Debug.Assert(secPerGen > 0, "Seconds per step not set");
        Debug.Assert(WFCsText != null, "WFCs text not set");
        Debug.Assert(UnboundText != null, "Unbound text not set");
        Debug.Assert(CellText != null, "Cell text not set");
        Debug.Assert(CameraVText != null, "V text not set");
        Debug.Assert(CameraWText != null, "V text not set");
        Debug.Assert(player != null, "Player not set");
        Debug.Assert(cameraController != null, "Camera Controller not set");
        Debug.Assert(canvas != null, "Canvas not set");
    }

    private void AddListeners()
    {
        // Add listeners to the dropdowns
        if (tileDropdown != null)
        {
            tileDropdown.onValueChanged.AddListener(OnTileDropdownValueChanged);
        }
        if (directionDropdown != null)
        {
            directionDropdown.onValueChanged.AddListener(OnDirectionDropdownValueChanged);
        }
        // Initialize the input fields with default values
        if (TimeDelayInputField != null)
            TimeDelayInputField.text = timeDelayPerStep.ToString();

        if (StepsInputField != null)
            StepsInputField.text = numberOfSteps.ToString();

        // add listeners to update the values dynamically
        if (TimeDelayInputField != null)
            TimeDelayInputField.onValueChanged.AddListener(OnTimeDelayInputChanged);

        if (StepsInputField != null)
            StepsInputField.onValueChanged.AddListener(OnStepsInputChanged);

        if (SeedInputField != null)
        {
            SeedInputField.onValueChanged.AddListener(OnSeedInputChanged);
        }
        if (dimensionField != null)
        {
            dimensionField.onValueChanged.AddListener(OnDimensionInputChanged);
        }

        if (BulkSpeedRunTimesInputField != null)
        {
            BulkSpeedRunTimesInputField.onValueChanged.AddListener(OnBulkSpeedRunTimesInputChanged);
        }

        // Add listener for speed test toggle
        if (speedTestToggle != null)
        {
            speedTestToggle.onValueChanged.AddListener(OnSpeedTestToggleChanged);
        }

        if (StepByStepToggle != null)
        {
            isStepByStep = StepByStepToggle.isOn;
            StepByStepToggle.onValueChanged.AddListener(OnStepByStepToggleChanged);
        }



        if (checkDistanceToggle != null)
        {
            isCheckingDistance = checkDistanceToggle.isOn;
            checkDistanceToggle.onValueChanged.AddListener(OnCheckDistanceToggleChanged);
        }
    }

    // check distance periodically
    void Update()
    {
        if (checkPerUpdate)
        {
            UpdateUI();
        }
    }

    void ApplyCurrentRule(Vector3 playerPosition)
    {
        if (currentTileSet != null)
        {
            currentTileSet.ApplyCurrentRule(playerPosition);
        }
    }
    private void UpdateTileDropdownOptions()
    {
        // Ensure tileDropdown and tileSets are not null
        if (tileDropdown == null || tileSets == null)
        {
            Debug.LogError("TileDropdown or TileSets list is not set.");
            return;
        }

        // Clear existing options in the dropdown
        tileDropdown.ClearOptions();

        // Create a new list of option strings based on tileSets names or asset names
        List<string> tileSetNames = tileSets.Select(tileSet => tileSet.name).ToList();

        // Add the new options to the dropdown
        tileDropdown.AddOptions(tileSetNames);
    }

    private void InitializeRuleDropdown()
    {
        ruleDropdown.ClearOptions();
        var options = currentTileSet.effectRules.Select(r => r.name).ToList();

        // Add a default "None" option if there are no rules
        if (options.Count == 0)
        {
            options.Add("None");
        }
        ruleDropdown.AddOptions(options);

        ruleDropdown.onValueChanged.AddListener(index =>
        {
            if (index < currentTileSet.effectRules.Count)
            {
                // Update both currentRule and the tile set's currentEffectRule
                currentRule = currentTileSet.effectRules[index];
                currentTileSet.currentEffectRule = currentRule;
            }
            else
            {
                Debug.LogWarning("Invalid effect rule index selected.");
            }
        });
    }

    private void OnRuleDropdownValueChanged(int value)
    {
        SelectRule(value);
    }


    // Method to set and initialize a tile set
    public void SetTileSet(int setIndex)
    {
        if (setIndex >= 0 && setIndex < tileSets.Count)
        {
            currentTileSet = tileSets[setIndex];
            fallBackTile = currentTileSet.GetFallBackTile() != null ?
                            currentTileSet.GetFallBackTile() : fallBackTile;
            // Set CurrentTiles directly from the currentTileSet
            CurrentTiles = currentTileSet.tiles.Select(tileData => tileData.tilePrefab).ToArray();
            // Call SetTileOptions to complete the setup
            SetTileOptions();

        }
        else
        {
            Debug.LogError("Invalid tile set index.");
        }
        Debug.Assert(CurrentTiles.Length > 0, "Tiles options not set");
    }

    public Tile GetFallBackFromWorld()
    {
        Tile toRT = currentTileSet.GetFallBackTile();
        return toRT == null ?
                fallBackTile : toRT;
    }

    /// <summary>
    /// Responds to movement input and generates new grids as needed.
    /// </summary>
    public void RespondToMovement(int fourWayDirection)
    {
        if (fourWayDirection < 0 || fourWayDirection > 3)
        {
            Debug.LogError("Invalid RespondToMovement direction");
            return;
        }
        /*
        for each direction:
        get the  world to cell grid Vector2Int from the corresponding boundary
        check if there is a wfc at that Grid Vector2Int, 
        if not, create one
        */
        Transform boundaryTransform = fourWayDirection switch
        {
            0 => UpBound.transform,
            1 => RightBound.transform,
            2 => DownBound.transform,
            3 => LeftBound.transform,
            _ => null
        };
        if (boundaryTransform == null)
        {
            Debug.LogError("Invalid movement direction.");
            return;
        }

        // Convert the boundary position to a grid position (Vector2Int)
        Vector2Int boundaryPosition = (Vector2Int)_worldGrid.WorldToCell(boundaryTransform.position);

        // Check if there's already a WFC at this boundary position
        WaveFunction wfc = GetWaveFunctionAt(boundaryPosition);

        // If no WFC exists, create one
        if (wfc == null)
        {
            wfc = CreateWFC2D(boundaryPosition);
            wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
            wfc.Initialize();
            wfc.RunWFC();

            // Add the new WFC to the relevant collections
            AddWFCToCollection2D(wfc, boundaryPosition);
        }

        CompleteWFCSideNeighbors(wfc, fourWayDirection);
    }


    // <summary>
    // Create a grid of size LxL
    // </summary>
    /*with Given wfc, create wfc on its coverDistance as if it's at the center of the row
    the length of the coverDistance is calculated from W when direction equal 0 or 2
    the length of the coverDistance is calculated from V when direction equal 1 or 3
    the amount of wfc should be placed on each side should be 
    (((W-dimensions)/2) / dimensions) + 1 
    or
    (((V-dimensions)/2) / dimensions) + 1 
    so that the wfcs covers the corners
    Make sure to check if there is wfc already existing, skip it
    */
    private void CompleteWFCSideNeighbors(WaveFunction originWfc, int fourWayDirection)
    {
        // Determine the axis to extend along based on the direction
        bool isHorizontal = (fourWayDirection == 0 || fourWayDirection == 2);

        // Calculate the total length to cover
        float totalLength = isHorizontal ? cameraW : cameraV;

        // Calculate the number of additional WFCs needed on each side
        int additionalWFCs = Mathf.CeilToInt(((totalLength - dimensions) / 2) / (dimensions - 1));

        // Get the original WFC's position
        Vector3 originPosition = originWfc.transform.position;

        // Instantiate WFCs on each side
        for (int i = -additionalWFCs; i <= additionalWFCs; i++)
        {
            if (i == 0) continue; // Skip the origin WFC

            // Calculate the new position
            Vector3 newPosition = originPosition + (isHorizontal ? Vector3.right : Vector3.up) * i * (dimensions - 1);
            Vector2Int newPos2D = (Vector2Int)_worldGrid.WorldToCell(newPosition);

            // Check if a WFC already exists at this position
            if (GetWaveFunctionAt(newPos2D) == null)
            {
                // Instantiate a new WFC
                WaveFunction newWfc = CreateWFC2D(newPos2D);
                newWfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);

                // Add the new WFC to the relevant collections
                AddWFCToCollection2D(newWfc, newPos2D);
                newWfc.Initialize();
                // newWfc.NeighborInteractions();
                newWfc.RunWFC();
                newWfc.ParentGrid = originWfc;

            }
        }
    }

    // <summary>
    // Reset the weights of all tiles to their default values
    // </summary>
    void TileWeightReset()
    {
        foreach (TileSetOptions option in tileSets)
        {
            option.ResetTileWeightsToDefault();
        }
    }

    // <summary>
    // Set the tile options based on the selected tile set
    // </summary>
    public void SetTileOptions()
    {
        // Reset weights to the original settings before applying new options
        TileWeightReset();

        if (CurrentTiles == null || CurrentTiles.Length == 0)
        {
            Debug.LogError("CurrentTiles is empty after setting tile set.");
            return;
        }

        // Link fallback tile to the new CurrentTiles set
        LinkFallbackTileConnections();

        // Validate and initialize the tiles
        if (CheckTileConnections())
        {
            DestroyAllWFCs(); // If tile connections are valid, reset the Wave Functions
        }
        else
        {
            Debug.LogWarning("Invalid tile connections in " + tileDropdown.value);
            DestroyAllWFCs();
        }
    }

    // <summary> 
    // add all tiles in current tile set to the surrounding tiles of the fallBackTile
    // </summary>
    // this is used to link the fallBackTile to all other tiles in the set
    void LinkFallbackTileConnections()
    {
        if (fallBackTile != null && currentTileSet != null)
        {
            fallBackTile.upNeighbours = currentTileSet.tiles.Select(td => td.tilePrefab).ToArray();
            fallBackTile.downNeighbours = currentTileSet.tiles.Select(td => td.tilePrefab).ToArray();
            fallBackTile.leftNeighbours = currentTileSet.tiles.Select(td => td.tilePrefab).ToArray();
            fallBackTile.rightNeighbours = currentTileSet.tiles.Select(td => td.tilePrefab).ToArray();
        }
    }


    // <summary>
    // Check if all tiles in the current tile set are connected to each other
    // </summary>
    private bool CheckTileConnections()
    {
        bool validTileConnections = true;
        foreach (Tile tile in CurrentTiles)
        {
            validTileConnections = validTileConnections && tile.AllNeighborsContainSelf();

            tile.CleanUpDuplicates();
        }
        return validTileConnections;
    }


    // <summary>
    // Trigger TileSet change based on the dropdown value change
    // </summary>
    private void OnTileDropdownValueChanged(int value)
    {
        SetTileSet(value); // Set the tile set based on the dropdown value
        LinkFallbackTileConnections();
        InitializeRuleDropdown();
        SelectRule(0); // Select the first rule by default
    }

    private void OnStepByStepToggleChanged(bool value) { isStepByStep = value; }

    private void OnCheckDistanceToggleChanged(bool value) { isCheckingDistance = value; }

    private void OnDirectionDropdownValueChanged(int value) { autoDirection = value; }

    // <summary>
    // Select a rule based on the index
    // </summary>
    private void SelectRule(int index)
    {
        if (currentTileSet != null && currentTileSet.effectRules != null && currentTileSet.effectRules.Count > 0)
        {
            if (index < currentTileSet.effectRules.Count)
            {
                currentRule = currentTileSet.effectRules[index];
                currentTileSet.currentEffectRule = currentRule;
            }
            else
            {
                Debug.LogWarning($"Index {index} is out of bounds for effect rules.");
            }
        }
        else
        {
            Debug.LogWarning("No effect rules available in the current tile set.");
        }
    }


    // <summary>
    // trigger RunWFC on all WaveFunctions
    // </summary>
    void WakeUpAllWFCs()
    {
        foreach (WaveFunction wfc in waveFunctionMap.Values)
        {
            wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
            wfc.Initialize();
            wfc.RunWFC();
        }
    }

    // Calculate the total grid length in x or y direction
    private int GetTotalGridLength(int gridSize, int gridCount)
    {
        // Formula: first grid size + (grid size - 1) * number of additional grids
        return gridSize + (gridSize - 1) * (gridCount - 1);
    }

    // <summary>
    // Calculate the number of grids needed to cover the total length L
    // </summary>
    private void Initialize()
    {
        firstTileReady = false;
        if (dimensions == default)
            spawnDistance = dimensions * 2;
        if (_worldGrid != null)
        {
            // change the grid size to match the dimensions
            _worldGrid.cellSize = new Vector3(dimensions, dimensions, 1);
        }
        gridCount = 0;
        cellCount = 0;
        gridCompletionTimesInMilliSec.Clear();
        backedSteps.Clear();
        player.InitializePlayer();
        // cameraController.InitializeCamera();
        TileWeightReset();
    }

    // <summary>
    // Create first WFC at the center of the grid
    // </summary>
    public WaveFunction CreateFirstWFC()
    {
        DestroyAllWFCs();
        WaveFunction wfc = GetWaveFunctionAt(player.transform.position);
        firstTileReady = wfc != null ? true : false;
        if (!firstTileReady)
        {
            wfc = CreateWFC2D(Vector2Int.zero);
            firstTileReady = true;
        }
        return wfc;
    }

    // <summary>
    // Create WFC grid at given position and optionally set a parent
    // </summary>
    public WaveFunction CreateWFC(Vector3 position, Transform parent = null)
    {
        WaveFunction wfc = Instantiate(waveFunctionPrefab, position, Quaternion.identity);
        wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
        wfc.transform.parent = parent != null ? parent : transform;
        return wfc;
    }

    // <summary>
    // Create WFC grid with 2D grid index position
    // </summary>
    public WaveFunction CreateWFC2D(Vector2Int gridPosition2D)
    {
        // Convert grid position to world position
        Vector3 worldPosition = _worldGrid.CellToWorld((Vector3Int)gridPosition2D);
        WaveFunction wfc = Instantiate(waveFunctionPrefab, worldPosition, Quaternion.identity);
        wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
        wfc.InitializeWithGridPosition(this, gridPosition2D); // Initialize WFC with grid position
        AddWFCToCollection2D(wfc, gridPosition2D);
        waveFunctionMap[gridPosition2D] = wfc;
        wfc.name = $"WFC_{gridPosition2D.x}_{gridPosition2D.y}";
        return wfc;
    }

    public void DropFirstWFC()
    {
        Initialize();
        CreateFirstWFC();
        WakeUpAllWFCs(); // Normal behavior
    }

    public void InputReceived()
    {
        playerPos = player.transform.position;
        ApplyCurrentRule(playerPos);

        UpdateUI();
    }

    // Called by UI button to change the dimension
    public void ChangeDimension(int difference)
    {
        dimensions += difference;
        if (dimensions < 1)
        {
            dimensions = 1;
        }
        Restart();
    }

    public void AddWFCToCollection2D(WaveFunction wfc, Vector2Int gridPosition)
    {
        waveFunctionMap[gridPosition] = wfc;
    }

    public void AddWFCToCollection(WaveFunction wfc, Vector3Int gridPosition)
    {
        AddWFCToCollection2D(wfc, (Vector2Int)gridPosition);
    }

    public Vector2Int GetWFCWorldPos2DInt(WaveFunction wfc)
    {
        return (Vector2Int)_worldGrid.WorldToCell(wfc.transform.position);
    }
    public void RemoveWFCFromCollection(Vector2Int gridPosition)
    {
        if (waveFunctionMap.ContainsKey(gridPosition))
        {
            waveFunctionMap.Remove(gridPosition);
        }
    }
    public WaveFunction GetWaveFunctionAt(Vector2Int gridPos2DInt)
    {
        return waveFunctionMap.TryGetValue(gridPos2DInt, out WaveFunction wfc) ? wfc : null;
    }

    public WaveFunction GetWaveFunctionAt(Vector3 worldPosition)
    {
        return GetWaveFunctionAt((Vector2Int)_worldGrid.WorldToCell(worldPosition));
    }

    // <summary>
    // Get neighboring WaveFunctions based on grid position
    // </summary>
    public WaveFunction[] GetNeighborWaveFunctions(Vector3 worldGridPosition)
    {
        Vector2Int gridPos2DKey = (Vector2Int)_worldGrid.WorldToCell(worldGridPosition);
        WaveFunction[] neighbors = new WaveFunction[8];

        Vector2Int neighborPos2DKey;
        for (int i = 0; i < offsets2D.Length; i++)
        {
            neighborPos2DKey = gridPos2DKey + offsets2D[i];
            neighbors[i] = GetWaveFunctionAt(neighborPos2DKey);
        }

        return neighbors;
    }

    public Cell GetCellPrefab()
    {
        return cellPrefab;
    }

    public void ReloadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void Restart()
    {
        StopAllCoroutines();
        StopAutoWalkProtocol();
        WriteLogToFile();
        DestroyAllWFCs();
        TileWeightReset();
        Initialize();
        DropStartingGrids();
        UpdateUI();
    }

    [ContextMenu("DestroyAllWFCs")]
    public void DestroyAllWFCs()
    {
        // Clear all WaveFunctions in waveFunctionMap
        foreach (var wfcEntry in waveFunctionMap)
        {
            if (wfcEntry.Value != null)
            {
                Destroy(wfcEntry.Value.gameObject); // Destroy the WaveFunction's GameObject
            }
        }
        waveFunctionMap.Clear();
    }

    public int GetDimension() { return dimensions; }
    public float GetExtendDistance() { return spawnDistance; }
    public Tile[] GetTileOptions() { return CurrentTiles; }
    public int GetBacktrackLimit() { return backtrackLimit; }
    public float GetSecPerStep() { return secPerGen; }
    public bool GetUseBacktrack() { return usingBacktrack; }
    public bool GetIsStepByStep() { return isStepByStep; }

    void OnDestroy()
    {
        WriteLogToFile();
        if (tileDropdown != null)
        {
            tileDropdown.onValueChanged.RemoveAllListeners();
        }
        if (directionDropdown != null)
        {
            directionDropdown.onValueChanged.RemoveAllListeners();
        }
        TileWeightReset();
    }


    // ================== Start of Zone Effect ==================

    private void AdjustTileWeightBasedOnDistance(float distance, int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= CurrentTiles.Length)
        {
            Debug.LogWarning("Invalid tile index");
            return;
        }

        // Define the max distance for the weight to reach the minimum (1) and the minimum distance for the weight to be at its max (20)
        float maxDistance = 50f; // Adjust based on your game world size
        float minDistance = 0f;  // The closest possible distance

        // Calculate the weight based on the inverse distance
        float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance)); // Normalized distance (0 = close, 1 = far)
        float weight = Mathf.Lerp(20f, 0f, normalizedDistance); // Interpolate from 20 (close) to 1 (far)

        // Apply the adjusted weight to the tile
        CurrentTiles[tileIndex].weight = (int)weight;
        Debug.Log("Tile " + CurrentTiles[tileIndex].name + " weight: " + weight);
    }


    // ================== Start of Seed ==================

    // Method to handle seed input change
    public void OnSeedInputChanged(string value)
    {
        if (int.TryParse(value, out int newSeed)) { seed = newSeed; }
        if (seed == 0) { useSeed = false; }
        else { useSeed = true; }
    }

    public void OnDimensionInputChanged(string value)
    {
        if (int.TryParse(value, out int newDimension))
        {
            dimensions = newDimension;
        }
        UpdateUI();
    }

    // Method to get the seed value
    public int GetSeed()
    {
        return seed;
    }

    // Method to check if seeding is enabled
    public bool IsSeedingEnabled()
    {
        return useSeed;
    }

    IEnumerator RestartCoroutine()
    {
        // Ensure all WFCs and world data are reset properly
        DestroyAllWFCs();
        TileWeightReset();
        RestartCoroutine_Walk();

        Initialize();  // Reinitialize the world
        yield return new WaitForEndOfFrame();  // Ensure the restart finishes before starting the next run
    }
}
