using System.Collections;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.SceneManagement;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore
{
    /// <summary>Scene entry point: loads the environment, initialises the flow, coordinates startup.</summary>
    public class GameManager : SingletonBehaviour<GameManager>
    {
        /// <summary>The graph runner.</summary>
        [Tooltip("Graph flow manager driving the module.")]
        public GraphFlowManager FlowManager;

        /// <summary>Environment loader.</summary>
        public EnvironmentLoader EnvironmentLoader;

        /// <summary>Startup fade duration.</summary>
        public float FadeDuration = 2f;

        /// <summary>Start-step index the flow will begin from (set before run).</summary>
        public int StartStep;

        /// <summary>Invoked once initial setup (environment + fade) has finished.</summary>
        public UnityEngine.Events.UnityEvent OnInitalSetUp;

        /// <summary>Invoked when the flow graph is initialised.</summary>
        public UnityEngine.Events.UnityEvent OnGraphLoaded;

        /// <summary>Invoked when the flow graph starts.</summary>
        public UnityEngine.Events.UnityEvent OnGraphStarted;

        /// <summary>Whether the module has finished starting.</summary>
        public bool Started { get; private set; }

        /// <summary>Initialise the step counter for a new run.</summary>
        public void InitializeStepCounter(int startStep = 0)
        {
            StartStep = startStep;
            StepCounter.InitializeTo(startStep);
        }

        /// <summary>Run the startup sequence: environment → flow init → start graph.</summary>
        public IEnumerator StartupRoutine(GameMode mode)
        {
            if (EnvironmentLoader != null)
                EnvironmentLoader.LoadEnvironment();
            if (FadeManager.InstanceExists)
            {
                FadeManager.Instance.FadeToBlack();
                yield return new WaitForSeconds(FadeDuration);
                FadeManager.Instance.FadeToClear();
            }
            InitializeStepCounter(StartStep);
            OnInitalSetUp?.Invoke();
            if (FlowManager != null)
            {
                FlowManager.Initialize(mode);
                OnGraphLoaded?.Invoke();
                FlowManager.StartGraph();
                OnGraphStarted?.Invoke();
            }
            Started = true;
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
