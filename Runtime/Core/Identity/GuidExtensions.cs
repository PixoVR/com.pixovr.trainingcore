using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Identity
{
    /// <summary>Guid convenience accessors for GameObjects/Components carrying a <see cref="GuidComponent"/>.</summary>
    public static class GuidExtensions
    {
        /// <summary>Guid of the object's GuidComponent; Guid.Empty if none is present or assigned.</summary>
        public static Guid GetGuid(this GameObject go)
        {
            var c = go.GetComponent<GuidComponent>();
            return c == null ? Guid.Empty : c.GetGuid();
        }

        /// <summary>Guid of the component's GameObject; Guid.Empty if none.</summary>
        public static Guid GetGuid(this Component component) => component.gameObject.GetGuid();

        /// <summary>Guid as string; empty string when no guid is present.</summary>
        public static string GetGuidString(this GameObject go)
        {
            var g = go.GetGuid();
            return g == Guid.Empty ? string.Empty : g.ToString();
        }
    }
}
