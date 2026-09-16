using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Settings
{
    /// <summary>
    /// Runtime configuration asset. Loaded once from <c>Resources/TrainingConfig</c>;
    /// holds the tap threshold, the <see cref="Utility.LogCategory"/> mask and feature sets.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingConfig", menuName = "TrainingCore/Training Config")]
    public class TrainingConfig : ScriptableObject
    {
        private static TrainingConfig instance;

        /// <summary>The loaded config, or a default instance when no asset exists in Resources.</summary>
        public static TrainingConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<TrainingConfig>("TrainingConfig");
                    if (instance == null)
                        instance = CreateInstance<TrainingConfig>();
                }
                return instance;
            }
        }

        /// <summary>Seconds a tap must be held before it counts.</summary>
        [Range(0f, 2f)]
        public float MinTapDuration = 0.2f;

        /// <summary>Enabled <see cref="Utility.LogCategory"/> mask.</summary>
        public Utility.LogCategory LogMask = Utility.LogCategory.All;

        /// <summary>Fade overlay settings.</summary>
        public FadeSettings FadeSettings = new FadeSettings();

        /// <summary>Default display placement settings.</summary>
        public PlacerSettings PlacerSettings = new PlacerSettings();

        /// <summary>Feature set selected for this build.</summary>
        public PlatformFeatureSet PlatformFeatureSet;

        /// <summary>Reset the cached instance (used by tests/domain reload).</summary>
        public static void ResetInstance() => instance = null;
    }
}
