using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PixoVR.TrainingCore.Tests
{
    [TestFixture]
    public class GameManagerTests
    {
        [Test]
        public void GameManager_DeclaresPrivateStartCoroutine()
        {
            var m = typeof(GameManager).GetMethod("Start",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(m, "missing non-public Start");
            Assert.AreEqual(typeof(void), m.ReturnType);
        }

        [Test]
        public void GameManager_DeclaresPublicRelaunch()
        {
            var m = typeof(GameManager).GetMethod("Relaunch",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(m, "missing public Relaunch");
            Assert.AreEqual(typeof(void), m.ReturnType);
        }
    }
}
