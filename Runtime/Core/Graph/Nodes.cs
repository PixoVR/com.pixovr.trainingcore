using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility.Display;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>Base for all TrainingCore nodes: scene-reference data + game-mode include flags.</summary>
    public abstract class TrainingBaseNode : BaseNode
    {
        /// <summary>Editor search highlight flag (serialized).</summary>
        [HideInInspector]
        public bool SearchHighlight;

        /// <summary>Saved scene references + game-mode data.</summary>
        [HideInInspector]
        public NodeData Data = new NodeData();

        /// <summary>True when this node is included for the given mode.</summary>
        public bool IsIncludedInMode(GameMode gameMode) => Data == null || Data.IsIncludedInMode(gameMode);

        /// <summary>Set the include flag for a mode.</summary>
        public void OnGameModeValueChanged(GameMode mode, bool value) =>
            (Data ??= new NodeData()).SetIncludeValueFor(mode, value);

        /// <summary>Read a saved scene component bound to <paramref name="propertyName"/>.</summary>
        protected T GetSavedComponent<T>(string propertyName) where T : Component
        {
            var prop = Data?.Find(propertyName);
            return prop?.ObjectReference?.GetComponent<T>();
        }

        /// <summary>Store a scene component binding under <paramref name="propertyName"/>.</summary>
        protected void SetSavedComponent<T>(string propertyName, T value) where T : Component
        {
            if (Data == null)
                Data = new NodeData();
            var prop = Data.Find(propertyName) ?? new NodeSavedProperty(propertyName);
            prop.ObjectReference = value == null ? null : new GuidReference(value.gameObject);
            Data.Add(prop);
        }

        /// <summary>Collect <see cref="NodeSavedProperty"/> entries (override to register bindings).</summary>
        protected virtual void AddReferences() { }

        /// <summary>Capture references into <see cref="Data"/> (called by tooling).</summary>
        public void SaveReferences()
        {
            Data ??= new NodeData();
            AddReferences();
        }

        /// <summary>Edges where the named port on this node is the input.</summary>
        public List<SerializableEdge> GetEdgesForInputPort(string portName) =>
            graph == null ? new List<SerializableEdge>()
                : graph.edges.Where(e => e.inputNode == this && e.inputFieldName == portName).ToList();

        /// <summary>Edges where the named port on this node is the output.</summary>
        public List<SerializableEdge> GetEdgesForOutputPort(string portName) =>
            graph == null ? new List<SerializableEdge>()
                : graph.edges.Where(e => e.outputNode == this && e.outputFieldName == portName).ToList();

        /// <summary>Flow outputs of this node (mode-aware).</summary>
        public virtual IEnumerable<StepBaseNode> GetStepOutputs(GameMode gameMode) =>
            GetIncludedOutputStepsFrom(GetStepOutputs(), gameMode);

        /// <summary>Flow outputs of this node.</summary>
        public virtual IEnumerable<StepBaseNode> GetStepOutputs() => Enumerable.Empty<StepBaseNode>();

        /// <summary>Filter output nodes to those included in a mode.</summary>
        protected IEnumerable<StepBaseNode> GetIncludedOutputStepsFrom(IEnumerable<StepBaseNode> outputNodes, GameMode gameMode) =>
            outputNodes.Where(n => n == null || n.IsIncludedInMode(gameMode));

        /// <summary>All nodes connected to the named port.</summary>
        public IEnumerable<T> GetNodesOnPort<T>(string portName, bool IsOutputPort = true) where T : BaseNode
        {
            var edges = IsOutputPort ? GetEdgesForOutputPort(portName) : GetEdgesForInputPort(portName);
            return edges.Select(e => IsOutputPort ? e.inputNode : e.outputNode).OfType<T>();
        }
    }

    /// <summary>Base for all step nodes.</summary>
    public abstract class StepBaseNode : TrainingBaseNode
    {
        /// <summary>Whether this step is a valid skip destination.</summary>
        [HideInInspector]
        public bool IsSkipPoint = false;

        private string _stepNumber = "0";

        /// <summary>Parent group node, if nested.</summary>
        [HideInInspector]
        [SerializeField]
        public StepBaseNode ParentNode = null;

        /// <summary>Dotted step number assigned by <see cref="StepNumberUpdater"/>.</summary>
        [HideInInspector]
        public string StepNumber
        {
            get => _stepNumber;
            set
            {
                if (_stepNumber == value)
                    return;
                _stepNumber = value;
                OnStepNumberChanged?.Invoke(value);
            }
        }

        /// <summary>Fired when the assigned step number changes.</summary>
        public event Action<string> OnStepNumberChanged;

        /// <summary>Nodes feeding this step's input port.</summary>
        public virtual IEnumerable<StepBaseNode> GetInputFlowNodes() => Enumerable.Empty<StepBaseNode>();

        /// <summary>Index of <paramref name="node"/> among this node's step outputs.</summary>
        public int GetStepOutputChildIndex(StepBaseNode node) => GetStepOutputs().ToList().IndexOf(node);

        /// <summary>Create the runtime step twin.</summary>
        public abstract StepBase Create();
    }

    /// <summary>Step node with execution flow + action ports and fail data.</summary>
    public abstract class StepExecutionNode : StepBaseNode
    {
        /// <summary>Execution input port.</summary>
        [Input(name = "Start", allowMultiple = true)]
        public ExecutionLink executed;

        /// <summary>Use the graph's default fail handler.</summary>
        [HideInInspector]
        public bool UseDefaultFailHandler = true;

        /// <summary>Explicit fail-handler GUID.</summary>
        [HideInInspector]
        public string SelectedFailHandlerGuid;

        /// <summary>Fail exceptions serialized on this node.</summary>
        [HideInInspector]
        public FailData ExceptionsData;

        /// <summary>User description.</summary>
        [HideInInspector]
        public string Description;

        /// <summary>Show description in UI.</summary>
        [HideInInspector]
        public bool ShowDescription = false;

        /// <summary>Show exceptions foldout.</summary>
        [HideInInspector]
        public bool ShowExceptions = true;

        /// <summary>Play a sound on completion.</summary>
        [HideInInspector]
        public bool PlaySoundOnComplete = false;

        /// <summary>Input flow nodes connected to <see cref="executed"/>.</summary>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("executed", false);

        /// <summary>Action nodes bound to the start-actions port.</summary>
        public abstract IEnumerable<ActionNode> GetOnStartActionNodes(GameMode gameMode);

        /// <summary>Action nodes bound to the finish-actions port.</summary>
        public abstract IEnumerable<ActionNode> GetOnFinishActionNodes(GameMode gameMode);
    }

    /// <summary>Step node with a single linear flow (executed in, executes out + both action ports).</summary>
    public abstract class SingleFlowStepNode : StepExecutionNode
    {
        /// <summary>Complete-actions output port.</summary>
        [Output(name = "Complete Actions", allowMultiple = true)]
        public ActionLink OnFinishActions;

        /// <summary>Start-actions output port.</summary>
        [Output(name = "Start Actions", allowMultiple = true)]
        public ActionLink OnStartActions;

        /// <summary>Execution output port.</summary>
        [Output(name = "Complete", allowMultiple = true)]
        public ExecutionLink executes;

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() => GetNodesOnPort<StepBaseNode>("executes");

        /// <summary>Action nodes on the start port.</summary>
        public IEnumerable<ActionNode> GetOnStartActionNodes() => GetNodesOnPort<ActionNode>("OnStartActions");

        /// <inheritdoc/>
        public override IEnumerable<ActionNode> GetOnStartActionNodes(GameMode gameMode) =>
            GetOnStartActionNodes().Where(n => n.IsIncludedInMode(gameMode));

        /// <summary>Action nodes on the finish port.</summary>
        public IEnumerable<ActionNode> GetOnFinishActionNodes() => GetNodesOnPort<ActionNode>("OnFinishActions");

        /// <inheritdoc/>
        public override IEnumerable<ActionNode> GetOnFinishActionNodes(GameMode gameMode) =>
            GetOnFinishActionNodes().Where(n => n.IsIncludedInMode(gameMode));
    }

    /// <summary>Step node targeting a single interactable via the "TargetInteractable" saved property.</summary>
    public abstract class ObjectInteractionStepNodeBase : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the target interactable.</summary>
        protected readonly string TargetInteractedObjectPropertyName = "TargetInteractable";

        /// <summary>Target interactable GameObject.</summary>
        [HideInInspector]
        public GameObject TargetInteractedGameObject
        {
            get => Data?.Find(TargetInteractedObjectPropertyName)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Events.ObservableSubject>(TargetInteractedObjectPropertyName,
                value == null ? null : value.GetComponent<Events.ObservableSubject>());
        }

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(TargetInteractedObjectPropertyName);
    }

    /// <summary>Step node that branches (conditional).</summary>
    public abstract class ConditionalStepNode : StepBaseNode
    {
        /// <summary>Branch key → output nodes (default: single branch 0 = all outputs).</summary>
        public virtual IEnumerable<(int key, IEnumerable<StepBaseNode> nodes)> GetOutputBranches() =>
            new List<(int, IEnumerable<StepBaseNode>)> { (0, GetStepOutputs()) };
    }

    /// <summary>Conditional whose outputs are chosen dynamically at runtime.</summary>
    public abstract class DynamicConditionalStepNode : ConditionalStepNode
    {
        /// <summary>Input flow port.</summary>
        [Input(name = "Start", allowMultiple = true)]
        public ExecutionLink executed;

        /// <summary>Dynamic output port 1.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes1")]
        public ExecutionLink Executes1;

        /// <summary>Dynamic output port 2.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes2")]
        public ExecutionLink Executes2;

        /// <summary>Dynamic output port 3.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes3")]
        public ExecutionLink Executes3;

        /// <summary>Dynamic output port 4.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes4")]
        public ExecutionLink Executes4;

        /// <summary>Dynamic output port 5.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes5")]
        public ExecutionLink Executes5;

        /// <summary>Dynamic output port 6.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes6")]
        public ExecutionLink Executes6;

        /// <summary>Dynamic output port 7.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes7")]
        public ExecutionLink Executes7;

        /// <summary>Dynamic output port 8.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes8")]
        public ExecutionLink Executes8;

        /// <summary>Dynamic output port 9.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes9")]
        public ExecutionLink Executes9;

        /// <summary>Dynamic output port 10.</summary>
        [Output(allowMultiple = true)]
        [FormerlySerializedAs("executes10")]
        public ExecutionLink Executes10;

        /// <summary>Bookkeeping for the dynamic ports.</summary>
        [HideInInspector]
        [SerializeField]
        public List<DynamicPort> DynamicPorts;

        /// <summary>Number of bound ports.</summary>
        public int UsedPorts => GetUsedPorts().Count();

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("executed", false);

        /// <summary>Pick the output nodes at runtime.</summary>
        public abstract IEnumerable<StepBaseNode> ChooseOutputNodes();

        /// <summary>All dynamic ports (lazily initialized).</summary>
        public IEnumerable<DynamicPort> GetAllDynamicPorts() =>
            DynamicPorts ??= Enumerable.Range(1, 10).Select(i => new DynamicPort($"Executes{i}")).ToList();

        /// <summary>Bound dynamic ports.</summary>
        public IEnumerable<DynamicPort> GetUsedPorts() => GetAllDynamicPorts().Where(p => p.IsUsed());

        /// <summary>First unbound port.</summary>
        protected DynamicPort GetFreePort() => GetAllDynamicPorts().FirstOrDefault(p => !p.IsUsed());

        /// <summary>Bind a free port; false when all are taken.</summary>
        public bool TryBindFreePort(out DynamicPort port)
        {
            port = GetFreePort();
            port?.Bind();
            return port != null;
        }

        /// <summary>Outputs wired to a dynamic port.</summary>
        public IEnumerable<StepBaseNode> GetStepOutputs(DynamicPort port) =>
            port == null ? Enumerable.Empty<StepBaseNode>() : GetNodesOnPort<StepBaseNode>(port.FieldName);

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() =>
            GetUsedPorts().SelectMany(GetStepOutputs);

        /// <summary>Free one port by field name.</summary>
        public void ResetPort(string fieldName) =>
            GetAllDynamicPorts().FirstOrDefault(d => d.FieldName == fieldName)?.Reset();

        /// <summary>Free all ports.</summary>
        public void ResetPorts() => GetAllDynamicPorts().ToList().ForEach(p => p.Reset());

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            DynamicPorts ??= GetAllDynamicPorts().ToList();
        }
    }

    /// <summary>Conditional node whose outputs map to enum values via dynamic ports.</summary>
    public abstract class EnumConditionalStepNode : DynamicConditionalStepNode
    {
        /// <summary>Enum index → dynamic port.</summary>
        [HideInInspector]
        [SerializeField]
        public Dictionary<int, DynamicPort> enumToPortDictionary = new Dictionary<int, DynamicPort>();

        /// <summary>Enum type this conditional switches on.</summary>
        public abstract Type SelectedEnumType { get; }

        /// <summary>Outputs wired to the port for an enum index.</summary>
        protected IEnumerable<StepBaseNode> GetStepOutputsFor(int enumIndex)
        {
            if (enumToPortDictionary.TryGetValue(enumIndex, out var port) && port != null)
                return GetNodesOnPort<StepBaseNode>(port.FieldName);
            return Enumerable.Empty<StepBaseNode>();
        }

        /// <inheritdoc/>
        public override IEnumerable<(int key, IEnumerable<StepBaseNode> nodes)> GetOutputBranches() =>
            enumToPortDictionary
                .Select(kv => (kv.Key, GetStepOutputsFor(kv.Key)))
                .ToList();
    }
}
