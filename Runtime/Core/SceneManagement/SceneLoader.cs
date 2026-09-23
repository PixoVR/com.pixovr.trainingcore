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
    /// App-wide scene loading. All loads are additive; a non-additive load unloads every
    /// tracked scene. Tracked scenes include those loaded by other systems (e.g.
    /// Addressables environments loaded via <see cref="EnvironmentLoader"/>), so a
    /// non-additive load leaves only the Unity-booted scene (persistence, network
    /// manager, XR rig) resident. The boot scene is recorded at startup and is never
    /// tracked or unloaded.
    /// </summary>
    public static class SceneLoading
    {
        private static string bootSceneName;

        /// <summary>Records the boot scene and starts tracking every scene load/unload.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            bootSceneName = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>Fired when a scene load starts.</summary>
        public static event Action<string> OnLoadStarted;
        /// <summary>Fired when a scene finished loading.</summary>
        public static event Action<string> OnLoadCompleted;

        private static readonly List<string> loadedScenes = new List<string>();
        private static SceneLoadRunner runner;

        /// <summary>
        /// Load a scene additively. When <paramref name="additive"/> is false, first unload every
        /// scene previously loaded through this class; the boot scene is never touched.
        /// </summary>
        public static void Load(string sceneName, bool additive = false)
        {
            OnLoadStarted?.Invoke(sceneName);
            if (runner == null)
            {
                var go = new GameObject("SceneLoadRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<SceneLoadRunner>();
            }
            runner.StartCoroutine(LoadRoutine(sceneName, additive));
        }

        /// <summary>Unload a scene previously loaded through this class.</summary>
        public static void Unload(string sceneName)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).isLoaded)
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            loadedScenes.Remove(sceneName);
        }

        private static IEnumerator LoadRoutine(string sceneName, bool additive)
        {
            var deferred = new List<string>();
            if (!additive)
            {
                foreach (var name in loadedScenes.ToArray())
                {
                    if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName(name).isLoaded)
                        continue;
                    // Unity refuses to unload the last resident scene and throws; defer it
                    // until the target scene is loaded so it can be unloaded afterwards.
                    if (SceneManager.sceneCount == 1)
                    {
                        deferred.Add(name);
                        continue;
                    }
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
                }
            }

            if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).isLoaded)
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            foreach (var name in deferred)
            {
                if (name != sceneName && UnityEngine.SceneManagement.SceneManager.GetSceneByName(name).isLoaded)
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
            }

            if (!loadedScenes.Contains(sceneName))
                loadedScenes.Add(sceneName);

            var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid())
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(loaded);

            OnLoadCompleted?.Invoke(sceneName);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                loadedScenes.Clear();
            if (scene.name == bootSceneName)
                return;
            if (!loadedScenes.Contains(scene.name))
                loadedScenes.Add(scene.name);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            loadedScenes.Remove(scene.name);
        }

        private class SceneLoadRunner : MonoBehaviour
        {
        }
    }

    /// <summary>
    /// Scene loading facade used by steps/graphs and the lobby,
    /// coordinating room <see cref="Room.SceneNameProperty"/> in multiuser sessions.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>Load <see cref="SceneName"/> in <see cref="Start"/>.</summary>
        public bool LoadOnStart;

        /// <summary>Load <see cref="SceneName"/> additively without unloading other tracked scenes.</summary>
        public bool AdditiveScene;

        /// <summary>Scene to load when <see cref="LoadConfiguredScene"/> is called.</summary>
        public string SceneName;

        private void Start()
        {
            if (LoadOnStart)
                LoadScene();
        }

        /// <summary>Load the configured <see cref="SceneName"/>.</summary>
        public void LoadScene() => LoadScene(SceneName);

        /// <summary>Load the configured <see cref="SceneName"/>.</summary>
        public void LoadConfiguredScene() => LoadScene(SceneName);

        /// <summary>Load a scene asynchronously through <see cref="SceneLoading"/>.</summary>
        public void LoadScene(string sceneName) => SceneLoading.Load(sceneName, AdditiveScene);

        /// <summary>Load the scene stored in the current room's <see cref="Room.SceneNameProperty"/>.</summary>
        public void LoadRoomScene()
        {
            var room = NetworkManager.InstanceExists ? NetworkManager.Instance.CurrentRoom : null;
            var scene = room?.GetProperty<string>(Room.SceneNameProperty);
            if (!string.IsNullOrEmpty(scene))
                LoadScene(scene);
        }
    }

}
