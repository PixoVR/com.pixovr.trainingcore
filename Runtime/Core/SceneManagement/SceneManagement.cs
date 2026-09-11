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
        /// <summary>Fired when a scene load starts.</summary>
        public static Action<string> OnLoadStarted;
        /// <summary>Fired when a scene finished loading.</summary>
        public static Action<string> OnLoadCompleted;

        /// <summary>Load a scene (single mode) asynchronously.</summary>
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
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone)
                yield return null;
            OnLoadCompleted?.Invoke(sceneName);
        }
    }

    /// <summary>Loads the scenario environment scene/prefab via Addressables.</summary>
    public class EnvironmentLoader : MonoBehaviour
    {
        /// <summary>Environment asset reference.</summary>
        public AssetReference Environment;

        /// <summary>Instantiated environment root.</summary>
        public GameObject EnvironmentObject { get; private set; }

        /// <summary>Instantiate <see cref="Environment"/> under this transform.</summary>
        public void LoadEnvironment()
        {
            if (Environment == null || !Environment.RuntimeKeyIsValid())
                return;
            Environment.InstantiateAsync(transform).Completed += handle =>
            {
                EnvironmentObject = handle.Result;
            };
        }

        /// <summary>Release the loaded environment.</summary>
        public void UnloadEnvironment()
        {
            if (EnvironmentObject != null)
            {
                Environment.ReleaseInstance(EnvironmentObject);
                EnvironmentObject = null;
            }
        }
    }
}
