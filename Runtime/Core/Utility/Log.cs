using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>Bit flags selecting which subsystems emit log output.</summary>
    [Flags]
    public enum LogCategory
    {
        /// <summary>No category.</summary>
        None = 0,
        /// <summary>Networking / multiuser.</summary>
        Multiuser = 1,
        /// <summary>Step/flow logic.</summary>
        StepLogic = 2,
        /// <summary>Fail handling.</summary>
        GameModeFailExceptions = 4,
        /// <summary>General manager logic.</summary>
        GameManagerLogic = 8,
        /// <summary>Hardware/platform layer.</summary>
        Hardware = 0x10,
        /// <summary>Scenario-specific logging.</summary>
        ScenarioSpecific = 0x20,
        /// <summary>Interaction events.</summary>
        Interaction = 0x100,
        /// <summary>Flow execution.</summary>
        Flow = 0x200,
        /// <summary>Platform/backend session.</summary>
        Platform = 0x400,
        /// <summary>Scene loading/management.</summary>
        Scene = 0x800,
        /// <summary>Uncategorised.</summary>
        Other = 0x40,
        /// <summary>Replay/undo.</summary>
        Replay = 0x80,
        /// <summary>Everything.</summary>
        All = ~0
    }

    /// <summary>Static logger honouring the runtime mask on <see cref="Settings.TrainingConfig.LogMask"/>.</summary>
    public static class Log
    {
        /// <summary>Fired for every message that passes the mask.</summary>
        public static Action<object, LogCategory> OnMessageSent;

        /// <summary>Runtime category mask; <see cref="LogCategory.All"/> when no config overrides it.</summary>
        public static LogCategory Mask
        {
            get
            {
                var cfg = Settings.TrainingConfig.Instance;
                return cfg != null ? cfg.LogMask : LogCategory.All;
            }
        }

        /// <summary>Is a category enabled?</summary>
        public static bool IsEnabled(LogCategory category) => (Mask & category) != 0;

        /// <summary>Log an informational message.</summary>
        public static void Info(object message, LogCategory category = LogCategory.Other)
        {
            if (!IsEnabled(category)) return;
            Debug.Log(message);
            OnMessageSent?.Invoke(message, category);
        }

        /// <summary>Log an informational message (alias of <see cref="Info"/> kept for call-site parity).</summary>
        public static void Message(object message, LogCategory category = LogCategory.Other) => Info(message, category);

        /// <summary>Log a warning.</summary>
        public static void Warning(object message, LogCategory category = LogCategory.Other)
        {
            if (!IsEnabled(category)) return;
            Debug.LogWarning(message);
            OnMessageSent?.Invoke(message, category);
        }

        /// <summary>Log an error (always emitted regardless of the mask).</summary>
        public static void Error(object message, LogCategory category = LogCategory.Other)
        {
            Debug.LogError(message);
            OnMessageSent?.Invoke(message, category);
        }
    }
}
