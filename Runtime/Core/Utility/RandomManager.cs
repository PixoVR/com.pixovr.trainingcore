using UnityEngine;
using System;
using System.Collections.Generic;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>
    /// Seeded RNG manager. Each named RNG (plus the anonymous default) keeps an independent
    /// <see cref="System.Random"/> so multi-user sessions stay deterministic.
    /// </summary>
    public class RandomManager : SingletonBehaviour<RandomManager>
    {
        private readonly Dictionary<Guid, System.Random> _rngs = new Dictionary<Guid, System.Random>();

        /// <summary>Re-seed all RNGs (clears named generators).</summary>
        public virtual void ResetRandoms() => _rngs.Clear();

        /// <summary>Next value from the anonymous RNG.</summary>
        public double Next() => Next(Guid.Empty);

        /// <summary>Next value from a named RNG.</summary>
        public double Next(Guid guid) => GetRng(guid).NextDouble();

        /// <summary>Next value from a named RNG (name converted to a Guid).</summary>
        public double Next(string name) => Next(new Guid(ToBytes(name)));

        /// <summary>Draw a random item from a list.</summary>
        public T GetRandomItem<T>(List<T> list) => GetRandomItem(Guid.Empty, list);

        /// <summary>Draw a random item from a list using a named RNG.</summary>
        public T GetRandomItem<T>(Guid guid, List<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[GetRng(guid).Next(list.Count)];
        }

        /// <summary>Draw a random item using a named RNG (name converted to a Guid).</summary>
        public T GetRandomItem<T>(string name, List<T> list) => GetRandomItem(new Guid(ToBytes(name)), list);

        /// <summary>Compute a deterministic double in [0,1) for a guid+index pair (used for synced data).</summary>
        public static double ComputeDeterministicValue(Guid guid, int index)
        {
            var bytes = guid.ToByteArray();
            int seed = (int)(uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24)) ^ index;
            return new System.Random(seed).NextDouble();
        }

        /// <summary>Guid for the anonymous RNG.</summary>
        public Guid NextGuid() => Guid.NewGuid();

        private System.Random GetRng(Guid guid)
        {
            if (guid == Guid.Empty)
                guid = Guid.Empty;
            if (!_rngs.TryGetValue(guid, out var rng))
            {
                var bytes = guid.ToByteArray();
                int seed = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
                rng = new System.Random(seed);
                _rngs[guid] = rng;
            }
            return rng;
        }

        private static byte[] ToBytes(string name)
        {
            var bytes = new byte[16];
            var src = System.Text.Encoding.UTF8.GetBytes(name ?? string.Empty);
            Array.Copy(src, bytes, Math.Min(16, src.Length));
            return bytes;
        }
    }
}

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>List helpers used by step scripts.</summary>
    public static class ListUtility
    {
        /// <summary>In-place Fisher-Yates shuffle.</summary>
        public static void Shuffle<T>(this System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}


namespace PixoVR.TrainingCore.Utils
{
    /// <summary>Places players in a circle around a multiuser anchor.</summary>
    public static class CircularPlayerPlacer
    {
        /// <summary>Calculate a player's spawn position.</summary>
        /// <param name="anchorCenter">Center of the spawn circle.</param>
        /// <param name="angleMin">Minimum placement angle (degrees).</param>
        /// <param name="angleMax">Maximum placement angle (degrees).</param>
        /// <param name="multiuserAnchorPosition">Optional anchor offset.</param>
        /// <param name="spacingRadius">Circle radius.</param>
        public static Vector3 CalculatePlayerLocation(Vector3 anchorCenter, float angleMin, float angleMax, Vector3 multiuserAnchorPosition = default, float spacingRadius = 0.4f)
        {
            float angle = UnityEngine.Random.Range(angleMin, angleMax) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * spacingRadius;
            return anchorCenter + multiuserAnchorPosition + offset;
        }
    }
}


namespace PixoVR.TrainingCore.Utils
{
    /// <summary>LayerMask helper extensions.</summary>
    public static class LayerMaskExtensions
    {
        /// <summary>Add a layer index to a mask value.</summary>
        public static LayerMask AddLayerToLayerMask(this LayerMask mask, int layer) => mask | (1 << layer);

        /// <summary>Remove a layer index from a mask value.</summary>
        public static LayerMask RemoveLayerFromLayerMask(this LayerMask mask, int layer) => mask & ~(1 << layer);
    }
}
