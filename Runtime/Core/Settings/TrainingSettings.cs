using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Settings
{
    /// <summary>Screen-fade timing configuration.</summary>
    [Serializable]
    public class FadeSettings
    {
        /// <summary>Seconds for a full fade.</summary>
        public float FadeTime = 0.5f;
    }

    /// <summary>Start-location placement configuration.</summary>
    [Serializable]
    public class PlacerSettings
    {
        /// <summary>Tag used to find the spawn point when none is assigned.</summary>
        public string SpawnPointTag = "Respawn";
    }

    /// <summary>Feature flags describing what a platform (PC, HMD model…) supports.</summary>
    [CreateAssetMenu(fileName = "PlatformFeatureSet", menuName = "TrainingCore/Platform Feature Set")]
    public class PlatformFeatureSet : ScriptableObject
    {
        /// <summary>Display name of this platform.</summary>
        public string PlatformName;

        /// <summary>Whether this platform supports multi-user.</summary>
        public bool MultiuserSupport = true;

        /// <summary>Whether this platform can host a session.</summary>
        public bool CanHost;
    }

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

        /// <summary>Start-location placement settings.</summary>
        public PlacerSettings PlacerSettings = new PlacerSettings();

        /// <summary>Feature set selected for this build.</summary>
        public PlatformFeatureSet PlatformFeatureSet;

        /// <summary>Reset the cached instance (used by tests/domain reload).</summary>
        public static void ResetInstance() => instance = null;
    }
}
