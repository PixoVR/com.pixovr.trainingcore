using System;
using GraphProcessor;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>"Global Parameter" node: exposes a global parameter's current value on a port.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Global Parameter", null)]
    public class GlobalParameterNode : TrainingBaseNode
    {
        /// <summary>Name of the global parameter.</summary>
        [HideInInspector]
        public string ParameterName;

        /// <summary>Current value output.</summary>
        [Output(name = "Value")]
        public object Value;

        /// <inheritdoc/>
        public override string name => "Global Parameter";

        /// <inheritdoc/>
        protected override void Process()
        {
            Value = GlobalParameterManager.Instance.GetParameter(ParameterName)?.value;
        }
    }

    /// <summary>Graph helpers: BFS step collection.</summary>
    public static class GraphUtility
    {
        /// <summary>All step nodes reachable from <paramref name="start"/> over <see cref="StepBaseNode.GetStepOutputs()"/> (distinct, BFS).</summary>
        public static System.Collections.Generic.List<StepBaseNode> GetStepsStartingFrom(StepBaseNode start)
        {
            var result = new System.Collections.Generic.List<StepBaseNode>();
            var visited = new System.Collections.Generic.HashSet<StepBaseNode>();
            var queue = new System.Collections.Generic.Queue<StepBaseNode>();
            if (start != null)
                queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node == null || !visited.Add(node))
                    continue;
                result.Add(node);
                foreach (var next in node.GetStepOutputs())
                    if (next != null && !visited.Contains(next))
                        queue.Enqueue(next);
            }
            return result;
        }
    }

    /// <summary>Step-node → runtime-step conversion helpers.</summary>
    public static class GraphExtensions
    {
        /// <summary>Map step nodes to their parsed runtime steps via <paramref name="parser.Steps"/>.</summary>
        public static System.Collections.Generic.List<Flow.StepBase> ToSteps(
            this System.Collections.Generic.IEnumerable<StepBaseNode> stepNodes, GraphParser parser)
        {
            var result = new System.Collections.Generic.List<Flow.StepBase>();
            if (stepNodes == null || parser == null)
                return result;
            foreach (var node in stepNodes)
                if (node != null && parser.Steps.TryGetValue(node.GUID, out var step) && step != null)
                    result.Add(step);
            return result;
        }
    }
}
