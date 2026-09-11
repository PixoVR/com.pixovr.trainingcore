using System;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>
    /// A step of a training flow. Wave 1 stub: only the surface the platform/multiuser layers need.
    /// wave 2: graph parsing, inputs/outputs, undo actions, lifecycle events.
    /// </summary>
    [Serializable]
    public class Step
    {
        /// <summary>Node guid this step was parsed from.</summary>
        public string GUID;

        /// <summary>Display name.</summary>
        public string Name = string.Empty;

        /// <summary>Dotted step number ("3", "3.1").</summary>
        public string StepNumber = string.Empty;

        /// <summary>Main (integer) part of <see cref="StepNumber"/>.</summary>
        public int GetMainStepNumber()
        {
            int dot = StepNumber.IndexOf('.');
            return dot > 0 ? int.Parse(StepNumber.Substring(0, dot)) : int.Parse(StepNumber);
        }
    }
}
