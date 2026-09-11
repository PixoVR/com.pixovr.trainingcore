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

        /// <summary>Active flow.</summary>
        public FlowBase ActiveFlow => _activeFlow;

        /// <summary>The normal flow.</summary>
        public NormalFlow NormalFlow => _normalFlow;

        /// <summary>The parsed graph data.</summary>
        public GraphData GraphData => _graphData;

        /// <summary>Current steps on the active flow.</summary>
        public List<StepBase> CurrentSteps => ActiveFlow?.CurrentSteps;

        private FlowBase _activeFlow;
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
            ParseGraph(gameMode);
        }

        /// <summary>Begin executing the parsed graph.</summary>
        public virtual void StartGraph()
        {
            if (_normalFlow == null)
            {
                Log.Warning("GraphFlowManager.StartGraph called before Initialize", LogCategory.Flow);
                return;
            }
            _activeFlow = _normalFlow;
            _activeFlow.InitializeIterator();
            _activeFlow.FlowIterator.CurrentNodeChanged += OnIteratorChanged;
            _activeFlow.OnFlowCompleted += () => OnGraphCompleted?.Invoke();
            _activeFlow.Start();
            OnGraphStarted?.Invoke();
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
            var handler = _graphData?.DefaultFailureHandler
                          ?? _graphData?.FailureHandlerSteps.FirstOrDefault();
            if (handler == null)
            {
                Log.Warning($"OnFail with no fail handler: {reason}", LogCategory.Flow);
                return;
            }
            var flow = new FailHandlerFlow(handler, _normalFlow);
            flow.InitializeIterator();
            flow.OnFlowCompleted += ReturnToNormalFlow;
            _activeFlow = flow;
            flow.Start();
        }

        /// <summary>Return to the normal flow when a fail-handler completes.</summary>
        public virtual void ReturnToNormalFlow()
        {
            _activeFlow = _normalFlow;
            _activeFlow?.FlowIterator?.Restart();
        }

        /// <summary>Skip to a step by GUID.</summary>
        public virtual void SkipToStep(string guid)
        {
            if (ActiveFlow != null && ActiveFlow.SkipToStep(guid))
            {
                OnSkipMultipleStepsCompleted?.Invoke();
                OnCurrentStepsChanged?.Invoke(CurrentSteps);
            }
        }

        /// <summary>Skip forward one step.</summary>
        public virtual void SkipNext()
        {
            ActiveFlow?.SkipToNext();
            OnSkipForwardCompleted?.Invoke();
            OnCurrentStepsChanged?.Invoke(CurrentSteps);
        }

        /// <summary>Skip back one step.</summary>
        public virtual void SkipBack()
        {
            ActiveFlow?.SkipToBack();
            OnSkipBackwardsCompleted?.Invoke();
            OnCurrentStepsChanged?.Invoke(CurrentSteps);
        }

        private void OnIteratorChanged() => OnCurrentStepsChanged?.Invoke(CurrentSteps);

        private void OnEnable()
        {
            if (SkipForwards != null)
                SkipForwards.performed += _ => SkipNext();
            if (SkipBackwards != null)
                SkipBackwards.performed += _ => SkipBack();
        }
    }
}
