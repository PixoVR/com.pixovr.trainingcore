using NUnit.Framework;
using PixoVR.TrainingCore.Commands;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class CommandLayerTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandHistory.Instance.Reset();
            UnityEngine.TestRuntime.IsPlaying = true;
        }

        [TearDown]
        public void TearDown()
        {
            CommandHistory.Instance.Reset();
            UnityEngine.TestRuntime.IsPlaying = false;
        }

        [Test]
        public void SetObjectPositionCommand_ExecuteUnexecuteInverse()
        {
            var go = new GameObject("t");
            go.transform.position = new Vector3(1, 2, 3);
            var cmd = new SetObjectPositionCommand(go, new Vector3(9, 9, 9));

            cmd.Execute();
            Assert.AreEqual(new Vector3(9, 9, 9), go.transform.position);
            Assert.AreEqual(new Vector3(1, 2, 3), cmd.InitialPosition);

            cmd.Unexecute();
            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.position);

            cmd.Execute();
            cmd.GetInverse().Execute();
            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.position);
        }

        private class TestBehaviour : MonoBehaviour { }

        [Test]
        public void SetComponentStateCommand_ExecuteUnexecute()
        {
            var go = new GameObject("t");
            var source = go.AddComponent<TestBehaviour>();
            var cmd = new SetComponentStateCommand(source, false);

            cmd.Execute();
            Assert.IsFalse(source.enabled);
            cmd.Unexecute();
            Assert.IsTrue(source.enabled);
        }

        [Test]
        public void SetColorCommand_ExecuteUnexecute()
        {
            var go = new GameObject("t");
            var renderer = go.AddComponent<MeshRenderer>();
            var initial = renderer.material.color;
            var cmd = new SetColorCommand(go, Color.red);

            cmd.Execute();
            Assert.AreEqual(Color.red, renderer.material.color);
            cmd.Unexecute();
            Assert.AreEqual(initial, renderer.material.color);
        }

        [Test]
        public void SpawnCommand_SpawnDestroyRespawn()
        {
            var prefab = new GameObject("prefab");
            var cmd = new SpawnCommand(prefab);

            cmd.Execute();
            var spawned = cmd.SpawnedObject;
            Assert.IsNotNull(spawned);

            cmd.Unexecute();
            Assert.IsNull(cmd.SpawnedObject);

            cmd.Execute();
            Assert.IsNotNull(cmd.SpawnedObject);
            Assert.AreNotSame(spawned, cmd.SpawnedObject);
        }

        [Test]
        public void DestroySpawnedObjectCommand_RoundTrip()
        {
            var prefab = new GameObject("prefab");
            var spawn = new SpawnCommand(prefab);
            spawn.Execute();
            var spawned = spawn.SpawnedObject;

            var destroy = new DestroySpawnedObjectCommand(spawn);
            destroy.Execute();
            Assert.IsTrue(spawned == null);

            destroy.Unexecute();
            Assert.IsNotNull(spawn.SpawnedObject);
        }

        [Test]
        public void GenericActionCommand_InvokesActionAndUndo()
        {
            int invoked = 0, undone = 0;
            var cmd = new GenericActionCommand(
                new UnityEvent(), new UnityEvent());
            cmd.FunctionalityAction.AddListener(() => invoked++);
            cmd.FunctionalityUndo.AddListener(() => undone++);

            cmd.Execute();
            Assert.AreEqual(1, invoked);
            cmd.Unexecute();
            Assert.AreEqual(1, undone);
        }

        [Test]
        public void CommandHistory_ExecuteAndRecord_UndoAndPrint()
        {
            var go = new GameObject("t");
            var cmd = new SetObjectPositionCommand(go, new Vector3(5, 5, 5));

            CommandHistory.Instance.ExecuteAndRecord(cmd);
            Assert.AreEqual(new Vector3(5, 5, 5), go.transform.position);
            Assert.AreEqual(1, CommandHistory.Instance.Executed.Count);

            var text = CommandHistory.Instance.PrintHistory();
            StringAssert.Contains("SetObjectPositionCommand", text);

            CommandHistory.Instance.Undo();
            Assert.AreEqual(Vector3.zero, go.transform.position);
        }
    }
}
