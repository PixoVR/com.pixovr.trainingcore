using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.TrainingCore.Identity
{
    /// <summary>
    /// Process-wide map of guid → live GameObject. Supports deferred resolution:
    /// callbacks passed to <see cref="Resolve(Guid, Action{GameObject}, Action)"/> are invoked
    /// when the target registers/unregisters later.
    /// </summary>
    public static class GuidRegistry
    {
        private struct Entry
        {
            public GameObject Target;
            public Action<GameObject> OnAdded;
            public Action OnRemoved;

            public void NotifyAdd(GuidComponent c)
            {
                Target = c.gameObject;
                OnAdded?.Invoke(Target);
            }

            public void NotifyRemove()
            {
                Target = null;
                OnRemoved?.Invoke();
            }
        }

        private static readonly Dictionary<Guid, Entry> Map = new Dictionary<Guid, Entry>();
        private static readonly Dictionary<Guid, Entry> Pending = new Dictionary<Guid, Entry>();

        /// <summary>Registers a component; false if another object already holds the guid.</summary>
        public static bool Add(GuidComponent component)
        {
            var guid = component.GetGuid();
            if (guid == Guid.Empty)
                return false;

            if (Map.TryGetValue(guid, out var existing))
            {
                if (existing.Target == component.gameObject)
                    return true;
                if (existing.Target != null)
                    return false;
            }

            Entry entry = existing;
            if (Pending.TryGetValue(guid, out var pending))
            {
                entry.OnAdded += pending.OnAdded;
                entry.OnRemoved += pending.OnRemoved;
                Pending.Remove(guid);
            }
            entry.NotifyAdd(component);
            Map[guid] = entry;
            return true;
        }

        /// <summary>Removes a guid registration and notifies pending removal listeners.</summary>
        public static void Remove(GuidComponent component) => Remove(component.GetGuid());

        /// <summary>Removes a guid registration and notifies pending removal listeners.</summary>
        public static void Remove(Guid guid)
        {
            if (Map.TryGetValue(guid, out var entry))
            {
                entry.NotifyRemove();
                Map.Remove(guid);
                Pending[guid] = entry;
            }
        }

        /// <summary>
        /// Returns the GameObject for <paramref name="guid"/> if registered; otherwise stores the
        /// callbacks and fires them when the object appears/disappears.
        /// </summary>
        public static GameObject Resolve(Guid guid, Action<GameObject> onAdded = null, Action onRemoved = null)
        {
            if (Map.TryGetValue(guid, out var entry) && entry.Target != null)
            {
                if (onAdded != null || onRemoved != null)
                {
                    var p = Pending.TryGetValue(guid, out var pend) ? pend : new Entry();
                    p.OnAdded += onAdded;
                    p.OnRemoved += onRemoved;
                    Pending[guid] = p;
                    var merged = entry;
                    merged.OnAdded += onAdded;
                    merged.OnRemoved += onRemoved;
                    Map[guid] = merged;
                }
                return entry.Target;
            }
            var waiting = Pending.TryGetValue(guid, out var w) ? w : new Entry();
            waiting.OnAdded += onAdded;
            waiting.OnRemoved += onRemoved;
            Pending[guid] = waiting;
            return null;
        }

        /// <summary>Resolves a guid string; null/empty/malformed strings return null.</summary>
        public static GameObject Resolve(string guid)
        {
            return Guid.TryParse(guid, out var g) ? Resolve(g) : null;
        }

        /// <summary>True when a guid currently maps to a live GameObject.</summary>
        public static bool IsRegistered(Guid guid) => Map.TryGetValue(guid, out var e) && e.Target != null;

        /// <summary>Drops all registrations and pending callbacks (tests / scene teardown).</summary>
        public static void Clear()
        {
            Map.Clear();
            Pending.Clear();
        }
    }
}
