using UnityEngine;
using System.Collections.Generic;
using System.Linq;


[CreateAssetMenu(fileName = "MultipleEffect", menuName = "Rules/MultipleEffect")]
public class MultipleEffect : EffectRule
{
    public List<EffectRule> rules = new List<EffectRule>();

    public override string RuleName => "Multiple Effect";

    public override void ApplyRule(Vector3 playerPosition, List<TileData> tiles)
    {
        foreach (var rule in rules)
        {
            rule.ApplyRule(playerPosition, tiles);
        }
    }
}

