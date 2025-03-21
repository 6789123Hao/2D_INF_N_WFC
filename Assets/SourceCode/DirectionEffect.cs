using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Adjusts tile weights based on the player's position relative to cardinal directions.
/// </summary>
[CreateAssetMenu(fileName = "DirectionEffect", menuName = "Rules/Direction Effect")]
public class DirectionEffect : EffectRule
{
    public override string RuleName => "Direction Effect";
    [Tooltip("Direction-based weighting factors.")]
    public bool PositiveX, NegativeX, PositiveY, NegativeY;
    public float scalingFactor = 10f;

    /// <summary>
    /// Applies directional weight scaling based on player position.
    /// </summary>
    public override void ApplyRule(Vector3 playerPosition, List<TileData> tiles)
    {
        foreach (var tile in tiles)
        {
            // Skip if this tile is not affected by the rule
            if (!affectedTiles.Any(affectedTile => affectedTile.tilePrefab == tile.tilePrefab))
                continue;


            // these tile weight are sum of player's x and y coordinates mod scaling factor;
            float weight = CalculateWeight(playerPosition);

            tile.tilePrefab.weight = weight / scalingFactor;
        }
    }

    /// <summary>
    /// Calculates weight based on enabled directional factors.
    /// factor into number of true directions, if all are true, times 2 divide by 4
    /// </summary>
    private float CalculateWeight(Vector3 playerPosition)
    {
        int activeDirections = 0;
        float weight = 0;

        if (PositiveX) { weight += playerPosition.x; activeDirections++; }
        if (NegativeX) { weight -= playerPosition.x; activeDirections++; }
        if (PositiveY) { weight += playerPosition.y; activeDirections++; }
        if (NegativeY) { weight -= playerPosition.y; activeDirections++; }

        return activeDirections > 0 ? (weight * 2 / activeDirections) : 0;
    }
}

