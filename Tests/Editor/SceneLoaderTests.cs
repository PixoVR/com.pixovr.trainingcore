using System;
using System.Reflection;
using NUnit.Framework;
using PixoVR.TrainingCore.SceneManagement;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class SceneLoaderTests
    {
        [Test]
        public void SceneLoader_IsPlainMonoBehaviour()
        {
            Assert.AreEqual(typeof(MonoBehaviour), typeof(SceneLoader).BaseType);
        }

        [Test]
        public void SceneLoader_KeepsSerializedFieldContract()
        {
            var t = typeof(SceneLoader);
            foreach (var name in new[] { "LoadOnStart", "AdditiveScene", "SceneName" })
            {
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(f, $"missing serialized field {name}");
            }
            Assert.AreEqual(typeof(bool), t.GetField("LoadOnStart").FieldType);
            Assert.AreEqual(typeof(bool), t.GetField("AdditiveScene").FieldType);
            Assert.AreEqual(typeof(string), t.GetField("SceneName").FieldType);
        }

        [Test]
        public void SceneLoading_ExposesLoadLifecycleEvents()
        {
            Assert.IsNotNull(typeof(SceneLoading).GetEvent("OnLoadStarted"));
            Assert.IsNotNull(typeof(SceneLoading).GetEvent("OnLoadCompleted"));
        }
    }
}
