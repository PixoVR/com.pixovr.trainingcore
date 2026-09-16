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
    /// <summary>Loads the scenario environment scene/prefab via Addressables.</summary>
    public class EnvironmentLoader : MonoBehaviour
    {
        /// <summary>Environment used on desktop/editor (serialized name kept for migration parity).</summary>
        public AssetReference DesktopEnvrionment;

        /// <summary>Environment used on Android builds.</summary>
        public AssetReference AndroidEnvrionment;

        /// <summary>Optional editor-only override.</summary>
        public AssetReference EditorEnvrionmentToLoad;

        /// <summary>Invoked when the environment finished loading.</summary>
        public Action LoadingDone;

        /// <summary>Instantiated environment root.</summary>
        public GameObject EnvironmentObject { get; private set; }

        /// <summary>Platform-appropriate environment reference.</summary>
        public AssetReference CurrentEnvironment =>
#if UNITY_EDITOR
            EditorEnvrionmentToLoad ?? DesktopEnvrionment;
#elif UNITY_ANDROID
            AndroidEnvrionment ?? DesktopEnvrionment;
#else
            DesktopEnvrionment;
#endif

        /// <summary>Instantiate <see cref="CurrentEnvironment"/> under this transform.</summary>
        public void LoadEnvironment()
        {
            var reference = CurrentEnvironment;
            if (reference == null || !reference.RuntimeKeyIsValid())
                return;
            reference.InstantiateAsync(transform).Completed += handle =>
            {
                EnvironmentObject = handle.Result;
                LoadingDone?.Invoke();
            };
        }

        /// <summary>Release the loaded environment.</summary>
        public void UnloadEnvironment()
        {
            if (EnvironmentObject != null)
            {
                CurrentEnvironment?.ReleaseInstance(EnvironmentObject);
                EnvironmentObject = null;
                
            }
        }
    }
}
