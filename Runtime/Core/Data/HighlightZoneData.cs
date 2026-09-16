using System;
using PixoVR.TrainingCore.Identity;

namespace PixoVR.TrainingCore.Data
{
    /// <summary>One highlight-zone binding: a target plus the state to set.</summary>
    [Serializable]
    public class HighlightZoneData
    {
        /// <summary>Target object reference.</summary>
        public GuidReference Target;

        /// <summary>Highlight state to apply.</summary>
        public bool State = true;
    }
}
