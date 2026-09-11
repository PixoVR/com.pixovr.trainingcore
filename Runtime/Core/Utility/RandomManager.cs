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
