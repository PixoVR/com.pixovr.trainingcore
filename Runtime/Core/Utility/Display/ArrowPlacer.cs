using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Spawns an arrow pointing from a source object toward a target.</summary>
    public class ArrowPlacer : MonoBehaviour
    {
        /// <summary>Placement settings.</summary>
        public ArrowPlacerSettings Settings;

        /// <summary>Arrow prefab override (uses <see cref="ArrowPlacerSettings.ArrowPrefab"/> when null).</summary>
        public GameObject ArrowPrefab;

        /// <summary>The spawned arrow instance.</summary>
        public GameObject Arrow { get; private set; }

        /// <summary>Place an arrow on <paramref name="source"/> pointing at <paramref name="target"/>.</summary>
        public virtual void PlaceArrow(GameObject source, GameObject target)
        {
            var prefab = ArrowPrefab != null ? ArrowPrefab : Settings?.ArrowPrefab;
            if (prefab == null || source == null || target == null)
                return;
            if (Arrow == null)
            {
                Arrow = Instantiate(prefab);
                if (Settings != null && Settings.SpawnAsChild)
                    Arrow.transform.SetParent(source.transform, false);
            }
            float offset = Settings?.Offset ?? 0f;
            Arrow.transform.position = source.transform.position + Vector3.up * offset;
            Arrow.transform.LookAt(target.transform);
            if (Settings != null && Settings.Degrees != Vector2.zero)
                Arrow.transform.Rotate(Settings.Degrees.x, Settings.Degrees.y, 0f);
        }

        /// <summary>Destroy the arrow.</summary>
        public virtual void ClearArrow()
        {
            if (Arrow != null)
                Destroy(Arrow);
        }
    }
}
