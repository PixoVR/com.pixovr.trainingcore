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

        /// <summary>Optional editor-only override.</summary>
        public AssetReference EditorEnvrionmentToLoad;

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
            EditorEnvrionmentToLoad ?? DesktopEnvrionment;
#elif UNITY_ANDROID
            AndroidEnvrionment ?? DesktopEnvrionment;
#else
            DesktopEnvrionment;
#endif

        /// <summary>Load <see cref="CurrentEnvironment"/>: scene assets load additively and become the active scene, prefab assets instantiate under this transform. <see cref="LoadingDone"/> always fires.</summary>
        public void LoadEnvironment()
        {
            var reference = CurrentEnvironment;
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                LoadingDone?.Invoke();
                return;
            }
            Addressables.LoadResourceLocationsAsync(reference.RuntimeKey).Completed += locHandle =>
            {
                var locations = locHandle.Result;
                bool isScene = locations != null && locations.Count > 0 &&
                               locations[0].ResourceType == typeof(SceneInstance);
                Addressables.Release(locHandle);
                if (isScene)
                {
                    reference.LoadSceneAsync(LoadSceneMode.Additive).Completed += h =>
                    {
                        if (h.Status == AsyncOperationStatus.Succeeded)
                        {
                            EnvironmentScene = h.Result;
                            SceneManager.SetActiveScene(h.Result.Scene);
                        }
                        else
                        {
                            Log.Error($"EnvironmentLoader: failed to load environment scene '{reference.RuntimeKey}': {h.OperationException}", LogCategory.Scene);
                        }
                        LoadingDone?.Invoke();
                    };
                }
                else
                {
                    reference.InstantiateAsync(transform).Completed += h =>
                    {
                        if (h.Status == AsyncOperationStatus.Succeeded)
                        {
                            EnvironmentObject = h.Result;
                        }
                        else
                        {
                            Log.Error($"EnvironmentLoader: failed to instantiate environment '{reference.RuntimeKey}': {h.OperationException}", LogCategory.Scene);
                        }
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
                CurrentEnvironment?.UnLoadScene();
                EnvironmentScene = null;
            }
            if (EnvironmentObject != null)
            {
                CurrentEnvironment?.ReleaseInstance(EnvironmentObject);
                EnvironmentObject = null;
            }
        }
    }
}
