using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq; // Required for calculating average time
//using Math
using System;

public partial class TheWorld : MonoBehaviour
{

    // Conections
    [Header("Player and Interaction")]
    public ThePlayer player;
    private Vector3 playerPos;
    public CameraController cameraController;
    public TMP_InputField TimeDelayInputField;
    public TMP_InputField StepsInputField, BulkSpeedRunTimesInputField;
    [SerializeField] private Transform UpBound, RightBound, DownBound, LeftBound;


    // for the speed test
    [Header("Speed Test Settings")]
    public bool runSpeedTest = false; // Toggle to run the speed test
    public int numberOfGrids = 10;    // Number of grids to generate
    public int bulkSpeedRunTimes = 3; // Number of times to run the speed test
    public Toggle speedTestToggle;    // Toggle to enable/disable speed test

    // UI Elements
    public TextMeshProUGUI PosText, DimensionText, CameraVText, CameraWText;
    public GameObject finishedTextPrefab;  // Reference to the "Finished" text prefab
    public GameObject canvas;
    private float cameraV = 0f, cameraW = 0f;


    // Test-related fields
    // Default values for speed and steps
    [SerializeField] private float timeDelayPerStep = 0.1f;
    private int numberOfSteps = 10000;
    private bool isAutoMoveActive = false, firstTileReady = false;
    private Coroutine autoWalkCoroutine;
    private float playerStartTime, playerEndTime;
    private int gridsGenerated = 0;   // Counter for the grids generated
    private float speedTestStartTime, speedTestEndTime;



    public void UpdateUI()
    {
        PosText.text = $"Pos: {player.transform.position.x}, {player.transform.position.y}";
        DimensionText.text = $"Dimension {dimensions} X";
        CameraVText.text = $"Cam Height V = {cameraV}";
        CameraWText.text = $"Cam Width W = {cameraW}";
    }
    public void UIRest()
    {
        PosText.text = "Pos: 0, 0";
        CameraVText.text = "Cam Height V = ?";
        CameraVText.text = "Cam Width W = ?";
    }

    public void SetV(float newV) { cameraV = newV; }
    public void SetW(float newW) { cameraW = newW; }

    public void OnBulkSpeedRunTimesInputChanged(string value)
    {
        if (int.TryParse(value, out int newTimes))
        {
            numberOfGrids = newTimes;
        }
    }


    // ================== Start of AutoWalk ==================

    public void WalkOnce(float distance = 1)
    {
        player.WalkOnce(dimensions, autoDirection, distance);
        cameraController.TeleportCamera(player.transform.position, false);
        RespondToMovement(autoDirection / 2);
        InputReceived();
    }

    public void StartAutoWalkProtocol(int direction) // 8 is spiral, speed is 20 times faster
    {
        if (direction == 8)
        {
            StartAutoWalkProtocol(dimensions, secPerGen * dimensions);
        }
        else
        {
            StartAutoWalkProtocol(dimensions, secPerGen * dimensions * dimensions);
        }
    }


    public void StartAutoWalkProtocol(int dimension, float second)
    {
        if (autoWalkCoroutine != null)
        {
            StopCoroutine(autoWalkCoroutine);
        }
        autoWalkCoroutine = StartCoroutine(AutoWalk(dimension, second));
    }

    // Coroutine that moves the player at intervals
    private IEnumerator AutoWalk(int dimension, float interval)
    {
        while (player.totalSteps < player.MaxSteps)
        {
            WalkOnce(dimension / 2);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator AutoWalk(int dimension, float timeDelay, int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            WalkOnce(1);
            yield return new WaitForSeconds(timeDelay);
        }

        // When automated walk is finished, log end time
        playerEndTime = Time.realtimeSinceStartup;
        isAutoMoveActive = false;  // Disable flag after auto-move completes
        Debug.Log("Automated movement finished.");
        WriteLogToFile();  // Write the log to file
    }


    // Stop the AutoWalk
    public void StopAutoWalkProtocol()
    {
        // player.totalSteps = player.MaxSteps;
        if (autoWalkCoroutine != null)
        {
            StopCoroutine(autoWalkCoroutine);
        }
    }

    // ================== End of AutoWalk ==================

    // ================== Start of Logging ==================

    public void LogGridCompletionTime(float timeTaken, int backSteps)
    {
        // Debug.Log("LogGridCompletionTime called" + timeTaken);
        gridCompletionTimesInMilliSec.Add(timeTaken);
        backedSteps.Add(backSteps);
    }

    void LogAllWFCTime()
    {
        foreach (var wfc in waveFunctionMap.Values)
        {
            LogGridCompletionTime(wfc.GetGenTime(), wfc.GetUseBacktrack());
        }
    }

    public void WriteLogToFile()
    {
        LogAllWFCTime();
        // Debug.Log(gridCompletionTimesInMilliSec.Count);
        string date = System.DateTime.Now.ToString("MM_dd_HH_mm_ss");
        string fileName = $"{dimensions}x{dimensions}_Spd-{timeDelayPerStep}_Tile-{tileDropdown.value}_{date}.txt";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log($"Saving log to: {filePath}");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Calculate the average time
            float averageTime = gridCompletionTimesInMilliSec.Count > 0 ?
                                gridCompletionTimesInMilliSec.Average() : 0;

            writer.WriteLine("===Summary===");
            writer.WriteLine($"{dimensions}x{dimensions}"); //dimensions
            writer.WriteLine($"{timeDelayPerStep}"); //speed
            writer.WriteLine($"{player.totalSteps}"); //steps
            writer.WriteLine($"{playerEndTime - playerStartTime:F9}"); //total time
            writer.WriteLine($"{averageTime:F9}"); //wfc average time in milli sec
            writer.WriteLine($"{gridCount}");//grid count
            writer.WriteLine($"{cellCount}");//cell count
            writer.WriteLine($"{currentTileSet}");//tile type
            writer.WriteLine("===Summary===");


            writer.WriteLine("===Details===");
            // Write player movement times (only during automated movement)
            if (isAutoMoveActive)
            {
                writer.WriteLine($"Total Player Movement Time (Sec): \n{playerEndTime - playerStartTime:F9}");
                // input delay
                writer.WriteLine($"Player Input Delay (Sec): \n{timeDelayPerStep:F9}");
                writer.WriteLine($"Player Total Steps: {player.totalSteps}");
                writer.WriteLine($"Player Start Time: {playerStartTime:F9} miliseconds");
                writer.WriteLine($"Player End Time: {playerEndTime:F9} miliseconds");
            }


            // Write the average time first
            writer.WriteLine($"\nGrid Formation:\nAverage Formation Time: {averageTime:F9} miliseconds\n");
            // Write grid details
            writer.WriteLine($"Grid size: {dimensions}x{dimensions}");
            writer.WriteLine($"Grid count: {gridCount}, Cell count: {cellCount}");
            writer.WriteLine($"Tile type / amount: \n{currentTileSet.name} / {CurrentTiles.Length}");
            // writer.WriteLine($"Average each tile connections: {CurrentTiles.Average(t => t.ConnectionsCounts)}");

            // Write all times in detail
            writer.WriteLine("Grid Formation Times:");
            if (gridCompletionTimesInMilliSec.Count != backedSteps.Count)
            {
                Debug.Log($"CountIssue {gridCompletionTimesInMilliSec.Count}   {backedSteps.Count}");
            }
            for (int i = 0; i < gridCompletionTimesInMilliSec.Count; i++)
            {
                writer.WriteLine(gridCompletionTimesInMilliSec[i].ToString("F9") + " milli-seconds");
                writer.WriteLine(backedSteps[i].ToString());
            }
        }

        Debug.Log($"Log saved to: {filePath}");
    }

    // ================== End of Logging ==================




    // ======= Speed Tests =======
    // Method to start auto walking
    public void StartAutoWalk(int steps, float timeDelay)
    {
        if (autoWalkCoroutine != null)
        {
            StopCoroutine(autoWalkCoroutine);
        }
        autoWalkCoroutine = StartCoroutine(AutoWalk(dimensions, timeDelay, steps));
    }

    // Method to be called when player starts moving
    public void OnPlayerStartMove()
    {
        if (isAutoMoveActive)
        {
            playerStartTime = Time.realtimeSinceStartup; // Capture the player's start time
            Debug.Log("Automated player movement started.");
        }
    }


    // ======= Speed Tests UI =======


    // Method to handle time delay input change
    public void OnTimeDelayInputChanged(string value)
    {
        if (float.TryParse(value, out float newTimeDelay))
        {
            timeDelayPerStep = newTimeDelay;
        }
    }

    // Method to handle steps input change
    public void OnStepsInputChanged(string value)
    {
        if (int.TryParse(value, out int newSteps))
        {
            numberOfSteps = newSteps;
        }
    }

    // When the move button is clicked, trigger the auto walk with updated inputs
    public void OnMoveButtonClicked()
    {
        isAutoMoveActive = true;  // Set the flag to indicate automated movement
        player.totalSteps = 0;

        OnPlayerStartMove();  // Log the start time
        // Use timeDelayPerStep and numberOfSteps to start the player's movement
        StartAutoWalk(numberOfSteps, timeDelayPerStep);
    }

    public void StopAutoMove()
    {
        isAutoMoveActive = false; // Disable automated movement flag
        StopAutoWalkProtocol();
    }


    // ================== End of Speed Tests ==================



    // Coroutine for running the speed test
    private IEnumerator SpeedTest()
    {
        // Record the start time
        speedTestStartTime = Time.realtimeSinceStartup;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        gridsGenerated = 0;

        WaveFunction firstWFC = CreateFirstWFC();

        firstWFC.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
        firstWFC.Initialize();
        firstWFC.RunWFC();

        // yield return new WaitUntil(() => firstWFC.gridComplete); // Wait until first WFC is done
        gridsGenerated++;
        // Sequentially generate grids to the right, extending from the previous grid
        WaveFunction currentWFC = firstWFC;

        // Vector2Int offset = new Vector2Int(dimensions - 1, 0); // Move to the right

        for (int i = 1; i < numberOfGrids; i++)
        {
            // WaveFunction newWFC = CreateWFC((Vector2Int)currentWFC.transform.position + offset, transform);
            WaveFunction newWFC = CreateWFC2D(new Vector2Int(i, 0));
            newWFC.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
            newWFC.Initialize();
            newWFC.RunWFC();

            // yield return new WaitUntil(() => newWFC.gridComplete); // Wait for this WFC to finish collapsing

            currentWFC = newWFC; // Update current WFC for next iteration
            gridsGenerated++;
        }
        yield return new WaitUntil(() => currentWFC.gridComplete); // Wait for the last grid to finish collapsing
        stopwatch.Stop();
        // Record the end time
        speedTestEndTime = Time.realtimeSinceStartup;
        // float totalTimeTaken = stopwatch.ElapsedMilliseconds;
        float totalTimeTaken = (speedTestEndTime - speedTestStartTime) * 1000; // Convert to milliseconds

        LogSpeedTestResults(totalTimeTaken);
    }

    // Method to log the results of the speed test
    private void LogSpeedTestResults(float totalTime)
    {
        string date = System.DateTime.Now.ToString("MM_dd_HH_mm_ss");
        string fileName = $"SpeedTest_{dimensions}x{dimensions}_{numberOfGrids}_Grids_{date}.txt";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("===Speed Test Results===");
            writer.WriteLine($"Grid size: {dimensions}x{dimensions}");
            writer.WriteLine($"Total grids generated: {gridsGenerated}");
            writer.WriteLine($"Total time taken (ms): {totalTime:F9}");
            writer.WriteLine("===End of Speed Test Results===");
        }

        // Debug.Log($"Speed test log saved to: {filePath}");
    }

    // Method to handle speed test toggle changes
    public void OnSpeedTestToggleChanged(bool isOn)
    {
        runSpeedTest = isOn;  // Update the runSpeedTest flag based on toggle value
        DestroyAllWFCs();
        if (runSpeedTest)
        {
            secPerGen = 0.00000001f;
            StartCoroutine(BulkSpeedRun()); // Start speed test
        }
        else
        {
            // If speed test is disabled, you can stop or reset any ongoing operations
            StopAllCoroutines();  // Stop all coroutines including any running speed tests
            Restart();
        }
    }

    void ShowFinishedText()
    {
        // Check if the finishedTextPrefab is set
        if (finishedTextPrefab == null)
        {
            Debug.LogError("Finished Text Prefab is not assigned.");
            return;
        }

        // Get the player's position in world space
        Vector3 playerWorldPosition = player.transform.position;

        // Convert the player's world position to screen space (so it can appear on the canvas)
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(playerWorldPosition + new Vector3(0, 1.5f, 0));  // Adjust Y-axis to move it above the player

        // Instantiate the "Finished" text prefab at the player's screen position
        GameObject finishedTextInstance = Instantiate(finishedTextPrefab, screenPosition, Quaternion.identity, canvas.transform);

        // Optionally, you can animate or destroy the text after some time
        Destroy(finishedTextInstance, 10f);  // Destroy after 3 seconds
    }





    // BulkSpeedRun now runs multiple speed tests in sequence
    IEnumerator BulkSpeedRun()
    {
        // Set the generation time to a very low value for speed tests 
        for (int i = 0; i < bulkSpeedRunTimes; i++)
        {
            // Start the speed test
            yield return StartCoroutine(SpeedTest());  // Wait for the speed test to finish

            // After each speed test, log the result and restart the world
            if (i < bulkSpeedRunTimes - 1) // Don't restart after the last test
            {
                yield return StartCoroutine(RestartCoroutine());  // Wait for the world to restart before starting the next run
            }
        }

        // Debug.Log("Bulk Speed Test completed!");
    }

    IEnumerator RestartCoroutine_Walk()
    {

        StopAutoWalkProtocol();

        yield return new WaitForEndOfFrame();  // Ensure the restart finishes before starting the next run
    }

    // ================== End of Speed Tests ==================

    // =====Start of MultipleGenerationTest

    public void On30GenerateNxNClicked()
    {
        Restart();
        StartCoroutine(GenerateNxNTilesMultipleTimes());
    }

    private IEnumerator GenerateNxNTilesMultipleTimes()
    {
        string date = DateTime.Now.ToString("MM_dd_HH_mm_ss");
        string fileName = $"{dimensions}x{dimensions}_ThirtyTest_{date}.txt";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);


        using (StreamWriter writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < 30; i++)
            {
                // Create a WaveFunction grid for the current NxN tile type
                WaveFunction wfc = CreateWFC2D(Vector2Int.zero);
                wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
                wfc.Initialize();
                wfc.RunWFC();

                // Wait until the grid generation completes
                yield return new WaitUntil(() => wfc.gridComplete);

                float genTime = wfc.GetGenTime(); // convert to milliseconds

                writer.WriteLine($"{genTime:F9}"); // Log the time in milliseconds

                // Clean up after each generation
                Destroy(wfc.gameObject);
                waveFunctionMap.Remove(GetWFCWorldPos2DInt(wfc));
            }
        }

        Debug.Log($"Log saved to: {filePath}");
    }

    public void On30GenerateNxN8NeighborClicked()
    {
        Restart();
        StartCoroutine(GenerateNxNTiles8Neighbor());
    }

    private IEnumerator GenerateNxNTiles8Neighbor()
    {
        string date = DateTime.Now.ToString("MM_dd_HH_mm_ss");
        string fileName = $"{dimensions}x{dimensions}_Thirty8NeighborTest_{date}.txt";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Prepare the 8 neighbors once around a center position
        Vector3 centerPosition = Vector3.zero;
        yield return StartCoroutine(PrepareNeighborGrids3D(centerPosition));

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < 30; i++)
            {
                // Create a WaveFunction grid for the current NxN tile type
                WaveFunction wfc = CreateWFC2D(Vector2Int.zero);
                wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
                wfc.Initialize();
                wfc.RunWFC();

                // Wait until the grid generation completes
                yield return new WaitUntil(() => wfc.gridComplete);

                float genTime = wfc.GetGenTime(); // convert to milliseconds

                writer.WriteLine($"{genTime:F9}"); // Log the time in milliseconds

                waveFunctionMap.Remove(GetWFCWorldPos2DInt(wfc));

                // Clean up after each generation
                Destroy(wfc.gameObject);
            }
        }

        Debug.Log($"Log saved to: {filePath}");
    }

    // Prepare 8 surrounding neighbors only once at the beginning
    private IEnumerator PrepareNeighborGrids3D(Vector3 centerPosition)
    {
        Vector3[] directions = {
            Vector3.up, Vector3.right, Vector3.down, Vector3.left,
            Vector3.up + Vector3.right, Vector3.up + Vector3.left,
            Vector3.down + Vector3.right, Vector3.down + Vector3.left
        };

        foreach (Vector3 direction in directions)
        {
            Vector3 neighborPos = centerPosition + (direction * (dimensions - 1));
            WaveFunction neighborWfc = CreateWFC(neighborPos, transform);
            neighborWfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
            neighborWfc.Initialize();
            neighborWfc.RunWFC();
            yield return new WaitUntil(() => neighborWfc.gridComplete); // Wait for each neighbor to complete
        }
    }

    // End of MultipleGenerationTest======

    // ======Start of Coverage Test=======

    // Setting KxK as the length to extend
    [Header("Coverage Test Settings")]
    public int K = 30; // The length to extend
    public bool waitForInsiders = false, TestCoverage = false; // Toggle to wait for insiders
    public double CoverStopwactTime = 0.0f;
    private System.Diagnostics.Stopwatch stopwatchUniversal;
    public TMP_InputField KInputField; // Input field to dynamically set K

    public void OnCoverTestBButtonClicked()
    {
        Restart();
        // start time
        float startTime = Time.realtimeSinceStartup;
        stopwatchUniversal = System.Diagnostics.Stopwatch.StartNew();
        if (KInputField != null)
        {
            int newK;
            if (int.TryParse(KInputField.text, out newK))
            {
                K = newK;
            }
        }

        int amount = CalculateGridLengthLfromK(K, dimensions);
        CreateLxL(amount);

        // wait for all grids to complete
        StartCoroutine(WaitForAllGrids(startTime, amount * amount));
    }

    // <summary>
    // Create LxL grids near the center, runs WFC on each grid in the end
    // </summary>
    private void CreateLxL(int amount)
    {
        int halfAmount = amount / 2;
        for (int i = 0; i < amount; i++)
        {
            for (int j = 0; j < amount; j++)
            {
                WaveFunction wfc = CreateWFC2D(new Vector2Int(i - halfAmount, j - halfAmount));
                wfc.name = $"WFC_{i - halfAmount}_{j - halfAmount}";
                wfc.InitializeGridProperties(dimensions, cellPrefab, secPerGen, this);
                wfc.Initialize();
            }
        }
        foreach (WaveFunction wfc in waveFunctionMap.Values)
        {
            wfc.RunWFC();
        }
    }

    // Coroutine to wait for all grids to complete
    private IEnumerator WaitForAllGrids(float startTime, int gridCount)
    {
        // yield return new WaitUntil(() => AllWFCs.TrueForAll(wfc => wfc.gridComplete));
        yield return new WaitUntil(() => waveFunctionMap.Values.All(wfc => wfc.gridComplete));
        stopwatchUniversal.Stop();
        // end time
        float endTime = Time.realtimeSinceStartup;
        CoverStopwactTime = stopwatchUniversal.Elapsed.TotalMilliseconds;
        LogCoverTestResults(CoverStopwactTime, endTime, gridCount, "MacroGrid");
    }

    // Calculate how many gridLength L to generate in each direction to cover K cell length
    private int CalculateGridLengthLfromK(int K, int dimensions)
    {
        return Mathf.CeilToInt((float)(K - dimensions) / (dimensions - 1));
    }

    // Coroutine to generate neighbors
    private IEnumerator GenerateNeighbors(List<WaveFunction> neighbors)
    {
        foreach (WaveFunction neighbor in neighbors)
        {
            if (neighbor != null)
            {
                neighbor.RunWFC();
                // if (waitForInsiders)
                // {
                yield return new WaitUntil(() => neighbor.gridComplete); // Wait until the grid is complete
                // }
            }
        }
        yield return null;
    }

    // Log the results of covertest
    private void LogCoverTestResults(double startTime, float endTime, int gridCount, string TestName = "Cover Test")
    {
        string date = System.DateTime.Now.ToString("MM_dd_HH_mm_ss");
        string fileName = $"CoverTest_{dimensions}x{dimensions}_{K}_{TestName}_{date}.txt";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        // Debug.Log($"Saving log file to: {filePath}");
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("===Cover Test Results===");
            writer.WriteLine($"Grid NxN: {dimensions}");
            writer.WriteLine($"Total Dimension exceeded KxK = {K}");
            // writer.WriteLine($"Total time taken: {endTime - startTime:F9} seconds");
            writer.WriteLine($"Total time taken: {startTime:F9} milli-seconds");
            writer.WriteLine($"Total grids generated: {gridCount}");
            writer.WriteLine("===End of Cover Test Results===");
        }

        // Debug.Log($"Cover test log saved to: {filePath}");
    }


    // ======End of Coverage Test=======


    [ContextMenu("TestingGridSystem")]
    public void TestingGridSystem()
    {

        foreach (WaveFunction wfc in waveFunctionMap.Values)
        {
            Vector3Int position = new Vector3Int((int)wfc.transform.position.x, (int)wfc.transform.position.y, 0);
            Debug.Log("WFC: " + wfc.name + " at " + position);
        }
        Debug.Log($"{_worldGrid.GetCellCenterLocal(new Vector3Int(0, 0, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(1, 1, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(1, -1, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(0, -1, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(-1, -1, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(-1, 0, 0))} {_worldGrid.GetCellCenterWorld(new Vector3Int(-1, 1, 0))}");
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(0, 0, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(1, 1, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(0, -1, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(-1, 0, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(-1, -1, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(-1, 1, 0)), Quaternion.identity, transform);
        Instantiate(waveFunctionPrefab, _worldGrid.GetCellCenterWorld(new Vector3Int(1, -1, 0)), Quaternion.identity, transform);

    }

}
