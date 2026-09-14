using System;
using System.Collections.Generic;
using NUnit.Framework;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Platform;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    public class EventBusTests
    {
        private class Observer : IEventObserver
        {
            public int Count;
            public void OnEvent(InteractionEventArgs args) => Count++;
        }

        [SetUp]
        public void Setup() => EventBus.Instance.Reset();

        [Test]
        public void Publish_NotifiesObserver()
        {
            var observer = new Observer();
            EventBus.Instance.Subscribe("s1", observer);
            EventBus.Instance.Publish("s1", new GenericInteractionEventArgs("s1", "e1"));
            Assert.AreEqual(1, observer.Count);
        }

        [Test]
        public void Publish_AlsoGlobal_NotifiesGlobalSubject()
        {
            var global = new Observer();
            EventBus.Instance.Subscribe(EventBus.GlobalSubjectId, global);
            EventBus.Instance.Publish("s1", new GenericInteractionEventArgs("s1", "e1"), alsoGlobal: true);
            Assert.AreEqual(1, global.Count);
        }

        [Test]
        public void Publish_RecordsHistory()
        {
            EventBus.Instance.Publish("s1", new GenericInteractionEventArgs("s1", "e1"));
            Assert.AreEqual(1, EventBus.Instance.History.Count);
        }

        [Test]
        public void Publish_UnsyncedGating()
        {
            EventBus.Instance.Publish("s1", new GenericInteractionEventArgs("s1", "e"), toNetwork: false);
            Assert.AreEqual(1, EventBus.Instance.UnsyncedEvents.Count);
            EventBus.Instance.Publish("s2", new GenericInteractionEventArgs("s2", "e"), toNetwork: true);
            Assert.AreEqual(1, EventBus.Instance.UnsyncedEvents.Count);
        }
    }

    public class CommandHistoryTests
    {
        private class TestCommand : CommandBase
        {
            public int Executions;
            public int Unexecutions;
            public TestCommand(string subjectId) : base(subjectId) { }
            public override void Execute() => Executions++;
            public override void Unexecute() => Unexecutions++;
        }

        [SetUp]
        public void Setup()
        {
            CommandHistory.Instance.Reset();
            StepCounter.InitializeTo(0);
        }

        [Test]
        public void Record_Undo_Redo()
        {
            var cmd = new TestCommand("s");
            CommandHistory.Instance.Record(cmd);
            cmd.Execute();
            CommandHistory.Instance.Undo();
            Assert.AreEqual(1, cmd.Unexecutions);
            CommandHistory.Instance.Redo();
            Assert.AreEqual(2, cmd.Executions);
        }

        [Test]
        public void UndoStep_GroupsSameStep()
        {
            StepCounter.InitializeTo(5);
            var a = new TestCommand("a");
            var b = new TestCommand("b");
            StepCounter.InitializeTo(6);
            var c = new TestCommand("c");
            CommandHistory.Instance.Record(a);
            CommandHistory.Instance.Record(b);
            CommandHistory.Instance.Record(c);
            CommandHistory.Instance.UndoStep(6); // top group only
            Assert.AreEqual(0, a.Unexecutions);
            Assert.AreEqual(0, b.Unexecutions);
            Assert.AreEqual(1, c.Unexecutions);
            CommandHistory.Instance.UndoStep(5); // then the older group
            Assert.AreEqual(1, a.Unexecutions);
            Assert.AreEqual(1, b.Unexecutions);
        }

        [Test]
        public void UseObject_MergesShorterDuration()
        {
            var go = new GameObject();
            var subject = go.AddComponent<ObservableSubject>();
            var location = new GameObject().transform;
            var s1 = new UseObjectCommand(subject, location, 1f);
            var s2 = new UseObjectCommand(subject, location, 2f);
            CommandHistory.Instance.Record(s1);
            CommandHistory.Instance.Record(s2);
            // shorter command was popped; only the longer one remains
            Assert.AreEqual(1, CommandHistory.Instance.Executed.Count);
            UnityEngine.Object.DestroyImmediate(location.gameObject);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    public class SessionContextTests
    {
        [Test]
        public void ResetSessionValues_ClearsSessionFields()
        {
            var go = new GameObject();
            var ctx = go.AddComponent<SessionContext>();
            ctx.RoomId = "r"; ctx.SessionId = "s"; ctx.isMultiuser = true;
            ctx.ResetPortalCommsFields();
            Assert.IsEmpty(ctx.RoomId);
            Assert.IsEmpty(ctx.SessionId);
            Assert.IsFalse(ctx.isMultiuser);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetAll_ClearsLoginState()
        {
            var go = new GameObject();
            var ctx = go.AddComponent<SessionContext>();
            ctx.isLoggedIn = true; ctx.AdminMode = 1;
            ctx.ResetSessionInfoFields();
            Assert.IsFalse(ctx.isLoggedIn);
            Assert.AreEqual(0, ctx.AdminMode);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    public class StepCounterTests
    {
        [Test]
        public void Increment_FromInitialize()
        {
            StepCounter.InitializeTo(3);
            Assert.AreEqual(4, StepCounter.Increment());
            Assert.AreEqual(4, StepCounter.Current);
        }
    }

    public class GuidRegistryTests
    {
        [SetUp]
        public void SetUpEditMode() => UnityEngine.TestRuntime.IsPlaying = false;

        private GuidComponent Make()
        {
            var go = new GameObject();
            var c = go.AddComponent<GuidComponent>();
            c.OnAfterDeserialize(); // ensure guid assigned in edit-mode context
            return c;
        }

        [TearDown]
        public void TearDown() => GuidRegistry.Clear();

        [Test]
        public void Add_And_IsRegistered()
        {
            var c = Make();
            Assert.IsTrue(GuidRegistry.Add(c));
            Assert.IsTrue(GuidRegistry.IsRegistered(c.GetGuid()));
        }

        [Test]
        public void Add_Duplicate_Fails()
        {
            var a = Make();
            var b = Make();
            // force same guid
            typeof(GuidComponent).GetField("guid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(b, a.GetGuid());
            GuidRegistry.Add(a);
            Assert.IsFalse(GuidRegistry.Add(b));
        }

        [Test]
        public void Resolve_Later_InvokesCallback()
        {
            var c = Make();
            var guid = c.GetGuid();
            GuidRegistry.Remove(c);
            bool added = false;
            GuidRegistry.Resolve(guid, _ => added = true, null);
            Assert.IsFalse(added);
            GuidRegistry.Add(c);
            Assert.IsTrue(added);
        }
    }
}
