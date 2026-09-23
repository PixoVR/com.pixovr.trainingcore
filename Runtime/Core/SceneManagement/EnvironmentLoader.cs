using System;
using System.Collections;
using System.Collections.Generic;
using PixoVR.TrainingCore.Multiuser;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace PixoVR.TrainingCore.SceneManagement
{
    /// <summary>Loads the scenario environment scene/prefab via Addressables.</summary>
    public class EnvironmentLoader : MonoBehaviour
    {
        /// <summary>Environment used on desktop/editor (serialized name kept for migration parity).</summary>
        public AssetReference DesktopEnvrionment;

        /// <summary>Environment used on Android builds.</summary>
        public AssetReference AndroidEnvrionment;

        /// <summary>Which environment reference to load when running in the Editor (serialized name kept for migration parity; serialized values match the Luminous enum).</summary>
        public enum EditorEnvrionmentOverride { Desktop, Android }

        /// <summary>Which environment reference to load when running in the Editor.</summary>
        [Tooltip("Which environment reference to load when running in the Editor.")]
        public EditorEnvrionmentOverride EditorEnvrionmentToLoad = EditorEnvrionmentOverride.Desktop;

        /// <summary>Load the environment automatically in Start (Luminous parity).</summary>
        [Tooltip("Load the environment automatically in Start.")]
        public bool LoadOnStart = true;

        /// <summary>Invoked when the environment finished loading (fires on failure too).</summary>
        public Action LoadingDone;

        /// <summary>Instantiated environment root (prefab path).</summary>
        public GameObject EnvironmentObject { get; private set; }

        /// <summary>Loaded environment scene (scene-asset path).</summary>
        public SceneInstance? EnvironmentScene { get; private set; }

        /// <summary>Whether the environment is loaded, either as a prefab instance or an additive scene.</summary>
        public bool IsLoaded => EnvironmentObject != null || EnvironmentScene.HasValue;

        /// <summary>Platform-appropriate environment reference (scene or prefab asset).</summary>
        public AssetReference CurrentEnvironment =>
#if UNITY_EDITOR
            EditorEnvrionmentToLoad == EditorEnvrionmentOverride.Android ? AndroidEnvrionment : DesktopEnvrionment;
#elif UNITY_ANDROID
            AndroidEnvrionment ?? DesktopEnvrionment;
#else
            DesktopEnvrionment;
#endif

        private bool isLoading;
        private AssetReference loadedEnvironment;

        private void OnEnable() => SceneManager.sceneUnloaded += OnSceneUnloaded;

        private void OnDisable() => SceneManager.sceneUnloaded -= OnSceneUnloaded;

        private void OnSceneUnloaded(Scene scene)
        {
            // The environment scene can be unloaded externally (e.g. SceneLoading's
            // non-additive load). Clear the tracked instance so IsLoaded/LoadEnvironment
            // stay truthful; UnLoadScene() completes cleanly and releases the handle
            // when the scene is already unloaded (SceneProvider.UnloadSceneOp.Execute).
            if (EnvironmentScene.HasValue && scene == EnvironmentScene.Value.Scene)
            {
                EnvironmentScene = null;
                loadedEnvironment?.UnLoadScene();
                loadedEnvironment = null;
            }
        }

        private void Start()
        {
            if (LoadOnStart) LoadEnvironment();
        }

        /// <summary>Load <see cref="CurrentEnvironment"/>: scene assets load additively and become the active scene, prefab assets instantiate under this transform. <see cref="LoadingDone"/> always fires. Safe to call while a load is in flight — subscribers still get the event.</summary>
        public void LoadEnvironment()
        {
            if (IsLoaded)
            {
                LoadingDone?.Invoke();
                return;
            }
            if (isLoading)
                return;
            var reference = CurrentEnvironment;
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                LoadingDone?.Invoke();
                return;
            }
            isLoading = true;
            Addressables.LoadResourceLocationsAsync(reference.RuntimeKey).Completed += locHandle =>
            {
                var locations = locHandle.Status == AsyncOperationStatus.Succeeded ? locHandle.Result : null;
                var exception = locHandle.OperationException;
                Addressables.Release(locHandle);
                if (locations == null || locations.Count == 0)
                {
                    Log.Error($"EnvironmentLoader: no Addressables location for environment '{reference.RuntimeKey}' (is Addressables player content built for this platform?) {exception}", LogCategory.Scene);
                    isLoading = false;
                    LoadingDone?.Invoke();
                    return;
                }
                bool isScene = locations[0].ResourceType == typeof(SceneInstance);
                if (isScene)
                {
                    reference.LoadSceneAsync(LoadSceneMode.Additive).Completed += h =>
                    {
                        if (h.Status == AsyncOperationStatus.Succeeded)
                        {
                            loadedEnvironment = reference;
                            EnvironmentScene = h.Result;
                            SceneManager.SetActiveScene(h.Result.Scene);
                        }
                        else
                        {
                            Log.Error($"EnvironmentLoader: failed to load environment scene '{reference.RuntimeKey}': {h.OperationException}", LogCategory.Scene);
                        }
                        isLoading = false;
                        LoadingDone?.Invoke();
                    };
                }
                else
                {
                    reference.InstantiateAsync(transform).Completed += h =>
                    {
                        if (h.Status == AsyncOperationStatus.Succeeded)
                        {
                            loadedEnvironment = reference;
                            EnvironmentObject = h.Result;
                        }
                        else
                        {
                            Log.Error($"EnvironmentLoader: failed to instantiate environment '{reference.RuntimeKey}': {h.OperationException}", LogCategory.Scene);
                        }
                        isLoading = false;
                        LoadingDone?.Invoke();
                    };
                }
            };
        }

        /// <summary>Release the loaded environment (scene or prefab instance).</summary>
        public void UnloadEnvironment()
        {
            if (EnvironmentScene.HasValue)
            {
                // Clear first so the sceneUnloaded callback fired by UnLoadScene() doesn't re-enter.
                EnvironmentScene = null;
                loadedEnvironment?.UnLoadScene();
                loadedEnvironment = null;
            }
            if (EnvironmentObject != null)
            {
                loadedEnvironment?.ReleaseInstance(EnvironmentObject);
                loadedEnvironment = null;
                EnvironmentObject = null;
            }
        }
    }
}
