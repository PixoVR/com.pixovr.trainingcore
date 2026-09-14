using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Settings
{
    /// <summary>Screen-fade configuration (serialized field names match the legacy plugin).</summary>
    [Serializable]
    public class FadeSettings
    {
        /// <summary>Use a custom fade colour instead of black.</summary>
        public bool UseCustomColor = false;

        /// <summary>Fade colour.</summary>
        public Color FadeColor;

        /// <summary>Seconds for a full fade.</summary>
        public float FadeDuration = 0.5f;

        /// <summary>Target opacity.</summary>
        public float TargetAlpha = 1f;

        /// <summary>Use unscaled time.</summary>
        public bool UseUnscaledTime;

        /// <summary>Defaults.</summary>
        public FadeSettings() { }

        /// <summary>Parameterised ctor.</summary>
        public FadeSettings(bool useCustomColor, Color fadeColor, float fadeDuration, float targetAlpha, bool useUnscaledTime)
        {
            UseCustomColor = useCustomColor;
            FadeColor = fadeColor;
            FadeDuration = fadeDuration;
            TargetAlpha = targetAlpha;
            UseUnscaledTime = useUnscaledTime;
        }

        /// <summary>Copy ctor.</summary>
        public FadeSettings(FadeSettings copy)
        {
            UseCustomColor = copy.UseCustomColor;
            FadeColor = copy.FadeColor;
            FadeDuration = copy.FadeDuration;
            TargetAlpha = copy.TargetAlpha;
            UseUnscaledTime = copy.UseUnscaledTime;
        }
    }

    /// <summary>How a placed display positions itself.</summary>
    public enum PlacementMode
    {
        /// <summary>Fixed transform.</summary>
        Fixed,
        /// <summary>Follow with deadzone.</summary>
        Deadzone,
        /// <summary>Actively re-place.</summary>
        Active,
        /// <summary>At a given scene location.</summary>
        SetLocation
    }

    /// <summary>Display placement configuration (field names match the legacy plugin).</summary>
    [Serializable]
    public class PlacerSettings
    {
        /// <summary>Placer object reference.</summary>
        [SerializeField]
        [HideInInspector]
        private GuidReference objectPlacerReference;

        /// <summary>Explicit set-location reference.</summary>
        [SerializeField]
        private GuidReference setLocation;

        /// <summary>Placement mode.</summary>
        public PlacementMode Mode = PlacementMode.Deadzone;

        /// <summary>Keep a fixed height.</summary>
        public bool FixedHeight = true;

        /// <summary>Anchor to origin height.</summary>
        public bool OriginHeight = true;

        /// <summary>Preferred distance from the camera origin.</summary>
        public float TargetOriginDistance = 1.5f;

        /// <summary>Minimum distance from the camera.</summary>
        public float MinimumDistanceFromCamera = 1f;

        /// <summary>Height of the display.</summary>
        public float DisplayHeight = 1.8f;

        /// <summary>Lerp speed for movement.</summary>
        public float LerpSpeed = 0.05f;

        /// <summary>Layer mask blocking placement.</summary>
        public LayerMask PlacementBlockingLayer = 512;

        /// <summary>Distance under which a placement snaps.</summary>
        public float SnapDistance = 0.1f;

        /// <summary>Collision padding.</summary>
        public float Padding = 0.15f;

        /// <summary>Size affects rotation fit.</summary>
        public bool SizeForRotation = true;

        /// <summary>Directions probed when finding a placement.</summary>
        public int DirectionAttempts = 60;

        /// <summary>Advanced-foldout editor state.</summary>
        public bool AdvanceFoldoutState = false;

        /// <summary>Show off-screen arrows.</summary>
        public bool OffScreenArrows = true;

        /// <summary>Position deadzone radius.</summary>
        public float PositionDeadzoneSize = 2.5f;

        /// <summary>Rotation deadzone (degrees).</summary>
        public float RotationDeadzoneSize = 2f;

        /// <summary>Resolved placer (rebound from <see cref="objectPlacerReference"/>).</summary>
        public Utility.Display.DisplayObjectPlacer ObjectPlacer
        {
            get => objectPlacerReference?.GameObject?.GetComponent<Utility.Display.DisplayObjectPlacer>();
            set => objectPlacerReference = value == null ? null : new GuidReference(value.gameObject);
        }

        /// <summary>Resolved set-location object.</summary>
        public GameObject SetLocation
        {
            get => setLocation?.GameObject;
            set => setLocation = value == null ? null : new GuidReference(value);
        }
    }

    /// <summary>And-group settings: how many grouped steps must complete.</summary>
    [Serializable]
    public class AndGroupSettings
    {
        /// <summary>Require every grouped step.</summary>
        public bool NeedToCompleteAll;

        /// <summary>Number of steps required when not completing all.</summary>
        public int NumberOfStepsToComplete;
    }

    /// <summary>Arrow placement settings.</summary>
    [Serializable]
    public class ArrowPlacerSettings
    {
        /// <summary>Arrow prefab.</summary>
        public GameObject ArrowPrefab;

        /// <summary>Sweep angles in degrees.</summary>
        public Vector2 Degrees;

        /// <summary>Offset from the target.</summary>
        public float Offset;

        /// <summary>Parent the arrow to the target.</summary>
        public bool SpawnAsChild = true;
    }

    /// <summary>A list of audio clips and playback parameters.</summary>
    [Serializable]
    public class AudioClipSettings
    {
        /// <summary>Clips to play in sequence.</summary>
        public List<AudioClip> ClipsToPlay = new List<AudioClip>();

        /// <summary>Playback volume.</summary>
        public float Volume = 1f;

        /// <summary>2D–3D spatial blend.</summary>
        public float SpatialBlend = 0f;

        /// <summary>Play at a fixed world position.</summary>
        public bool PlayFromSpecificLocation = false;

        /// <summary>World position when <see cref="PlayFromSpecificLocation"/>.</summary>
        public Vector3 LocationToPlayFrom;
    }

    /// <summary>Interaction duration requirement.</summary>
    [Serializable]
    public class DurationSettings
    {
        /// <summary>A minimum duration is enforced.</summary>
        public bool IsRequired = false;

        /// <summary>Require continuous contact.</summary>
        public bool IsContinuous = true;

        /// <summary>Required seconds.</summary>
        public float DurationNeeded = 3f;
    }

    /// <summary>Skip-the-step when the action completes.</summary>
    [Serializable]
    public class SkipOnCompleteSettings
    {
        /// <summary>Skip on complete.</summary>
        public bool SkipOnComplete = true;
    }

    /// <summary>A fail exception plus its per-step include flag.</summary>
    [Serializable]
    public class ExceptionSettings
    {
        /// <summary>The exception definition.</summary>
        [SerializeReference]
        public Flow.Exceptions.FailExceptionBase Exception;

        /// <summary>Include the exception's parameters in matching.</summary>
        public bool IncludeParams = true;

        /// <summary>Wrap an exception.</summary>
        public ExceptionSettings(Flow.Exceptions.FailExceptionBase exception, bool includeParams = true)
        {
            Exception = exception;
            IncludeParams = includeParams;
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public ExceptionSettings() { }
    }

    /// <summary>Failable flag + attached exceptions for a step or global-exceptions node.</summary>
    [Serializable]
    public class FailData
    {
        /// <summary>Node can fail.</summary>
        public bool Failable = true;

        /// <summary>Attached exceptions.</summary>
        [SerializeReference]
        public List<ExceptionSettings> Exceptions = new List<ExceptionSettings>();
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

        /// <summary>Default display placement settings.</summary>
        public PlacerSettings PlacerSettings = new PlacerSettings();

        /// <summary>Feature set selected for this build.</summary>
        public PlatformFeatureSet PlatformFeatureSet;

        /// <summary>Reset the cached instance (used by tests/domain reload).</summary>
        public static void ResetInstance() => instance = null;
    }
}
