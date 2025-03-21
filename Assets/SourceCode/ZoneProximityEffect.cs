using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ZoneProximityEffect increases tile weight based on proximity to predefined zone centers.
/// </summary>
[CreateAssetMenu(fileName = "ZoneProximityEffect", menuName = "Rules/Zone Proximity Effect")]
public class ZoneProximityEffect : EffectRule
{
    public override string RuleName => "Zone Proximity Effect";

    [Tooltip("Centers of influence zones.")]
    public Vector3[] zoneCenters;

    [Tooltip("Minimum and maximum distance range affecting tile weights.")]
    public float maxDistance = 50f;
    public float minDistance = 0f;

    [Tooltip("Maximum additional weight applied to tiles near zone centers.")]
    public float addingWeightMax = 25f;

    /// <summary>
    /// Applies the effect by adjusting tile weights based on the player's proximity to zone centers.
    /// </summary>
    public override void ApplyRule(Vector3 playerPosition, List<TileData> tiles)
    {
        foreach (var tile in tiles)
        {
            // Skip if this tile is not affected by the rule
            if (!affectedTiles.Any(affectedTile => affectedTile.tilePrefab == tile.tilePrefab))
                continue;

            float minWeight = tile.defaultWeight;
            float distance = Vector3.Distance(playerPosition, zoneCenters[0]); // Use first zone center for simplicity
            float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
            float weight = Mathf.Lerp(addingWeightMax + minWeight, minWeight, normalizedDistance);

            tile.tilePrefab.weight = weight;
        }
    }
}
