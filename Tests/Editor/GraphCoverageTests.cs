using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GraphProcessor;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class ConditionalStepTests
    {
        private static EnumToStepMapping Map(int key, params StepBase[] steps) =>
            new EnumToStepMapping { Enum = key, Steps = new List<StepBase>(steps) };

        private static StepBase Step() => new StepExecutionBase();

        [SetUp]
        public void SetUp()
        {
            CommandHistory.Instance.Reset();
            ExposedParameterManager.Instance = new ExposedParameterManager();
        }

        [TearDown]
        public void TearDown()
        {
            CommandHistory.Instance.Reset();
            ExposedParameterManager.Instance = null;
            GlobalParameterManager.Instance = null;
        }

        [Test]
        public void IfStep_TrueLiteral_ChoosesTrueBranch()
        {
            var node = new IfStepConditionalNode { ConditionParameter = true };
            var step = new IfConditionalStep(node);
            var a = Step();
            var b = Step();
            step.OutputMappings.AddRange(new[] { Map(0, a), Map(1, b) });

            CollectionAssert.AreEquivalent(new[] { b }, step.Choose());
        }

        [Test]
        public void IfStep_FalseLiteral_ChoosesFalseBranch()
        {
            var node = new IfStepConditionalNode { ConditionParameter = false };
            var step = new IfConditionalStep(node);
            var a = Step();
            var b = Step();
            step.OutputMappings.AddRange(new[] { Map(0, a), Map(1, b) });

            CollectionAssert.AreEquivalent(new[] { a }, step.Choose());
        }

        [Test]
        public void Comparison_Ints_GreaterEqualsLess()
        {
            var a = Step(); var b = Step(); var c = Step();

            var node = new ComparisonConditionalNode { ParameterA = 3, ParameterB = 2 };
            var step = new ComparisonConditionalStep(node);
            step.OutputMappings.AddRange(new[] { Map(0, a), Map(1, b), Map(2, c) });
            CollectionAssert.AreEquivalent(new[] { a }, step.Choose());

            node.ParameterA = 2; node.ParameterB = 2;
            CollectionAssert.AreEquivalent(new[] { b }, step.Choose());

            node.ParameterA = 1; node.ParameterB = 2;
            CollectionAssert.AreEquivalent(new[] { c }, step.Choose());
        }

        [Test]
        public void Comparison_IntVsFloat_ConvertsAndCompares()
        {
            var node = new ComparisonConditionalNode { ParameterA = 2, ParameterB = 2.5f };
            var step = new ComparisonConditionalStep(node);
            var equal = Step(); var greater = Step(); var less = Step();
            step.OutputMappings.AddRange(new[] { Map(0, greater), Map(1, equal), Map(2, less) });

            // B (2.5f) converts to int (2) → equal
            CollectionAssert.AreEquivalent(new[] { equal }, step.Choose());

            node.ParameterB = 3.9f; // (int)3.9f → 4 > 2 → less
            CollectionAssert.AreEquivalent(new[] { less }, step.Choose());
        }

        [Test]
        public void Random_ZeroWeightOnFirst_AlwaysChoosesSecond()
        {
            var node = new RandomStepConditionalNode();
            var ports = node.GetAllDynamicPorts().Take(2).ToList();
            ports[0].Bind(); ports[0].Weight = 0;
            ports[1].Bind(); ports[1].Weight = 5;

            var step = new RandomStepConditional(node);
            var a = Step(); var b = Step();
            step.OutputMappings.AddRange(new[] { Map(0, a), Map(1, b) });

            for (int i = 0; i < 10; i++)
                CollectionAssert.AreEquivalent(new[] { b }, step.Choose());
        }

        [Test]
        public void GameMode_FollowsCurrentMode()
        {
            var node = new GameModeStepNode();
            var step = new GameModeConditionalStep(node);
            var training = Step(); var practice = Step();
            step.OutputMappings.AddRange(new[] { Map((int)GameMode.Training, training), Map((int)GameMode.Practice, practice) });

            var previous = GameModeManager.CurrentMode;
            try
            {
                GameModeManager.CurrentMode = GameMode.Training;
                CollectionAssert.AreEquivalent(new[] { training }, step.Choose());
                GameModeManager.CurrentMode = GameMode.Practice;
                CollectionAssert.AreEquivalent(new[] { practice }, step.Choose());
            }
            finally { GameModeManager.CurrentMode = previous; }
        }

        [Test]
        public void Iterator_ExpandsConditional_EntersChosenBranch()
        {
            var root = Step();
            var condNode = new IfStepConditionalNode { ConditionParameter = true };
            var cond = new IfConditionalStep(condNode);
            var target = Step();
            cond.OutputMappings.AddRange(new[] { Map(0), Map(1, target) });
            root.OutputSteps.Add(cond);

            var iterator = new GraphIterator(new GraphData { mainFlowRoot = root });
            iterator.StartIterator();
            iterator.NextSteps();

            CollectionAssert.Contains(iterator.CurrentSteps, target);
            CollectionAssert.DoesNotContain(iterator.CurrentSteps, cond);
            CollectionAssert.Contains(iterator.visitedStepGuids, cond.GUID);
        }
    }

    [TestFixture]
    public class ExtraActionTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandHistory.Instance.Reset();
            ExposedParameterManager.Instance = new ExposedParameterManager();
        }

        [TearDown]
        public void TearDown()
        {
            CommandHistory.Instance.Reset();
            ExposedParameterManager.Instance = null;
            GlobalParameterManager.Instance = null;
        }

        private static GameObject TargetGo()
        {
            var go = new GameObject("target");
            go.AddComponent<GuidComponent>();
            return go;
        }

        [Test]
        public void SetColorAction_ActChangesColour_UndoRestores()
        {
            var go = TargetGo();
            var renderer = go.AddComponent<MeshRenderer>();
            var initial = renderer.material.color;
            var node = new SetColorActionNode { TargetObject = go, TargetColor = Color.red };
            var action = node.Create();

            action.Act();
            Assert.AreEqual(Color.red, renderer.material.color);

            action.Undo();
            Assert.AreEqual(initial, renderer.material.color);
        }

        [Test]
        public void SetGameObjectPositionAction_ActMoves_UndoRestores()
        {
            var go = TargetGo();
            go.transform.position = new Vector3(1, 1, 1);
            var node = new SetGameObjectPositionActionNode
            {
                TargetObject = go,
                UseTransform = false,
                ObjectPostion = new Vector3(5, 6, 7),
            };
            var action = node.Create();

            action.Act();
            Assert.AreEqual(new Vector3(5, 6, 7), go.transform.position);

            action.Undo();
            Assert.AreEqual(new Vector3(1, 1, 1), go.transform.position);
        }

        [Test]
        public void SetGameObjectMaterialAction_ActSwaps_UndoRestores()
        {
            var go = TargetGo();
            var renderer = go.AddComponent<MeshRenderer>();
            var original = renderer.sharedMaterial;
            var replacement = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var node = new SetGameObjectMaterialActionNode { TargetObject = go, TargetMaterial = replacement };
            var action = node.Create();

            action.Act();
            Assert.AreSame(replacement, renderer.sharedMaterial);

            action.Undo();
            Assert.AreSame(original, renderer.sharedMaterial);
        }

        [Test]
        public void LogAction_Act_DoesNotThrow()
        {
            var action = new LogActionNode { Message = "hello", LogType = LogType.Warning }.Create();
            Assert.DoesNotThrow(() => action.Act());
            Assert.DoesNotThrow(() => action.Undo());
        }

        [Test]
        public void SetExposedParameterAction_UpdatesParameterValue()
        {
            var graph = ScriptableObject.CreateInstance<TrainingGraph>();
            graph.exposedParameters.Add(new ExposedParameter { guid = "g1", name = "count", value = 0 });
            var parameterNode = graph.AddNode(new ParameterNode { parameterGUID = "g1", GUID = "p1" });
            var setterNode = (SetIntParameterActionNode)graph.AddNode(new SetIntParameterActionNode { GUID = "s1" });
            ExposedParameterManager.Instance.AddParameters(graph);
            graph.edges.Add(new SerializableEdge
            {
                outputNode = parameterNode,
                inputNode = setterNode,
                inputFieldName = "ParameterNode",
            });
            setterNode.Value = 42;

            var action = setterNode.Create();
            action.Act();

            Assert.AreEqual(42, ExposedParameterManager.Instance.Parameters.First(p => p.guid == "g1").value);
        }

        [Test]
        public void GlobalParameterManager_Reset_RestoresInitialValue()
        {
            var parameters = ScriptableObject.CreateInstance<GlobalParameters>();
            var param = new ExposedParameter();
            param.Initialize("counter", 5);
            parameters.Data.Add(new GlobalParameter(param));
            var manager = new GlobalParameterManager();
            manager.SetParameters(parameters);
            GlobalParameterManager.Instance = manager;

            param.value = 99;
            manager.Reset();

            Assert.AreEqual(5, manager.GetParameter("counter").value);
        }
    }

    [TestFixture]
    public class GroupedInfoPointsStepTests
    {
        private class TestInfoPoint : InfoPointBase
        {
            public void Complete(bool correct) => OnInteractionCompleted(correct);
            public void Revert() => RevertCompletion();
        }

        [SetUp]
        public void SetUp()
        {
            CommandHistory.Instance.Reset();
        }

        [TearDown]
        public void TearDown() => CommandHistory.Instance.Reset();

        private static (GroupedInfoPointsStep step, InfoPointStep child, TestInfoPoint point) Make()
        {
            var pointGo = new GameObject("point");
            pointGo.AddComponent<GuidComponent>();
            var point = pointGo.AddComponent<TestInfoPoint>();

            var childNode = new InfoPointStepNode { InfoPoint = point };
            var child = new InfoPointStep(childNode);

            var parentNode = new GroupedInfoPointsStepNode
            {
                SelectRandomInfoPoints = false,
                RequireButtonPressOnComplete = false,
            };
            var parent = new GroupedInfoPointsStep(parentNode);
            parent.SetGroupedSteps(new List<StepBase> { child });
            return (parent, child, point);
        }

        [Test]
        public void AllChildrenCorrect_OutcomeTrue()
        {
            var (step, _, point) = Make();
            step.OnEnter();
            point.Complete(true);
            Assert.IsTrue(step.Outcome);
        }

        [Test]
        public void OneChildIncorrect_OutcomeFalse()
        {
            var (step, _, point) = Make();
            step.OnEnter();
            point.Complete(false);
            Assert.IsFalse(step.Outcome);
        }

        [Test]
        public void TwoPoints_BothCorrect_OutcomeTrue()
        {
            var (step, child, point) = Make();
            var secondGo = new GameObject("point2");
            secondGo.AddComponent<GuidComponent>();
            var point2 = secondGo.AddComponent<TestInfoPoint>();
            var child2 = new InfoPointStep(new InfoPointStepNode { InfoPoint = point2 });
            step.SetGroupedSteps(new List<StepBase> { child, child2 });

            step.OnEnter();
            point.Complete(true);
            Assert.IsNull(step.Outcome);
            point2.Complete(true);
            Assert.IsTrue(step.Outcome);
        }
    }
}
