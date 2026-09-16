using System;
using System.Collections.Generic;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>A single runnable module inside a scenario (was Luminous.Samples.Module).</summary>
    [Serializable]
    public class Module
    {
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Portal description.</summary>
        public string Description;
        /// <summary>Unity scene to load for this module.</summary>
        public string SceneToLoad;
        /// <summary>Hosted thumbnail path (relative to the server base address).</summary>
        public string ImageAddress;
        /// <summary>Estimated run length (display string).</summary>
        public string EstimatedLength;
        /// <summary>Difficulty label.</summary>
        public string Difficulty;
    }

    /// <summary>A group of modules offered to a user (was Luminous.Samples.Scenario).</summary>
    [Serializable]
    public class Scenario
    {
        /// <summary>Display name.</summary>
        public string ScenarioName;
        /// <summary>Backend scenario id.</summary>
        public string ScenarioId;
        /// <summary>Modules contained in the scenario.</summary>
        public List<Module> Modules = new List<Module>();
    }

    /// <summary>A scheduled multi-user session (was Luminous.Samples.SessionDescription).</summary>
    [Serializable]
    public class SessionDescription
    {
        /// <summary>Session display name.</summary>
        public string SessionName;
        /// <summary>Room the session joins.</summary>
        public string RoomName;
        /// <summary>Scenario the session runs.</summary>
        public string ScenarioName;
        /// <summary>Module the session runs.</summary>
        public string ModuleName;
        /// <summary>Mode label ("Guided Performance", "Practice", "Assessment").</summary>
        public string Mode;
        /// <summary>Scheduled start time.</summary>
        public DateTime ScheduledTime;
        /// <summary>Current lifecycle status.</summary>
        public SessionStatus Status;
        /// <summary>Module names included in the session.</summary>
        public List<string> Modules = new List<string>();
        /// <summary>User ids of session leads/instructors.</summary>
        public List<string> LeadUserIds = new List<string>();
    }

    /// <summary>Portal catalog for the current user (was Luminous.Samples.UserScenarios).</summary>
    [Serializable]
    public class UserScenarios
    {
        /// <summary>Scenarios the user may run.</summary>
        public List<Scenario> AvailableScenarios = new List<Scenario>();
        /// <summary>Sessions scheduled for the user.</summary>
        public List<SessionDescription> ScheduledSessions = new List<SessionDescription>();
    }
}
