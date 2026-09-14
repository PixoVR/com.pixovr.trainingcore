using PixoVR.TrainingCore.Utility;
using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Identity
{
    /// <summary>
    /// Gives a GameObject a persistent 16-byte GUID (serialized as <see cref="serializedGuid"/>).
    /// Generates a guid on Awake/OnValidate; prefab assets are skipped so instances each get their own.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class GuidComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        private Guid guid = Guid.Empty;

        [SerializeField]
        private byte[] serializedGuid;

        /// <summary>True once a non-empty guid has been assigned.</summary>
        public bool IsGuidAssigned => guid != Guid.Empty;

        /// <summary>Returns the guid, rehydrating from the serialized bytes when needed.</summary>
        public Guid GetGuid()
        {
            if (guid == Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
                guid = new Guid(serializedGuid);
            return guid;
        }

        /// <summary>Assigns a new guid if none exists (no-op for prefab assets on disk) and registers it.</summary>
        public void CreateGuid()
        {
            if (serializedGuid == null || serializedGuid.Length != 16)
            {
                if (IsAssetOnDisk())
                    return;

                guid = Application.isPlaying ? RandomManager.Instance.NextGuid() : Guid.NewGuid();
                serializedGuid = guid.ToByteArray();
            }
            else if (guid == Guid.Empty)
            {
                guid = new Guid(serializedGuid);
            }

            if (guid != Guid.Empty && !GuidRegistry.Add(this))
            {
                serializedGuid = null;
                guid = Guid.Empty;
                CreateGuid();
            }
        }

        private bool IsAssetOnDisk()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return true;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
            return stage != null;
#else
            return false;
#endif
        }

        public void OnBeforeSerialize()
        {
            if (IsAssetOnDisk())
            {
                serializedGuid = null;
                guid = Guid.Empty;
            }
            else if (guid != Guid.Empty)
            {
                serializedGuid = guid.ToByteArray();
            }
        }

        public void OnAfterDeserialize()
        {
            if (serializedGuid != null && serializedGuid.Length == 16)
                guid = new Guid(serializedGuid);
        }

        private void Awake() => CreateGuid();

        private void OnValidate()
        {
            if (IsAssetOnDisk())
            {
                serializedGuid = null;
                guid = Guid.Empty;
            }
            else
            {
                CreateGuid();
            }
        }

        private void OnDestroy()
        {
            if (guid != Guid.Empty)
                GuidRegistry.Remove(this);
        }
    }
}
