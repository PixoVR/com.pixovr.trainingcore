using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Utility;

namespace PixoVR.TrainingCore
{
    /// <summary>Scene entry point: owns the step counter and coordinates startup sequencing.</summary>
    public class GameManager : SingletonBehaviour<GameManager>
    {
        /// <summary>Start-step index the flow will begin from (set before run).</summary>
        public int StartStep;

        /// <summary>Initialise the step counter for a new run.</summary>
        public void InitializeStepCounter(int startStep = 0)
        {
            StartStep = startStep;
            StepCounter.InitializeTo(startStep);
        }
    }

    /// <summary>Minimal UI coordinator: show/hide flow-driven UI roots.</summary>
    public class UserInterfaceManager : SingletonBehaviour<UserInterfaceManager>
    {
        /// <summary>Whether UI is currently visible.</summary>
        public bool IsVisible { get; private set; } = true;

        /// <summary>Show or hide all managed UI.</summary>
        public void SetVisible(bool visible) => IsVisible = visible;
    }
}
