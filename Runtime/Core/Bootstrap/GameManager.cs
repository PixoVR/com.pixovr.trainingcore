using System.Collections;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Multiuser;
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

        /// <summary>See the base contract.</summary>
        protected override void Awake()
        {
            base.Awake();
            if (NetworkManager.InstanceExists)
                NetworkManager.Instance.ResetRandoms();
            else if (RandomManager.InstanceExists)
                RandomManager.Instance.ResetRandoms();
        }

        private void Start() => Launch();

        /// <summary>Re-run the startup sequence.</summary>
        public void Relaunch() => Launch();

        private Coroutine startupCoroutine;

        private void Launch()
        {
            if (startupCoroutine != null)
                StopCoroutine(startupCoroutine);
            Started = false;
            startupCoroutine = StartCoroutine(StartupRoutine(GameModeManager.CurrentMode));
        }

        /// <summary>Initialise the step counter for a new run.</summary>
        public void InitializeStepCounter(int startStep = 0)
        {
            StartStep = startStep;
            StepCounter.InitializeTo(startStep);
        }

        /// <summary>Run the startup sequence: fade in → environment → flow init → start graph → sync wait → fade out.</summary>
        public IEnumerator StartupRoutine(GameMode mode)
        {
            if (FadeManager.InstanceExists)
            {
                FadeManager.Instance.FadeToBlack();
                yield return new WaitForSeconds(FadeDuration);
            }
            else
            {
                Log.Warning("Missing fade manager.", LogCategory.GameManagerLogic);
            }

            if (FlowManager == null)
                FlowManager = FindObjectOfType<GraphFlowManager>();
            if (FlowManager == null)
            {
                Log.Error("GameManager unable to find GraphFlowManager", LogCategory.GameManagerLogic);
                yield break;
            }
            if (FlowManager.NodeGraph == null)
            {
                Log.Error("Flow manager missing graph", LogCategory.GameManagerLogic);
                yield break;
            }

            if (EnvironmentLoader == null)
                EnvironmentLoader = FindObjectOfType<EnvironmentLoader>();
            if (EnvironmentLoader != null && EnvironmentLoader.EnvironmentObject == null &&
                EnvironmentLoader.CurrentEnvironment != null &&
                EnvironmentLoader.CurrentEnvironment.RuntimeKeyIsValid())
            {
                var loaded = false;
                System.Action onDone = () => loaded = true;
                EnvironmentLoader.LoadingDone += onDone;
                EnvironmentLoader.LoadEnvironment();
                yield return new WaitUntil(() => loaded);
                EnvironmentLoader.LoadingDone -= onDone;
            }

            SafeInvoke(OnInitalSetUp);

            CommandHistory.Instance.Reset();
            InitializeStepCounter(StartStep);
            FlowManager.Initialize(mode);
            SafeInvoke(OnGraphLoaded);
            FlowManager.StartGraph();
            SafeInvoke(OnGraphStarted);

            if (NetworkManager.InstanceExists && NetworkManager.Instance.InRoom && NetworkManager.Instance.IsSyncing)
            {
                AudioListener.pause = true;
                yield return new WaitWhile(() => NetworkManager.Instance.IsSyncing);
                AudioListener.pause = false;
            }

            if (FadeManager.InstanceExists)
                FadeManager.Instance.FadeToClear();
            Started = true;
        }

        private static void SafeInvoke(UnityEngine.Events.UnityEvent evt)
        {
            try
            {
                evt?.Invoke();
            }
            catch (System.Exception e)
            {
                Log.Error(e, LogCategory.GameManagerLogic);
            }
        }
    }

}
