using System.Linq;
using NUnit.Framework;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.XRI;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class QuestionNodeTests
    {
        [SetUp]
        public void SetUp() => UnityEngine.TestRuntime.IsPlaying = false;

        [Test]
        public void Parse_QuestionNode_ProducesQuestionStep()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var q = GraphTestHelpers.Node<QuestionNode>(g, "q");
            GraphTestHelpers.Flow(g, start, "executes", q, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            Assert.IsInstanceOf<QuestionStep>(data.mainFlowRoot.OutputSteps[0]);
            Assert.AreEqual("Question", (data.mainFlowRoot.OutputSteps[0] as StepBase).Name);
        }

        [Test]
        public void QuestionStep_CorrectAnswer_TakesCorrectBranch()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var q = GraphTestHelpers.Node<QuestionNode>(g, "q");
            var good = GraphTestHelpers.Node<GenericStepNode>(g, "good");
            var bad = GraphTestHelpers.Node<GenericStepNode>(g, "bad");
            GraphTestHelpers.Flow(g, start, "executes", q, "executed");
            GraphTestHelpers.Flow(g, q, "CorrectOutput", good, "executed");
            GraphTestHelpers.Flow(g, q, "IncorrectOutput", bad, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var step = data.mainFlowRoot.OutputSteps[0] as QuestionStep;
            Assert.NotNull(step);
            Assert.AreEqual(2, step.OutputSteps.Count);

            step.AnswerQuestion(true);
            Assert.AreEqual(true, step.Outcome);
            Assert.AreEqual(1, step.OutputSteps.Count);
            Assert.AreEqual("good", step.OutputSteps[0].GUID);
        }

        [Test]
        public void QuestionStep_IncorrectAnswer_TakesIncorrectBranch()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var q = GraphTestHelpers.Node<QuestionNode>(g, "q");
            var good = GraphTestHelpers.Node<GenericStepNode>(g, "good");
            var bad = GraphTestHelpers.Node<GenericStepNode>(g, "bad");
            GraphTestHelpers.Flow(g, start, "executes", q, "executed");
            GraphTestHelpers.Flow(g, q, "CorrectOutput", good, "executed");
            GraphTestHelpers.Flow(g, q, "IncorrectOutput", bad, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var step = data.mainFlowRoot.OutputSteps[0] as QuestionStep;
            step.AnswerQuestion(false);
            Assert.AreEqual(false, step.Outcome);
            Assert.AreEqual("bad", step.OutputSteps[0].GUID);
        }

        [Test]
        public void Parse_QuestionNode_PreservesSerializedFields()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var q = GraphTestHelpers.Node<QuestionNode>(g, "q");
            q.AnswerCount = 4;
            q.RandomOrder = false;
            q.Answers.Add(new Data.Answer { Text = "yes", Correct = true });
            GraphTestHelpers.Flow(g, start, "executes", q, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var step = data.mainFlowRoot.OutputSteps[0] as QuestionStep;
            Assert.NotNull(step.QuestionNode);
            Assert.AreEqual(1, step.QuestionNode.Answers.Count);
            Assert.AreEqual(4, step.QuestionNode.AnswerCount);
        }
    }

    [TestFixture]
    public class LostObjectManagerTests
    {
        [SetUp]
        public void SetUp() => UnityEngine.TestRuntime.IsPlaying = true;

        [TearDown]
        public void TearDown()
        {
            typeof(Utility.SingletonBehaviour<LostObjectManager>)
                .GetField("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(null, null);
        }

        [Test]
        public void Tick_ResetsObjectThatFellOutOfRange()
        {
            var managerGo = new GameObject("manager");
            var manager = managerGo.AddComponent<LostObjectManager>();
            var go = new GameObject("lost");
            var lost = go.AddComponent<LostObject>();

            go.transform.position = new Vector3(0, -100, 0);
            manager.Tick(0.016f);
            Assert.AreEqual(0f, go.transform.position.y, 0.001f);
        }

        [Test]
        public void Tick_DoesNotResetObjectInRange()
        {
            var managerGo = new GameObject("manager");
            var manager = managerGo.AddComponent<LostObjectManager>();
            var go = new GameObject("lost");
            var lost = go.AddComponent<LostObject>();

            go.transform.position = new Vector3(1, 0, 0);
            manager.Tick(0.016f);
            Assert.AreEqual(1f, go.transform.position.x, 0.001f);
        }

        [Test]
        public void Tick_DroppedObject_ResetsAfterWaitTime()
        {
            var managerGo = new GameObject("manager");
            var manager = managerGo.AddComponent<LostObjectManager>();
            var go = new GameObject("lost");
            var lost = go.AddComponent<LostObject>();
            lost.ResetWhenOutOfRange = false;
            lost.ResetWhenDropped = true;
            lost.DroppedWaitTime = 1f;

            go.transform.position = new Vector3(5, 0, 0);
            manager.Tick(0.5f);
            Assert.AreEqual(5f, go.transform.position.x, 0.001f);
            manager.Tick(0.6f);
            Assert.AreEqual(0f, go.transform.position.x, 0.001f);
        }

        [Test]
        public void TrackObject_UntrackObject()
        {
            var managerGo = new GameObject("manager");
            var manager = managerGo.AddComponent<LostObjectManager>();
            var go = new GameObject("lost");
            var lost = go.AddComponent<LostObject>();
            Assert.IsTrue(manager.TrackedObjects.Contains(lost));
            manager.UntrackObject(lost);
            Assert.IsFalse(manager.TrackedObjects.Contains(lost));
        }
    }
}
