using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>"If" conditional: branches on a boolean literal or connected parameter.</summary>
    [Serializable]
    [NodeMenuItem("Current/Conditionals/If", null)]
    public class IfStepConditionalNode : ConditionalStepNode
    {
        /// <summary>Execution input.</summary>
        [Input(name = "Execute", allowMultiple = true)]
        public ExecutionLink executed;

        /// <summary>Boolean literal or connected parameter.</summary>
        [Input(name = "Condition")]
        public bool ConditionParameter;

        /// <summary>False-branch outputs.</summary>
        [Output(name = "False", allowMultiple = true)]
        public ExecutionLink FalseOutput;

        /// <summary>True-branch outputs.</summary>
        [Output(name = "True", allowMultiple = true)]
        public ExecutionLink TrueOutput;

        /// <inheritdoc/>
        public override string name => "If";

        /// <summary>Steps wired to the true output.</summary>
        public IEnumerable<StepBaseNode> GetTrueOutputNodes() => GetNodesOnPort<StepBaseNode>("TrueOutput");

        /// <summary>Steps wired to the false output.</summary>
        public IEnumerable<StepBaseNode> GetFalseOutputNodes() => GetNodesOnPort<StepBaseNode>("FalseOutput");

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("executed", false);

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() =>
            GetTrueOutputNodes().Concat(GetFalseOutputNodes());

        /// <inheritdoc/>
        public override IEnumerable<(int key, IEnumerable<StepBaseNode> nodes)> GetOutputBranches() =>
            new List<(int, IEnumerable<StepBaseNode>)> { (0, GetFalseOutputNodes()), (1, GetTrueOutputNodes()) };

        /// <summary>The connected condition parameter, if any.</summary>
        public ExposedParameter GetConditionParameter() =>
            ExposedParameterManager.ResolveConnectedParameter(this, "ConditionParameter");

        /// <inheritdoc/>
        public override StepBase Create() => new IfConditionalStep(this);
    }

    /// <summary>"Compare" conditional: Greater/Equals/Less branches on two comparable values.</summary>
    [Serializable]
    [NodeMenuItem("Current/Conditionals/Comparison", null)]
    public class ComparisonConditionalNode : ConditionalStepNode
    {
        /// <summary>Execution input.</summary>
        [Input(name = "Executed")]
        public ExecutionLink Executed;

        /// <summary>Left operand (literal or parameter).</summary>
        [Input(name = "Parameter A")]
        public IComparable ParameterA;

        /// <summary>Right operand (literal or parameter).</summary>
        [Input(name = "Parameter B")]
        public IComparable ParameterB;

        /// <summary>Outputs for A &gt; B.</summary>
        [Output(name = "Greater", allowMultiple = true)]
        public ExecutionLink GreaterThanOutputs;

        /// <summary>Outputs for A == B.</summary>
        [Output(name = "Equals", allowMultiple = true)]
        public ExecutionLink EqualsOutputs;

        /// <summary>Outputs for A &lt; B.</summary>
        [Output(name = "Less", allowMultiple = true)]
        public ExecutionLink LessThanOutputs;

        /// <inheritdoc/>
        public override string name => "Compare";

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("Executed", false);

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() =>
            GetNodesOnPort<StepBaseNode>("GreaterThanOutputs")
                .Concat(GetNodesOnPort<StepBaseNode>("EqualsOutputs"))
                .Concat(GetNodesOnPort<StepBaseNode>("LessThanOutputs"));

        /// <inheritdoc/>
        public override IEnumerable<(int key, IEnumerable<StepBaseNode> nodes)> GetOutputBranches() =>
            new List<(int, IEnumerable<StepBaseNode>)>
            {
                (0, GetNodesOnPort<StepBaseNode>("GreaterThanOutputs")),
                (1, GetNodesOnPort<StepBaseNode>("EqualsOutputs")),
                (2, GetNodesOnPort<StepBaseNode>("LessThanOutputs")),
            };

        /// <summary>The parameter connected to the named port, if any.</summary>
        public ExposedParameter GetParameter(string paramaterName) =>
            ExposedParameterManager.ResolveConnectedParameter(this, paramaterName);

        /// <inheritdoc/>
        public override StepBase Create() => new ComparisonConditionalStep(this);
    }

    /// <summary>"Random Step Conditional": picks one output port by weight.</summary>
    [Serializable]
    [NodeMenuItem("Current/Conditionals/Random Step Conditional", null)]
    public class RandomStepConditionalNode : DynamicConditionalStepNode
    {
        /// <summary>Execution input.</summary>
        [Input(name = "Execute", allowMultiple = true)]
        public ExecutionLink Executed;

        private const int initialPortsCount = 2;

        /// <summary>All output ports (the bound subset is what matters).</summary>
        public List<DynamicPort> OutputPorts => DynamicPorts;

        /// <inheritdoc/>
        public override string name => "Random Step Conditional";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            var ports = GetAllDynamicPorts().Take(initialPortsCount).ToList();
            foreach (var p in ports)
                p.Bind();
        }

        /// <summary>Port weight + its output nodes, in used-port order (index = branch key).</summary>
        public List<(int weight, List<StepBaseNode> steps)> GetWeightedOutputNodes() =>
            GetUsedPorts().Select(p => (p.Weight, GetStepOutputs(p).ToList())).ToList();

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> ChooseOutputNodes()
        {
            var weighted = GetWeightedOutputNodes();
            int total = weighted.Sum(w => Math.Max(0, w.weight));
            if (weighted.Count == 0)
                return Enumerable.Empty<StepBaseNode>();
            if (total <= 0)
                return weighted[0].steps;
            double pick = Utility.RandomManager.InstanceExists
                ? Utility.RandomManager.Instance.Next(GUID)
                : UnityEngine.Random.value;
            double r = pick * total;
            foreach (var (weight, steps) in weighted)
            {
                r -= Math.Max(0, weight);
                if (r < 0)
                    return steps;
            }
            return weighted[weighted.Count - 1].steps;
        }

        /// <inheritdoc/>
        public override IEnumerable<(int key, IEnumerable<StepBaseNode> nodes)> GetOutputBranches() =>
            GetUsedPorts().Select((p, i) => (i, GetStepOutputs(p))).ToList();

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("Executed", false);

        /// <inheritdoc/>
        public override StepBase Create() => new RandomStepConditional(this);
    }

    /// <summary>"Game Mode" conditional: branches on the active <see cref="GameMode"/>.</summary>
    [Serializable]
    [NodeMenuItem("Current/Conditionals/Game Mode", null)]
    public class GameModeStepNode : EnumConditionalStepNode
    {
        /// <summary>Execution input.</summary>
        [Input(name = "Execute", allowMultiple = true)]
        public ExecutionLink Executed;

        /// <inheritdoc/>
        public override Type SelectedEnumType => typeof(GameMode);

        /// <inheritdoc/>
        public override string name => "Game Mode";

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> ChooseOutputNodes() =>
            GetStepOutputsFor((int)GameModeManager.CurrentMode);

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("Executed", false);

        /// <inheritdoc/>
        public override StepBase Create() => new GameModeConditionalStep(this);
    }

    /// <summary>"Enum" conditional (legacy): branches on an enum field read off a bound object.</summary>
    [Serializable]
    [NodeMenuItem(null, null, menuTitle = "Legacy/Conditionals/Enum")]
    public class DynamicEnumConditionalStepNode : EnumConditionalStepNode
    {
        /// <summary>Execution input.</summary>
        [Input(name = "Execute", allowMultiple = true)]
        public ExecutionLink Executed;

        /// <summary>Object hosting the enum field.</summary>
        [SerializeField]
        [HideInInspector]
        public UnityEngine.Object GlobalObject;

        [SerializeField]
        [HideInInspector]
        private string globalTypeName = "";

        [SerializeField]
        [HideInInspector]
        private string EnumFieldStoredName;

        /// <summary>Name of the enum field on <see cref="GlobalObject"/>.</summary>
        [SerializeField]
        [HideInInspector]
        public string EnumStoredName;

        /// <summary>Type of <see cref="GlobalObject"/> (serialized by name).</summary>
        public Type GlobalObjectType
        {
            get => string.IsNullOrEmpty(globalTypeName) ? null : Type.GetType(globalTypeName);
            set => globalTypeName = value?.AssemblyQualifiedName;
        }

        /// <summary>The resolved enum field.</summary>
        public System.Reflection.FieldInfo SelectedEnumField
        {
            get => GlobalObject != null ? GlobalObject.GetType().GetField(EnumStoredName) : null;
            set => EnumStoredName = EnumFieldStoredName = value?.Name;
        }

        /// <inheritdoc/>
        public override Type SelectedEnumType => SelectedEnumField?.FieldType;

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> ChooseOutputNodes() =>
            GetStepOutputsFor(Convert.ToInt32(SelectedEnumField?.GetValue(GlobalObject) ?? 0));

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetInputFlowNodes() => GetNodesOnPort<StepBaseNode>("Executed", false);

        /// <inheritdoc/>
        public override StepBase Create() => new DynamicEnumConditionalStep(this);
    }
}
