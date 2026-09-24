using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>
    /// Owns the runtime graph: parses the assigned <see cref="TrainingGraph"/>, runs the
    /// normal flow, swaps in fail-handler flows, and handles skip hotkeys.
    /// </summary>
    public class GraphFlowManager : SingletonBehaviour<GraphFlowManager>
    {
        /// <summary>The graph asset to run.</summary>
        public TrainingGraph NodeGraph;

        /// <summary>Persisted flow state.</summary>
        public FlowSavedData FlowData;

        /// <summary>Iterator driving the active flow.</summary>
        public GraphIterator CurrentIterator => ActiveFlow?.FlowIterator;

        /// <summary>Whether module fail handlers are active.</summary>
        public bool CanFail = true;

        /// <summary>Skip hotkeys broadcast over the network.</summary>
        public bool SkippingHotkeyUsesNetwork = false;

        /// <summary>Skip-forwards hotkey.</summary>
        public InputAction SkipForwards;

        /// <summary>Skip-backwards hotkey.</summary>
        public InputAction SkipBackwards;

        /// <summary>Fired when parsing finished and the graph starts.</summary>
        public event Action OnGraphStarted;

        /// <summary>Static counterpart of <see cref="OnGraphStarted"/> usable before an instance exists.</summary>
        public static event Action GraphStarted;

        /// <summary>Fired when the normal flow completes.</summary>
        public event Action OnGraphCompleted;

        /// <summary>Fired when the current step set changes.</summary>
        public event Action<List<StepBase>> OnCurrentStepsChanged;

        /// <summary>Fired after a forward skip.</summary>
        public event Action OnSkipForwardCompleted;

        /// <summary>Fired after a multi-step skip.</summary>
        public event Action OnSkipMultipleStepsCompleted;

        /// <summary>Fired after a backward skip.</summary>
        public event Action OnSkipBackwardsCompleted;

        /// <summary>Fired every Update; steps needing polling (e.g. MoveToPositionStep) subscribe.</summary>
        public event Action Tick;

        /// <summary>True between graph start and <see cref="StopFlow"/>.</summary>
        public bool GraphRunning { get; private set; }

        /// <summary>Active flow.</summary>
        public FlowBase ActiveFlow => _activeFlow;

        /// <summary>The normal flow.</summary>
        public NormalFlow NormalFlow => _normalFlow;

        /// <summary>The parsed graph data.</summary>
        public GraphData GraphData => _graphData;

        /// <summary>Current steps on the active flow.</summary>
        public List<StepBase> CurrentSteps => ActiveFlow?.CurrentSteps;

        private FlowBase _activeFlow;
        private bool _moduleEnded;
        private NormalFlow _normalFlow;
        private GraphData _graphData;
        private GraphParser _parser;

        /// <summary>Parse the assigned graph and build the flows.</summary>
        public virtual void Initialize(GameMode gameMode)
        {
            if (NodeGraph == null)
            {
                Log.Warning("GraphFlowManager has no NodeGraph assigned", LogCategory.Flow);
                return;
            }
            RefreshGuids();
            ParseGraph(gameMode);
        }

        private static void RefreshGuids()
        {
            foreach (var guid in FindObjectsOfType<Identity.GuidComponent>(true))
                guid.CreateGuid();
        }

        /// <summary>Begin executing the parsed graph.</summary>
        public virtual void StartGraph()
        {
            if (_normalFlow == null)
            {
                Log.Warning("GraphFlowManager.StartGraph called before Initialize", LogCategory.Flow);
                return;
            }
            _moduleEnded = false;
            _activeFlow = _normalFlow;
            _activeFlow.InitializeIterator();
            _activeFlow.FlowIterator.CurrentNodeChanged += OnIteratorChanged;
            _activeFlow.OnFlowCompleted += OnFlowCompleted;
            GameModeManager.ModuleStarted(NodeGraph.name, NodeGraph);
            if (CanFail)
                FailureDetectionManager.Instance.StartDetecting();
            _activeFlow.Start();
            OnGraphStarted?.Invoke();
            GraphStarted?.Invoke();
            GraphRunning = true;
        }

        /// <summary>Re-parse with a parser.</summary>
        public virtual void ParseGraph(GraphParser parser)
        {
            _parser = parser;
            _graphData = parser.Parse(NodeGraph);
            _normalFlow = new NormalFlow(_graphData);
        }

        /// <summary>Parse with a fresh parser.</summary>
        public virtual void ParseGraph(GameMode gameMode) => ParseGraph(new GraphParser(gameMode));

        /// <summary>Handle a failure: switch to a fail-handler flow.</summary>
        public virtual void OnFail(IEnumerable<StepBase> steps, string reason, int failIndex = -1)
        {
            if (!CanFail)
            {
                Log.Info($"Fail ignored (CanFail=false): {reason}", LogCategory.Flow);
                return;
            }
            if (_graphData == null)
                return;
            FailHandlerStep handler = null;
            var handlerSteps = _graphData.FailureHandlerSteps;
            if (failIndex >= 0 && failIndex < handlerSteps.Count)
            {
                handler = handlerSteps[failIndex];
            }
            else
            {
                var failing = steps?.OfType<StepExecutionBase>().FirstOrDefault();
                if (failing != null && !failing.UseDefaultFailhandler
                    && !string.IsNullOrEmpty(failing.FailHandlerGuid))
                    handler = handlerSteps.FirstOrDefault(h => h != null && h.GUID == failing.FailHandlerGuid);
            }
            handler = handler ?? _graphData.DefaultFailureHandler ?? handlerSteps.FirstOrDefault();
            if (handler == null)
            {
                Log.Warning($"OnFail with no fail handler: {reason}", LogCategory.Flow);
                return;
            }
            if (_normalFlow?.CurrentSteps != null)
                foreach (var s in _normalFlow.CurrentSteps)
                {
                    s?.OnFail();
                    s?.OnExit();
                }
            handler.FailReason = reason;
            var flow = new FailHandlerFlow(handler, _normalFlow);
            flow.InitializeIterator();
            flow.OnFlowCompleted += ReturnToNormalFlow;
            _activeFlow = flow;
            flow.Start();
        }

        /// <summary>Called when the active flow completes.</summary>
        public virtual void OnFlowCompleted()
        {
            if (_activeFlow is NormalFlow)
            {
                Commands.CommandHistory.Instance.Reset();
                GameModeManager.ModuleCompleted(NodeGraph != null ? NodeGraph.name : string.Empty, NodeGraph);
                Events.EventBus.Instance.Reset();
                EndModule();
                OnGraphCompleted?.Invoke();
            }
            else
            {
                ReturnToNormalFlow();
            }
        }

        /// <summary>Return to the normal flow when a fail-handler completes.</summary>
        public virtual void ReturnToNormalFlow()
        {
            _activeFlow = _normalFlow;
            var iterator = _activeFlow?.FlowIterator;
            if (iterator?.CurrentSteps != null)
                iterator.SetCurrentSteps(new List<StepBase>(iterator.CurrentSteps));
        }

        /// <summary>Skip to a step by GUID.</summary>
        /// <summary>Skip to a step by index.</summary>
        public virtual void SkipToStep(int targetStepIndex) => SkipToStep(targetStepIndex, true);

        /// <summary>Skip to a step by index, optionally syncing over the network.</summary>
        public virtual void SkipToStep(int targetStepIndex, bool invokeToNetwork)
        {
            var current = CurrentSteps?.FirstOrDefault();
            if (current == null)
                return;
            int here = current.GetMainStepNumber();
            if (targetStepIndex > here)
                ActiveFlow?.ForwardSkip.SkipUntil(targetStepIndex);
            else if (targetStepIndex < here)
                ActiveFlow?.BackwardSkip.SkipUntil(targetStepIndex);
            else
                return;
            OnSkipMultipleStepsCompleted?.Invoke();
            OnCurrentStepsChanged?.Invoke(CurrentSteps);
        }

        public virtual void SkipToStep(string guid)
        {
            if (ActiveFlow != null && ActiveFlow.SkipToStep(guid))
            {
                OnSkipMultipleStepsCompleted?.Invoke();
                OnCurrentStepsChanged?.Invoke(CurrentSteps);
            }
        }

        /// <summary>Skip to the next step (Luminous name: SkipToNext).</summary>
        public virtual void SkipToNext()
        {
            SkipNext();
        }

        /// <summary>Skip to the next step; <paramref name="sync"/> reserved for network sync.</summary>
        public virtual void SkipToNext(bool sync) => SkipNext();

        /// <summary>Skip to the previous step; <paramref name="sync"/> reserved for network sync.</summary>
        public virtual void SkipToBack(bool sync) => SkipBack();

        /// <summary>Skip to the next step.</summary>
        public virtual void SkipNext()
        {
            ActiveFlow?.SkipToNext();
            OnSkipForwardCompleted?.Invoke();
            OnCurrentStepsChanged?.Invoke(CurrentSteps);
        }

        /// <summary>Skip to the previous step (Luminous name: SkipToBack).</summary>
        public virtual void SkipToBack()
        {
            SkipBack();
        }

        /// <summary>Skip to the previous step.</summary>
        public virtual void SkipBack()
        {
            ActiveFlow?.SkipToBack();
            OnSkipBackwardsCompleted?.Invoke();
            OnCurrentStepsChanged?.Invoke(CurrentSteps);
        }

        private void OnIteratorChanged() => OnCurrentStepsChanged?.Invoke(CurrentSteps);

        private void Update() => Tick?.Invoke();

        /// <summary>Stop the active flow and report module end (Luminous parity).</summary>
        public virtual void StopFlow()
        {
            _activeFlow?.Cancel();
            if (!ReferenceEquals(_normalFlow, _activeFlow))
                _normalFlow?.Cancel();
            _activeFlow = null;
            GraphRunning = false;
            FailureDetectionManager.Instance.StopDetecting();
            EndModule();
        }

        private void EndModule()
        {
            if (_moduleEnded)
                return;
            _moduleEnded = true;
            GameModeManager.ModuleEnded(NodeGraph != null ? NodeGraph.name : string.Empty, NodeGraph);
        }

        private void OnDisable() => StopFlow();

        private void OnEnable()
        {
            if (SkipForwards != null)
                SkipForwards.performed += _ => SkipNext();
            if (SkipBackwards != null)
                SkipBackwards.performed += _ => SkipBack();
        }
    }
}
