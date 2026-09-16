using System;
using System.Collections;
using System.Collections.Generic;
using PixoVR.TrainingCore.Multiuser;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace PixoVR.TrainingCore.SceneManagement
{
    /// <summary>Static helpers mapping step numbers to scenes (wave 2 supplies the real flow data).</summary>
    public static class SceneFlow
    {
        /// <summary>Registered scene names in flow order.</summary>
        public static readonly List<string> Scenes = new List<string>();

        /// <summary>Fired before a flow-driven scene change.</summary>
        public static Action<string> OnSceneChanging;

        /// <summary>Resolve the scene a step number belongs to, or null.</summary>
        public static string GetSceneForStep(int stepNumber) => null;
    }

    /// <summary>
    /// Scene loading facade: single-point entry used by steps/graphs and the lobby,
    /// coordinating room <see cref="Room.SceneNameProperty"/> in multiuser sessions.
    /// </summary>
    public class SceneLoader : SingletonBehaviour<SceneLoader>
    {
        /// <summary>Scene to load when <see cref="LoadConfiguredScene"/> is called.</summary>
        public string SceneName;

        /// <summary>Load <see cref="SceneName"/> additively instead of replacing the active scene.</summary>
        public bool AdditiveScene;

        /// <summary>Fired when a scene load starts.</summary>
        public static Action<string> OnLoadStarted;
        /// <summary>Fired when a scene finished loading.</summary>
        public static Action<string> OnLoadCompleted;

        /// <summary>Load the configured <see cref="SceneName"/>.</summary>
        public void LoadScene() => LoadScene(SceneName);

        /// <summary>Load the configured <see cref="SceneName"/>.</summary>
        public void LoadConfiguredScene() => LoadScene(SceneName);

        /// <summary>Load a scene asynchronously.</summary>
        public void LoadScene(string sceneName)
        {
            OnLoadStarted?.Invoke(sceneName);
            StartCoroutine(LoadCoroutine(sceneName));
        }

        /// <summary>Load the scene stored in the current room's <see cref="Room.SceneNameProperty"/>.</summary>
        public void LoadRoomScene()
        {
            var room = NetworkManager.InstanceExists ? NetworkManager.Instance.CurrentRoom : null;
            var scene = room?.GetProperty<string>(Room.SceneNameProperty);
            if (!string.IsNullOrEmpty(scene))
                LoadScene(scene);
        }

        private IEnumerator LoadCoroutine(string sceneName)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName,
                AdditiveScene ? LoadSceneMode.Additive : LoadSceneMode.Single);
            while (op != null && !op.isDone)
                yield return null;
            OnLoadCompleted?.Invoke(sceneName);
        }
    }

}
