using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TileRandom extends Tile to introduce random sprite selection and optional random item spawning.
/// </summary>
public class TileRandom : Tile
{
    [Header("Random Tile Options")]
    public List<Sprite> randomSprites = new List<Sprite>(); // List of possible sprites for this tile
    public float randomItemChance = 0.1f; // Probability of spawning a random item
    public List<Sprite> randomItems = new List<Sprite>(); // List of possible random items


    /// <summary>
    /// Overrides GetTileObject to apply a random sprite from the available list.
    /// </summary>
    public override GameObject GetTileObject(Vector3 position, Quaternion rotation)
    {
        GameObject tileObject = base.GetTileObject(position, rotation);
        
        if (randomSprites != null && randomSprites.Count > 0)
        {
            SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = randomSprites[UnityEngine.Random.Range(0, randomSprites.Count)];
            }
        }
        return tileObject;
    }

    /// <summary>
    /// Overrides OnTileSpawned to determine if a random item should be spawned.
    /// </summary>
    public override void OnTileSpawned()
    {
        base.OnTileSpawned();

        // Check if a random item should be spawned
        if (UnityEngine.Random.value < randomItemChance && randomItems != null && randomItems.Count > 0)
        {
            SpawnRandomItem();
        }
    }

    /// <summary>
    /// Spawns a random item as a child object of the tile.
    /// </summary>
    private void SpawnRandomItem()
    {
        GameObject itemObject = new GameObject("RandomItem");
        itemObject.transform.SetParent(transform);
        itemObject.transform.localPosition = new Vector3(0, -0.1f, -0.1f); // Position slightly above the tile

        SpriteRenderer renderer = itemObject.AddComponent<SpriteRenderer>();
        renderer.sprite = randomItems[UnityEngine.Random.Range(0, randomItems.Count)];
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(0.8f, 0.8f);
        
        // Match sorting layer with parent tile
        SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            renderer.sortingLayerName = parentRenderer.sortingLayerName;
            renderer.sortingOrder = parentRenderer.sortingOrder + 1; // Ensure item appears above the tile
        }
    }

}