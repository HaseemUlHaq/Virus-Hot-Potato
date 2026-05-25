using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identity of a virus in a formation puzzle (color, size tier, pulse, shape).
/// Used for order-free placeholder matching against <see cref="VirusFormationData"/>.
/// </summary>
public readonly struct FormationVirusKey : IEquatable<FormationVirusKey>
{
    public int MaterialIndex { get; }
    public float Scale { get; }
    public bool IsPulsating { get; }
    public int ShapeVariantIndex { get; }

    public FormationVirusKey(int materialIndex, float scale, bool isPulsating, int shapeVariantIndex)
    {
        MaterialIndex = materialIndex;
        Scale = NetworkGrabbableVirus.QuantizeScale(scale);
        IsPulsating = isPulsating;
        ShapeVariantIndex = shapeVariantIndex;
    }

    public static FormationVirusKey FromConfig(VirusFormationData.SlotConfig config)
    {
        return new FormationVirusKey(
            config.materialIndex,
            config.scale,
            config.isPulsating,
            config.shapeVariantIndex);
    }

    public bool MatchesVirus(NetworkGrabbableVirus virus, float scaleTolerance)
    {
        if (virus == null) return false;
        if (virus.MaterialIndex != MaterialIndex) return false;
        if (Mathf.Abs(virus.VirusScale - Scale) > scaleTolerance) return false;
        if ((bool)virus.IsPulsating != IsPulsating) return false;
        if (virus.ShapeVariantIndex != ShapeVariantIndex) return false;
        return true;
    }

    public bool Equals(FormationVirusKey other) =>
        MaterialIndex == other.MaterialIndex &&
        Scale.Equals(other.Scale) &&
        IsPulsating == other.IsPulsating &&
        ShapeVariantIndex == other.ShapeVariantIndex;

    public override bool Equals(object obj) => obj is FormationVirusKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = MaterialIndex;
            hash = (hash * 397) ^ Scale.GetHashCode();
            hash = (hash * 397) ^ IsPulsating.GetHashCode();
            hash = (hash * 397) ^ ShapeVariantIndex;
            return hash;
        }
    }

    public static bool TryMatchAny(
        IReadOnlyList<FormationVirusKey> required,
        NetworkGrabbableVirus virus,
        float scaleTolerance,
        out FormationVirusKey matched)
    {
        matched = default;
        if (required == null || virus == null) return false;

        for (int i = 0; i < required.Count; i++)
        {
            if (required[i].MatchesVirus(virus, scaleTolerance))
            {
                matched = required[i];
                return true;
            }
        }

        return false;
    }

    public static bool CountsMatch(
        IReadOnlyList<FormationVirusKey> required,
        IReadOnlyList<NetworkGrabbableVirus> placed,
        float scaleTolerance)
    {
        if (required == null || placed == null || required.Count != placed.Count)
            return false;

        var requiredCounts = new Dictionary<FormationVirusKey, int>();
        for (int i = 0; i < required.Count; i++)
        {
            FormationVirusKey key = required[i];
            requiredCounts.TryGetValue(key, out int count);
            requiredCounts[key] = count + 1;
        }

        var placedCounts = new Dictionary<FormationVirusKey, int>();
        for (int i = 0; i < placed.Count; i++)
        {
            NetworkGrabbableVirus virus = placed[i];
            if (!TryMatchAny(required, virus, scaleTolerance, out FormationVirusKey key))
                return false;

            placedCounts.TryGetValue(key, out int count);
            placedCounts[key] = count + 1;
        }

        if (placedCounts.Count != requiredCounts.Count)
            return false;

        foreach (var kv in requiredCounts)
        {
            if (!placedCounts.TryGetValue(kv.Key, out int placedCount) || placedCount != kv.Value)
                return false;
        }

        return true;
    }
}
