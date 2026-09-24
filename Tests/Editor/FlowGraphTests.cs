using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using NUnit.Framework;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Photon;
using PixoVR.TrainingCore.Events;
using PixoVR.Apex;
using PixoVR.Apex.XAPI;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    /// <summary>Builds TrainingGraph assets programmatically for parse/flow tests.</summary>
    public class GraphTestHelpers
    {
        public static T Node<T>(TrainingGraph graph, string guid) where T : BaseNode, new()
        {
            var node = new T { GUID = guid };
            node.Initialize(graph);
            graph.nodes.Add(node);
            graph.nodesPerGUID[guid] = node;
            return node;
        }

        public static void Flow(TrainingGraph graph, BaseNode from, string fromPort, BaseNode to, string toPort)
        {
            graph.edges.Add(new SerializableEdge
            {
                GUID = System.Guid.NewGuid().ToString(),
                outputNode = from,
                inputNode = to,
                outputFieldName = fromPort,
                inputFieldName = toPort
            });
        }

        public static TrainingGraph NewGraph() => ScriptableObject.CreateInstance<TrainingGraph>();
    }

    [TestFixture]
    public class GraphParserTests
    {

        private (TrainingGraph graph, StartNode start) LinearGraph(int steps)
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            StepBaseNode prev = start;
            for (int i = 0; i < steps; i++)
            {
                var step = GraphTestHelpers.Node<GenericStepNode>(g, $"step{i}");
                GraphTestHelpers.Flow(g, prev, "executes", step, "executed");
                prev = step;
            }
            return (g, start);
        }

        [Test]
        public void Parse_LinearGraph_CreatesChain()
        {
            var (g, _) = LinearGraph(3);
            var data = new GraphParser(GameMode.Training).Parse(g);
            Assert.NotNull(data.mainFlowRoot);
            // chain length = start + 3 steps
            Assert.AreEqual(4, data.mainFlowRoot.OutputSteps.Count == 1
                ? CountChain(data.mainFlowRoot)
                : -1);
        }

        private int CountChain(StepBase root)
        {
            int n = 0;
            var s = root;
            while (s != null)
            {
                n++;
                s = s.OutputSteps.FirstOrDefault();
            }
            return n;
        }

        [Test]
        public void Parse_BranchStep_TwoOutputs()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var branch = GraphTestHelpers.Node<GenericStepNode>(g, "b");
            var a = GraphTestHelpers.Node<GenericStepNode>(g, "a");
            var c = GraphTestHelpers.Node<GenericStepNode>(g, "c");
            GraphTestHelpers.Flow(g, start, "executes", branch, "executed");
            GraphTestHelpers.Flow(g, branch, "executes", a, "executed");
            GraphTestHelpers.Flow(g, branch, "executes", c, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var branchStep = data.mainFlowRoot.OutputSteps[0];
            Assert.AreEqual(2, branchStep.OutputSteps.Count);
        }

        [Test]
        public void Parse_FailHandlerNode_BecomesFailureHandlerStep()
        {
            var (g, _) = LinearGraph(1);
            var handler = GraphTestHelpers.Node<FailHandlerNode>(g, "fh");
            handler.IsDefault = true;

            var data = new GraphParser(GameMode.Training).Parse(g);
            Assert.AreEqual(1, data.FailureHandlerSteps.Count);
            Assert.NotNull(data.DefaultFailureHandler);
        }

        [Test]
        public void Parse_StepActions_AreAttached()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var step = GraphTestHelpers.Node<GenericStepNode>(g, "s1");
            var action = GraphTestHelpers.Node<SetGameObjectActiveStateNode>(g, "a1");
            GraphTestHelpers.Flow(g, start, "executes", step, "executed");
            GraphTestHelpers.Flow(g, step, "OnStartActions", action, "ActionLink");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var rt = data.mainFlowRoot.OutputSteps[0] as StepExecutionBase;
            Assert.NotNull(rt);
            Assert.AreEqual(1, rt.StartActions.Count);
        }

        [Test]
        public void Iterator_LinearFlow_AdvancesOnCompletion()
        {
            var (g, _) = LinearGraph(2);
            var data = new GraphParser(GameMode.Training).Parse(g);
            var it = data.GetIterator();
            it.StartIterator();
            Assert.AreEqual("step0", it.CurrentSteps[0].GUID);

            it.CurrentSteps[0].Complete();
            Assert.AreEqual("step1", it.CurrentSteps[0].GUID);
            Assert.AreEqual(3, it.VisitedNodes.Count);
        }

        [Test]
        public void NormalFlow_SkipForward_And_Back()
        {
            var (g, _) = LinearGraph(2);
            var data = new GraphParser(GameMode.Training).Parse(g);
            var flow = new NormalFlow(data);
            flow.InitializeIterator();
            flow.Start();

            flow.SkipToNext();
            Assert.AreEqual("step0", flow.CurrentSteps[0].GUID);
            flow.SkipToBack();
            Assert.AreEqual("start", flow.CurrentSteps[0].GUID);
        }

        [Test]
        public void FailHandlerFlow_Completes_BackToNormal()
        {
            var (g, _) = LinearGraph(1);
            var handlerNode = GraphTestHelpers.Node<FailHandlerNode>(g, "fh");
            handlerNode.IsDefault = true;
            var recovery = GraphTestHelpers.Node<GenericStepNode>(g, "rec");
            handlerNode.enumToPortDictionary[(int)GameMode.Training] = new DynamicPort("Executes1");
            GraphTestHelpers.Flow(g, handlerNode, "Executes1", recovery, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var normal = new NormalFlow(data);
            var failFlow = new FailHandlerFlow(data.DefaultFailureHandler, normal);
            bool completed = false;
            failFlow.OnFlowCompleted += () => completed = true;
            failFlow.Start();
            failFlow.Complete();
            Assert.IsTrue(completed);
            Assert.NotNull(failFlow.ReturnFlow);
        }
    }

    [TestFixture]
    public class PhotonSerializationTests
    {
        [Test]
        public void InteractionEventArgs_RoundTrips_ThroughPayload()
        {
            var subject = new GameObject("s").AddComponent<ObservableSubject>();
            var tap = new TapInteractionEventArgs(subject, 1.5f);

            object[] payload = PhotonEventSerializer.Serialize(tap);
            var sync = PhotonEventSerializer.DeserializeEventSyncData(payload);

            Assert.NotNull(sync);
            Assert.AreEqual(tap.SubjectId, sync.SubjectId);
            Assert.AreEqual(nameof(TapInteractionEventArgs), sync.EventType);
            Assert.AreEqual(1.5f, System.BitConverter.ToSingle(sync.Data, 0));

            var args = PhotonEventSerializer.FromSyncData(sync);
            Assert.IsInstanceOf<GenericInteractionEventArgs>(args);
            Assert.AreEqual(tap.SubjectId, args.SubjectId);
        }

        [Test]
        public void Deserialize_ShortPayload_ReturnsNull()
        {
            Assert.IsNull(PhotonEventSerializer.DeserializeEventSyncData(new object[] { "a" }));
        }
    }

    [TestFixture]
    public class ApexSessionTests
    {
        private class FakeClient : Apex.IApexClient
        {
            public bool SessionInProgress;
            public int NextSessionId = 42;
            public string FakeUserId = "user-1";
            public List<string> Events = new List<string>();
            public SessionData LastSessionData;
            public List<OrgModule> Modules = new List<OrgModule>();
            public bool IsSessionInProgress => SessionInProgress;
            public int SessionId => NextSessionId;
            public string UserId { get; private set; }
            public bool LoggedIn;
            public bool IsLoggedIn => LoggedIn;
            public void ClearUser() { UserId = null; LoggedIn = false; }
            public void Login(string u, string p, System.Action<bool, string> done) { LoggedIn = true; UserId = FakeUserId; done(true, null); }
            public void LoginWithToken(string t, System.Action<bool, string> done) { LoggedIn = true; UserId = FakeUserId; done(true, null); }
            public void QuickIdLogin(string s, string u, System.Action<bool, string> done) { LoggedIn = true; UserId = FakeUserId; done(true, null); }
            public void JoinSession(string s, Extension ctx, System.Action<bool, int> done) { SessionInProgress = true; done(true, NextSessionId); }
            public void CompleteSession(SessionData d, Extension ctx, Extension res, System.Action<bool> done) { SessionInProgress = false; LastSessionData = d; done(true); }
            public void SendSimpleSessionEvent(string a, string t, Extension ctx, System.Action<bool> done) { Events.Add(a); done(true); }
            public void SendSessionEvent(TinCan.Statement st, System.Action<bool> done) => done(true);
            public void Ping(System.Action<bool> done) => done(true);
            public void GetCurrentUserModules(System.Action<bool, IReadOnlyList<OrgModule>> done) => done(true, Modules);
        }

        [Test]
        public void Session_Login_Module_Step_Lifecycle()
        {
            var go = new GameObject("apex");
            var session = go.AddComponent<Apex.ApexPlatformSession>();
            var fake = new FakeClient();
            session.Client = fake;

            bool connected = false;
            session.Connected += () => connected = true;

            Assert.IsTrue(session.LoginAsync("u", "p").Result);
            Assert.IsTrue(connected);

            Assert.IsTrue(session.ModuleStartedAsync("Training", "sc", "mod").IsCompleted);
            Assert.AreEqual("42", session.SessionId);

            var step = new StepBase { GUID = "g", Name = "s" };
            session.StepStartedAsync(step).Wait();
            session.StepCompletedAsync(step).Wait();
            Assert.Contains("step_started", fake.Events);
            Assert.Contains("step_completed", fake.Events);

            session.ModuleEndedAsync("Training", "sc", "mod", true).Wait();
            Assert.IsTrue(fake.LastSessionData.Success);
        }

        [Test]
        public void GetUserScenarios_ReflectsClientModules()
        {
            var go = new GameObject("apex-catalog");
            var session = go.AddComponent<Apex.ApexPlatformSession>();
            var fake = new FakeClient();
            fake.Modules.Add(new OrgModule
            {
                ID = 7,
                Name = "Gas Sampling",
                Description = "desc",
                IconURL = "/img.png"
            });
            session.Client = fake;

            Assert.IsTrue(session.LoginAsync("u", "p").Result);

            var catalog = session.GetUserScenarios();
            Assert.IsNotNull(catalog);
            Assert.AreEqual(1, catalog.AvailableScenarios.Count);
            Assert.AreEqual("Gas Sampling", catalog.AvailableScenarios[0].ScenarioName);
            Assert.AreEqual("7", catalog.AvailableScenarios[0].ScenarioId);
            Assert.AreEqual(1, catalog.AvailableScenarios[0].Modules.Count);
            Assert.AreEqual("Gas Sampling", catalog.AvailableScenarios[0].Modules[0].SceneToLoad);
            Assert.AreEqual("/img.png", catalog.AvailableScenarios[0].Modules[0].ImageAddress);
        }
    }
}

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class UndoAndGroupIteratorTests
    {
        [Test]
        public void UndoEntryOnComplete_UndoesActionWhenTargetStepCompletes()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var s1 = GraphTestHelpers.Node<GenericStepNode>(g, "s1");
            var s2 = GraphTestHelpers.Node<GenericStepNode>(g, "s2");
            var action = GraphTestHelpers.Node<SetGameObjectActiveStateNode>(g, "a1");
            var target = new GameObject("undo-target");
            try
            {
                action.TargetObject = target;
                action.State = false;
                action.UndoEntries = new List<UndoOnStepNodeEntry> { new UndoOnStepNodeEntry("s2") };
                GraphTestHelpers.Flow(g, start, "executes", s1, "executed");
                GraphTestHelpers.Flow(g, s1, "executes", s2, "executed");
                GraphTestHelpers.Flow(g, s1, "OnStartActions", action, "ActionLink");

                var data = new GraphParser(GameMode.Training).Parse(g);
                var it = data.GetIterator();
                it.StartIterator();

                var step2 = data.FindStepByGuid("s2");
                Assert.IsNotNull(step2);
                Assert.IsFalse(target.activeSelf, "start action should have deactivated the target");

                step2.Complete();
                Assert.IsTrue(target.activeSelf, "undo entry on step2 complete should restore the target");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AndGroupStep_CompletesOnce_AndIgnoresRepeatedChildCompletions()
        {
            var node = new AndGroupStepNode { GUID = "grp" };
            node.CompleteAll = true;
            var group = new AndGroupStep(node);
            var c1 = new StepBase();
            var c2 = new StepBase();
            group.SetGroupedSteps(new List<StepBase> { c1, c2 });

            int completedCount = 0;
            group.StepCompleted += _ => completedCount++;

            group.OnEnter();
            c1.Complete();
            Assert.AreEqual(0, completedCount, "one of two children should not complete an all-group");
            c1.Complete();
            Assert.AreEqual(0, completedCount, "a repeated child completion must not count twice");
            c2.Complete();
            Assert.AreEqual(1, completedCount, "group completes when all children complete");
            c2.Complete();
            Assert.AreEqual(1, completedCount, "group must only complete once");
        }

        [Test]
        public void CancelledIterator_IgnoresLateStepCompletions()
        {
            var g = GraphTestHelpers.NewGraph();
            var start = GraphTestHelpers.Node<StartNode>(g, "start");
            var s1 = GraphTestHelpers.Node<GenericStepNode>(g, "s1");
            var s2 = GraphTestHelpers.Node<GenericStepNode>(g, "s2");
            GraphTestHelpers.Flow(g, start, "executes", s1, "executed");
            GraphTestHelpers.Flow(g, s1, "executes", s2, "executed");

            var data = new GraphParser(GameMode.Training).Parse(g);
            var it = data.GetIterator();
            var flowCompleted = false;
            it.FlowCompleted += () => flowCompleted = true;
            it.StartIterator();

            var step1 = data.FindStepByGuid("s1");
            Assert.IsNotNull(step1);
            it.Cancel();
            Assert.IsTrue(it.Cancelled);

            step1.Complete();
            Assert.IsFalse(flowCompleted, "cancelled iterator must not raise FlowCompleted");
            Assert.Contains(step1, it.CurrentSteps, "cancelled iterator must not advance");
        }
    }
}
