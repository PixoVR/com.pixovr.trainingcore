using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using GraphProcessor;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>Turns a <see cref="TrainingGraph"/> asset into a runtime <see cref="GraphData"/>.</summary>
    public class GraphParser
    {
        /// <summary>Steps created during the last parse.</summary>
        public Dictionary<string, StepBase> Steps { get; } = new Dictionary<string, StepBase>();

        /// <summary>Actions created during the last parse.</summary>
        public Dictionary<string, ActionBase> Actions { get; } = new Dictionary<string, ActionBase>();

        /// <summary>Fail-handler steps created during the last parse.</summary>
        public Dictionary<FailHandlerNode, FailHandlerStep> FailureSteps { get; } =
            new Dictionary<FailHandlerNode, FailHandlerStep>();

        /// <summary>Conditional resolver populated during parse.</summary>
        public ConditionalStepResolver Resolver { get; } = new ConditionalStepResolver();

        /// <summary>The game mode this parse targets.</summary>
        public GameMode GameMode { get; }

        private TrainingGraph graph;
        private GraphData graphData;

        /// <summary>Parse for the current mode.</summary>
        public GraphParser() : this(GameModeManager.CurrentMode) { }

        /// <summary>Parse for a given mode.</summary>
        public GraphParser(GameMode gameMode) => GameMode = gameMode;

        /// <summary>Parse the graph into runtime steps/actions.</summary>
        public virtual GraphData Parse(TrainingGraph graph)
        {
            if (graph == null)
                return null;
            this.graph = graph;
            Steps.Clear();
            Actions.Clear();
            FailureSteps.Clear();

            RebindReferences();

            var stepNodes = graph.nodes.OfType<StepBaseNode>()
                .Where(n => n.IsIncludedInMode(GameMode))
                .Where(n => !(n is FailHandlerNode)) // handlers wired separately
                .ToList();

            // create runtime twins
            foreach (var node in stepNodes)
                Steps[node.GUID] = node.Create();
            foreach (var node in graph.nodes.OfType<ActionNode>().Where(n => n.IsIncludedInMode(GameMode)))
                Actions[node.GUID] = node.Create();

            // flow wiring
            var start = graph.StartNode;
            foreach (var node in stepNodes)
            {
                var step = Steps[node.GUID];
                if (step is CorrectIncorrectStepBase)
                {
                    WireFlowOutputs(node, step);
                    continue;
                }
                foreach (var outNode in node.GetStepOutputs(GameMode))
                {
                    var target = outNode ?? start;
                    if (target == null || !Steps.TryGetValue(target.GUID, out var targetStep))
                        continue;
                    step.AddOuput(targetStep);
                    targetStep.AddInput(step);
                }
            }

            // action wiring on execution nodes
            foreach (var node in stepNodes.OfType<StepExecutionNode>())
            {
                var step = Steps[node.GUID] as StepExecutionBase;
                if (step == null)
                    continue;
                if (node.ExceptionsData?.Exceptions != null)
                    step.FailExceptions = node.ExceptionsData.Exceptions
                        .Select(e => e.Exception).Where(e => e != null).ToList();
                step.UseDefaultFailhandler = node.UseDefaultFailHandler;
                step.FailHandlerGuid = node.SelectedFailHandlerGuid;
                step.Description = node.Description;
                step.PlaySoundOnComplete = node.PlaySoundOnComplete;

                foreach (var a in node.GetOnStartActionNodes(GameMode))
                    if (Actions.TryGetValue(a.GUID, out var action))
                        step.StartActions.Add(action);
                foreach (var a in node.GetOnFinishActionNodes(GameMode))
                    if (Actions.TryGetValue(a.GUID, out var action))
                        step.CompleteActions.Add(action);
            }

            // global exceptions merge into each failable step
            var globalExceptions = graph.GlobalExceptions?.Data?.Exceptions?
                .Select(e => e.Exception).Where(e => e != null).ToList();
            if (globalExceptions != null)
                foreach (var step in Steps.Values.OfType<StepExecutionBase>())
                    step.FailExceptions.AddRange(globalExceptions);

            graphData = new GraphData { mainFlowRoot = Steps.TryGetValue(start?.GUID ?? "", out var root) ? root : null };

            // fail handlers
            graphData.SetDefaultFailHandlerGuid(graph.DefaultFailHandler?.GUID ?? "");
            foreach (var handlerNode in graph.FailHandlers.Where(n => n.IsIncludedInMode(GameMode)))
            {
                var handlerStep = handlerNode.Create() as FailHandlerStep;
                if (handlerStep == null)
                    continue;
                Steps[handlerNode.GUID] = handlerStep;
                FailureSteps[handlerNode] = handlerStep;
                graphData.FailureHandlerSteps.Add(handlerStep);
                WireFlowOutputs(handlerNode, handlerStep);
            }

            // group nodes: attach grouped steps to their group step
            foreach (var groupNode in stepNodes.OfType<AndGroupStepNode>())
            {
                if (Steps[groupNode.GUID] is AndGroupStep groupStep)
                    groupStep.GroupedSteps = groupNode.GetStepOutputs(GameMode)
                        .Select(n => Steps.TryGetValue(n?.GUID ?? "", out var s) ? s : null)
                        .Where(s => s != null).ToList();
            }

            return graphData;
        }

        /// <summary>Push per-node scene reference blobs (<c>savedRefs</c>) back onto their nodes.</summary>
        public virtual void RebindReferences()
        {
            var refs = graph.SavedRefs;
            foreach (var reference in refs)
            {
                if (graph.nodesPerGUID.TryGetValue(reference.NodeID, out var node) &&
                    node is TrainingBaseNode trainingNode)
                {
                    trainingNode.Data = reference.Data ?? new NodeData();
                    GraphReferencesManager.Instance?.AddToRebindQueue(reference);
                }
            }
        }

        private void WireFlowOutputs(StepBaseNode node, StepBase step)
        {
            if (step is CorrectIncorrectStepBase ci && node is CorrectIncorrectStepBaseNode ciNode)
            {
                WireList(ciNode.GetCorrectOutputs(), ci.CorrectStepOutputs);
                WireList(ciNode.GetIncorrectOutputs(), ci.IncorrectStepOutputs);
                foreach (var target in ci.CorrectStepOutputs.Concat(ci.IncorrectStepOutputs))
                {
                    step.AddOuput(target);
                    target.AddInput(step);
                }
                return;
            }
            foreach (var outNode in node.GetStepOutputs(GameMode) ?? Enumerable.Empty<StepBaseNode>())
            {
                if (outNode != null && Steps.TryGetValue(outNode.GUID, out var targetStep))
                {
                    step.AddOuput(targetStep);
                    targetStep.AddInput(step);
                }
            }

            void WireList(IEnumerable<StepBaseNode> nodes, List<StepBase> target)
            {
                foreach (var n in nodes ?? Enumerable.Empty<StepBaseNode>())
                    if (n != null && Steps.TryGetValue(n.GUID, out var s))
                        target.Add(s);
            }
        }
    }

    /// <summary>Rebinds saved scene references (GuidReferences) after scene load.</summary>
    public class GraphReferencesManager : SingletonBehaviour<GraphReferencesManager>
    {
        /// <summary>References pending resolution.</summary>
        public List<Reference> RebindQueue = new List<Reference>();

        /// <summary>Queue a reference blob for rebinding.</summary>
        public void AddToRebindQueue(Reference reference)
        {
            if (reference != null && !RebindQueue.Contains(reference))
                RebindQueue.Add(reference);
        }

        /// <summary>Drop all pending references.</summary>
        public void ClearRebindQueue() => RebindQueue.Clear();

        /// <summary>Resolve all queued references (forces the lazy GuidReference cache to refresh).</summary>
        public void UpdateReferences()
        {
            foreach (var reference in RebindQueue)
            {
                foreach (var prop in reference.Data?.SavedProperties ?? Enumerable.Empty<NodeSavedProperty>())
                {
                    _ = prop.ObjectReference?.GameObject;
                    if (prop.ObjectReferences != null)
                        foreach (var r in prop.ObjectReferences)
                            _ = r.GameObject;
                }
            }
        }
    }

    /// <summary>Assigns dotted step numbers (1, 1.1, ...) across the graph.</summary>
    public class StepNumberUpdater
    {
        private int highestNumber;

        /// <summary>Assign numbers breadth-first from <paramref name="startNode"/>.</summary>
        public void Update(StartNode startNode, GameMode gameMode)
        {
            highestNumber = 0;
            Assign(startNode, "");
        }

        private void Assign(StepBaseNode node, string prefix)
        {
            if (node == null)
                return;
            if (string.IsNullOrEmpty(prefix))
            {
                node.StepNumber = (++highestNumber).ToString();
            }
            else
            {
                node.StepNumber = prefix;
            }
            var outputs = node.GetStepOutputs().ToList();
            for (int i = 0; i < outputs.Count; i++)
                Assign(outputs[i], node.StepNumber + "." + (i + 1));
        }
    }

    /// <summary>Assigns "F1..Fn" indices to fail-handler nodes.</summary>
    public class FailHandlerIndexUpdater
    {
        /// <summary>Re-index all handlers in the graph.</summary>
        public void Update(TrainingGraph graph)
        {
            if (graph == null)
                return;
            int i = 1;
            foreach (var handler in graph.FailHandlers)
                handler.FailIndex = "F" + i++;
        }
    }

    /// <summary>Manages exposed graph parameters (NodeGraphProcessor <see cref="ExposedParameter"/>).</summary>
    public class ExposedParameterManager
    {
        /// <summary>Active parameter manager (assigned by the parser when a graph is parsed).</summary>
        public static ExposedParameterManager Instance { get; set; }

        /// <summary>Fired when a parameter's value changes.</summary>
        public event Action OnParameterChanged;

        /// <summary>All exposed parameters on the graph.</summary>
        public List<ExposedParameter> Parameters { get; } = new List<ExposedParameter>();

        /// <summary>Collect parameters from a graph.</summary>
        public void AddParameters(TrainingGraph graph)
        {
            Parameters.Clear();
            if (graph?.exposedParameters != null)
                Parameters.AddRange(graph.exposedParameters);
        }

        /// <summary>Find by name.</summary>
        public ExposedParameter Get(string parameterName) =>
            Parameters.FirstOrDefault(p => p.name == parameterName);

        /// <summary>Set a parameter's value (static convenience).</summary>
        public static void SetExposedParameter<T>(string parameterName, T newValue)
        {
            Instance?.SetValue(parameterName, newValue);
        }

        /// <summary>Set a parameter's value.</summary>
        public void SetValue(string parameterName, object value)
        {
            var p = Get(parameterName);
            if (p == null)
                return;
            p.value = value;
            OnParameterChanged?.Invoke();
        }
    }
}
