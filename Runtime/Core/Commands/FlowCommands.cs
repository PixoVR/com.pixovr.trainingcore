using System;
using PixoVR.TrainingCore.Flow;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Skips the flow to the next step; unexecute is a no-op.</summary>
    [Serializable]
    public sealed class SkipToNextCommand : CommandBase
    {
        public SkipToNextCommand() : base(string.Empty) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (GraphFlowManager.InstanceExists)
                GraphFlowManager.Instance.SkipToNext();
        }

        /// <inheritdoc/>
        public override void Unexecute() { }
    }

    /// <summary>Skips the flow back to the previous step; unexecute is a no-op.</summary>
    [Serializable]
    public sealed class SkipToBackCommand : CommandBase
    {
        public SkipToBackCommand() : base(string.Empty) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (GraphFlowManager.InstanceExists)
                GraphFlowManager.Instance.SkipToBack();
        }

        /// <inheritdoc/>
        public override void Unexecute() { }
    }
}
