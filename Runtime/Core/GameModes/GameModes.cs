using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
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

    /// <summary>Per-node include flag for a game mode (serialized in graph node data).</summary>
    [Serializable]
    public class GameModeData
    {
        /// <summary>Configured mode.</summary>
        public GameMode Mode;

        /// <summary>Node included when this mode is active.</summary>
        public bool Include = true;

        /// <summary>Included entry for a mode.</summary>
        public GameModeData(GameMode mode) => Mode = mode;

        /// <summary>Entry for a mode with explicit include flag.</summary>
        public GameModeData(GameMode mode, bool included)
        {
            Mode = mode;
            Include = included;
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public GameModeData() { }
    }

    /// <summary>Character shorthand for a game mode.</summary>
    public static class GameModeEnumUtility
    {
        /// <summary>'T', 'P' or 'A'.</summary>
        public static char ToChar(this GameMode gameMode) => gameMode switch
        {
            GameMode.Training => 'T',
            GameMode.Practice => 'P',
            _ => 'A'
        };
    }

    /// <summary>Static event hub for the active game mode, step lifecycle and module transitions.</summary>
    public static class GameModeManager
    {
        /// <summary>Failure handler delegate.</summary>
        public delegate void Failure(List<StepBase> currentSteps, string failureReason, int handlerIndex = -1);
        /// <summary>Step event delegate.</summary>
        public delegate void StepEvent(string flowName, StepBase step);
        /// <summary>Steps event delegate.</summary>
        public delegate void StepsEvent(string flowName, List<StepBase> step);
        /// <summary>Mode-change delegate.</summary>
        public delegate void GameModeEvent(GameMode gameMode);
        /// <summary>Module-change delegate.</summary>
        public delegate void ModuleChange(string moduleName = "", Graph.TrainingGraph graph = null);

        private static GameMode currentMode = GameMode.Training;

        /// <summary>Active game mode.</summary>
        public static GameMode CurrentMode
        {
            get => currentMode;
            set
            {
                if (currentMode == value)
                    return;
                currentMode = value;
                GameModeChange?.Invoke(value);
            }
        }

        /// <summary>Fired when the flow fails.</summary>
        public static event Failure OnFail;

        /// <summary>Fired when a step completes.</summary>
        public static event StepEvent OnStepComplete;

        /// <summary>Fired when a set of steps starts.</summary>
        public static event StepsEvent OnStepsStarted;

        /// <summary>Fired when the game mode changes.</summary>
        public static event GameModeEvent GameModeChange;

        /// <summary>Fired when a module is passed.</summary>
        public static event ModuleChange OnModulePassed;

        /// <summary>Fired when a module starts.</summary>
        public static event ModuleChange OnModuleStart;

        /// <summary>Fired when a module ends.</summary>
        public static event ModuleChange OnModuleEnd;

        /// <summary>Report a failure raised by an interaction event; routed to the active fail handler.</summary>
        public static void Fail(InteractionEventArgs stepArgs)
        {
            var steps = Flow.GraphFlowManager.InstanceExists
                ? Flow.GraphFlowManager.Instance.CurrentSteps
                : new List<StepBase>();
            Fail(steps, stepArgs?.ToString(), -1);
        }

        /// <summary>Report a failure with explicit context.</summary>
        public static void Fail(List<StepBase> steps, string reason, int handlerIndex = -1)
        {
            Log.Warning($"Flow failed: {reason}", LogCategory.GameModeFailExceptions);
            OnFail?.Invoke(steps, reason, handlerIndex);
            if (Flow.GraphFlowManager.InstanceExists)
                Flow.GraphFlowManager.Instance.OnFail(steps, reason, handlerIndex);
        }

        /// <summary>Notify a completed step.</summary>
        public static void StepCompleted(string flowName, StepBase step) => OnStepComplete?.Invoke(flowName, step);

        /// <summary>Notify started steps.</summary>
        public static void StepsStarted(string flowName, List<StepBase> steps) => OnStepsStarted?.Invoke(flowName, steps);
    }
}
