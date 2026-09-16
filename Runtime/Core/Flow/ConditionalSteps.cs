using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Enum-keyed conditional; subclasses supply the runtime enum index.</summary>
    [Serializable]
    public class EnumConditionalStep : ConditionalStepBase
    {
        /// <summary>The branch key chosen at runtime.</summary>
        protected virtual int EnumIndex => 0;

        /// <summary>Create from node.</summary>
        public EnumConditionalStep(EnumConditionalStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public EnumConditionalStep() { }

        /// <inheritdoc/>
        public override List<StepBase> Choose() => ChooseOutput(EnumIndex);
    }

    /// <summary>"If" conditional: true/false branch on a literal or connected parameter.</summary>
    [Serializable]
    public class IfConditionalStep : ConditionalStepBase
    {
        private IfStepConditionalNode node;

        /// <summary>Create from node.</summary>
        public IfConditionalStep(IfStepConditionalNode node)
        {
            this.node = node;
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
        }

        /// <inheritdoc/>
        public override List<StepBase> Choose()
        {
            var resolved = ExposedParameterManager.ResolvePortValue(node, "ConditionParameter", node?.ConditionParameter);
            bool value = resolved is bool b && b;
            return ChooseOutput(value ? 1 : 0);
        }
    }

    /// <summary>"Compare" conditional: Greater/Equals/Less on two comparable values.</summary>
    [Serializable]
    public class ComparisonConditionalStep : ConditionalStepBase
    {
        private ComparisonConditionalNode comparisonNode;

        /// <summary>Create from node.</summary>
        public ComparisonConditionalStep(ComparisonConditionalNode node)
        {
            comparisonNode = node;
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
        }

        /// <inheritdoc/>
        public override List<StepBase> Choose()
        {
            object a = ExposedParameterManager.ResolvePortValue(comparisonNode, "ParameterA", comparisonNode?.ParameterA);
            object b = ExposedParameterManager.ResolvePortValue(comparisonNode, "ParameterB", comparisonNode?.ParameterB);
            if (a == null || b == null)
                return ChooseOutput(1);

            try
            {
                if (IsNumeric(a) && IsNumeric(b))
                    b = Convert.ChangeType(b, a.GetType());
            }
            catch (Exception)
            {
                return ChooseOutput(1);
            }

            int cmp;
            try
            {
                cmp = a is IComparable comparable ? comparable.CompareTo(b) : 0;
            }
            catch (Exception)
            {
                cmp = 0;
            }
            return ChooseOutput(cmp > 0 ? 0 : cmp < 0 ? 2 : 1);
        }

        private static bool IsNumeric(object o) =>
            o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint ||
            o is long || o is ulong || o is float || o is double || o is decimal;
    }

    /// <summary>"Random Step Conditional": picks one branch by weight.</summary>
    [Serializable]
    public class RandomStepConditional : ConditionalStepBase
    {
        /// <summary>Weighted branches (index = <see cref="ChooseOutput"/> key).</summary>
        [SerializeField]
        private List<WeightedOutputSteps> weighedOutputSteps;

        private readonly List<(int index, int weight)> branchWeights = new List<(int, int)>();

        /// <summary>Create from node.</summary>
        public RandomStepConditional(RandomStepConditionalNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            int i = 0;
            foreach (var port in node.GetUsedPorts())
            {
                branchWeights.Add((i, port.Weight));
                i++;
            }
            weighedOutputSteps = new List<WeightedOutputSteps>();
        }

        /// <inheritdoc/>
        public override List<StepBase> Choose()
        {
            if (branchWeights.Count == 0)
                return ChooseOutput(0);
            int total = branchWeights.Sum(b => Math.Max(0, b.weight));
            if (total <= 0)
                return ChooseOutput(branchWeights[0].index);
            double pick = RandomManager.InstanceExists
                ? RandomManager.Instance.Next(GUID)
                : UnityEngine.Random.value;
            double r = pick * total;
            foreach (var (index, weight) in branchWeights)
            {
                r -= Math.Max(0, weight);
                if (r < 0)
                    return ChooseOutput(index);
            }
            return ChooseOutput(branchWeights[branchWeights.Count - 1].index);
        }
    }

    /// <summary>"Game Mode" conditional: branches on <see cref="GameModeManager.CurrentMode"/>.</summary>
    [Serializable]
    public class GameModeConditionalStep : EnumConditionalStep
    {
        /// <inheritdoc/>
        protected override int EnumIndex => (int)GameModeManager.CurrentMode;

        /// <summary>Create from node.</summary>
        public GameModeConditionalStep(GameModeStepNode node) : base(node) { }
    }

    /// <summary>"Enum" conditional (legacy): branches on an enum field read off a bound object.</summary>
    [Serializable]
    public class DynamicEnumConditionalStep : EnumConditionalStep
    {
        [SerializeField]
        private string enumFieldName;

        [SerializeField]
        private UnityEngine.Object globalObject;

        private System.Reflection.FieldInfo enumField =>
            globalObject != null ? globalObject.GetType().GetField(enumFieldName) : null;

        /// <summary>Create from node.</summary>
        public DynamicEnumConditionalStep(DynamicEnumConditionalStepNode node) : base(node)
        {
            globalObject = node.GlobalObject;
            enumFieldName = node.EnumStoredName;
        }

        /// <inheritdoc/>
        public override List<StepBase> Choose()
        {
            var value = enumField?.GetValue(globalObject);
            int index = 0;
            try { index = Convert.ToInt32(value ?? 0); } catch (Exception) { }
            return ChooseOutput(index);
        }
    }
}
