using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileSetOptions", menuName = "Tiles/Tile Set Options")]
public class TileSetOptions : ScriptableObject
{
    [Tooltip("List of tiles in this set.")]
    public List<TileData> tiles = new List<TileData>();
    public List<EffectRule> effectRules = new List<EffectRule>();

    [Tooltip("Current effect rule to be applied.")]
    public EffectRule currentEffectRule;

    // [Tooltip("Specify which tiles should be affected by the current rule.")]
    // public List<TileData> affectedTiles = new List<TileData>();

    [Tooltip("List of fallback tiles to use when no other tile is available.")]
    public List<Tile> fallbackTiles = new List<Tile>(); // Multiple fallback tiles

    public List<TileData> toBeCombinedTiles = new List<TileData>();

    public List<Tile> LoadingTiles = new List<Tile>();

    public void ResetTileWeightsToDefault()
    {
        // AbsorbAndCombineOptions();
        for (int i = 0; i < tiles.Count; i++)
        {
            Tile tile = tiles[i].tilePrefab;
            tile.weight = tiles[i].defaultWeight; // Reset to default
        }
    }

    public void ApplyAllRules(Vector3 playerPosition)
    {
        foreach (var rule in effectRules)
        {
            rule.ApplyRule(playerPosition, tiles);
        }
    }

    public void ApplyCurrentRule(Vector3 playerPosition)
    {
        if (currentEffectRule == null) return;
        currentEffectRule.ApplyRule(playerPosition, tiles);
    }

    public Tile GetFallBackTile()
    {
        if (fallbackTiles.Count == 0)
        {
            Debug.LogWarning("No fallback tiles available. Returning null.");
            return null;
        }

        // Randomly select one tile from the fallbackTiles list
        int randomIndex = Random.Range(0, fallbackTiles.Count);
        return fallbackTiles[randomIndex];
    }

    // Get fallbacktile by index
    public Tile GetFallBackTile(int index)
    {
        if (index < 0 || index >= fallbackTiles.Count)
        {
            Debug.LogWarning("Invalid fallback tile index. Returning null.");
            return null;
        }

        return fallbackTiles[index];
    }

    // Select a fallback tile based on adjacency rules or other criteria
    public Tile GetFallBackTile(Tile existingTile)
    {
        if (fallbackTiles.Count == 0)
        {
            Debug.LogWarning("No fallback tiles available. Returning null.");
            return null;
        }

        // Example logic: choose a fallback tile based on the adjacency of tileA and tileB
        // You can customize this logic to suit your needs
        foreach (Tile fallbackTile in fallbackTiles)
        {
            if (IsCompatibleWithAdjacentTiles(fallbackTile, existingTile))
            {
                return fallbackTile;
            }
        }

        // If no compatible tile is found, return a random fallback tile as a default
        return GetFallBackTile();
    }

    [ContextMenu("Combine")]
    public void AbsorbAndCombineOptions()
    {
        if (toBeCombinedTiles == null || toBeCombinedTiles.Count == 0)
        {
            Debug.LogWarning("No tiles to combine. Aborting.");
            return;
        }

        // Combine tiles from toBeCombinedTiles into the main tiles list
        foreach (TileData newTileData in toBeCombinedTiles)
        {
            // Check if the tile already exists in the main tiles list
            TileData existingTileData = tiles.Find(td => td.tilePrefab == newTileData.tilePrefab);

            if (existingTileData != null)
            {
                // If the tile exists, increase its weight (or adjust in any other way as needed)
                existingTileData.defaultWeight += newTileData.defaultWeight;
            }
            else
            {
                // If the tile does not exist, add it as new
                tiles.Add(new TileData { tilePrefab = newTileData.tilePrefab, defaultWeight = newTileData.defaultWeight });
            }
        }

        // Optionally, clear the toBeCombinedTiles list after merging
        toBeCombinedTiles.Clear();

        Debug.Log("Tiles from toBeCombinedTiles have been absorbed and combined into the main list.");
    }

    // Helper method to determine if a fallback tile is compatible with adjacent tiles
    private bool IsCompatibleWithAdjacentTiles(Tile fallbackTile, Tile existingTile)
    {
        if (fallbackTile.subTile == null) return false;

        return fallbackTile.subTile.ThisNeighborHasMe(fallbackTile);
    }

    [ContextMenu("LoadTilesFromList")]
    public void LoadTilesFromList()
    {
        if (LoadingTiles == null || LoadingTiles.Count == 0)
        {
            Debug.LogWarning("No tiles to load. Aborting.");
            return;
        }

        // Combine tiles from LoadingTiles into the main tiles list
        foreach (Tile newTile in LoadingTiles)
        {
            // Check if the tile already exists in the main tiles list
            TileData existingTileData = tiles.Find(td => td.tilePrefab == newTile);

            if (existingTileData != null)
            {
                // If the tile exists, increase its weight (or adjust in any other way as needed)
                existingTileData.defaultWeight += 1.0f;
            }
            else
            {
                // If the tile does not exist, add it as new
                tiles.Add(new TileData { tilePrefab = newTile, defaultWeight = 1.0f });
            }
        }

        // Optionally, clear the toBeCombinedTiles list after merging
        LoadingTiles.Clear();

        Debug.Log("Tiles from LoadingTiles have been absorbed and combined into the main list.");
    }
}

[System.Serializable]
public class TileData
{
    public Tile tilePrefab; // Reference to the tile prefab
    public float defaultWeight = 1.0f; // Default weight for the tile

    public TileData()
    {
        tilePrefab = null;
        defaultWeight = 1.0f;
    }

    public TileData(Tile tilePrefab)
    {
        this.tilePrefab = tilePrefab;
        // this.defaultWeight = defaultWeight;
    }
}


// -s- Interface for effect rules -s-
// abstract class for effect rules
public abstract class EffectRule : ScriptableObject
{
    [Tooltip("List of tiles this rule can affect.")]
    public List<TileData> affectedTiles = new List<TileData>();
    public abstract string RuleName { get; }
    public abstract void ApplyRule(Vector3 playerPosition, List<TileData> tiles);
}
