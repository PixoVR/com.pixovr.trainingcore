using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Utility;

namespace PixoVR.TrainingCore.GameModes
{
    /// <summary>Training flow execution mode.</summary>
    public enum GameMode
    {
        /// <summary>Guided training.</summary>
        Training = 0,
        /// <summary>Unguided practice.</summary>
        Practice = 1,
        /// <summary>Recorded assessment.</summary>
        Assessment = 2
    }

    /// <summary>Static data attached to a game-mode node.</summary>
    [Serializable]
    public class GameModeData
    {
        /// <summary>Configured mode.</summary>
        public GameMode Mode;

        /// <summary>Fail limit before the flow is marked failed (0 = unlimited).</summary>
        public int FailLimit;

        /// <summary>Log categories that record a fail event.</summary>
        public Utility.LogCategory LogFailCategories = Utility.LogCategory.None;

        /// <summary>Score value associated with this node.</summary>
        public int Value;
    }

    /// <summary>Active <see cref="GameMode"/> and fail-event dispatch.</summary>
    public class GameModeManager : SingletonBehaviour<GameModeManager>
    {
        /// <summary>Currently active mode.</summary>
        public GameMode CurrentMode { get; private set; } = GameMode.Training;

        /// <summary>Currently active mode data.</summary>
        public GameModeData CurrentGameModeData { get; private set; }

        /// <summary>Fired when the flow reports a failure. Args: failed steps, reason, handler index.</summary>
        public static Action<List<Step>, string, int> OnFail;

        /// <summary>Set the active mode.</summary>
        public void SetMode(GameMode mode, GameModeData data = null)
        {
            CurrentMode = mode;
            CurrentGameModeData = data;
        }

        /// <summary>Report a failure in the current flow.</summary>
        public void Fail(List<Step> steps, string reason, int handlerIndex = 0)
        {
            Log.Warning($"Flow failed: {reason}", LogCategory.GameModeFailExceptions);
            OnFail?.Invoke(steps, reason, handlerIndex);
        }
    }
}
