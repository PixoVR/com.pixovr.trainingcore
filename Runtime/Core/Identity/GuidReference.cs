using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Identity
{
    /// <summary>
    /// Serializable cross-object reference: stores the target's guid bytes; resolves to the
    /// live GameObject through <see cref="GuidRegistry"/> at runtime.
    /// </summary>
    [Serializable]
    public class GuidReference : ISerializationCallbackReceiver, IEquatable<GuidReference>
    {
        /// <summary>Raised when the referenced object registers after this reference resolved it.</summary>
        public event Action<GameObject> OnTargetAvailable;

        /// <summary>Raised when the referenced object unregisters.</summary>
        public event Action OnTargetRemoved;

        [SerializeField]
        private byte[] serializedGuid;

        [NonSerialized]
        private GameObject gameObject;

        private Guid guid;

        /// <summary>The referenced guid (Guid.Empty when unset).</summary>
        public Guid Guid
        {
            get
            {
                if (guid == Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
                    guid = new Guid(serializedGuid);
                return guid;
            }
            set
            {
                guid = value;
                serializedGuid = value == Guid.Empty ? null : value.ToByteArray();
            }
        }

        /// <summary>The resolved GameObject, or null while unresolved.</summary>
        public GameObject GameObject
        {
            get
            {
                if (gameObject == null && Guid != Guid.Empty)
                    gameObject = GuidRegistry.Resolve(Guid, OnAdded, OnRemoved);
                return gameObject;
            }
        }

        /// <summary>Empty reference.</summary>
        public GuidReference() { }

        /// <summary>Reference pointing at an object (reads its <see cref="GuidComponent"/>).</summary>
        public GuidReference(GameObject target)
        {
            var gc = target != null ? target.GetComponent<GuidComponent>() : null;
            if (gc != null)
            {
                Guid = gc.GetGuid();
                gameObject = target;
            }
        }

        /// <summary>The component of type <typeparamref name="T"/> on the resolved object, if any.</summary>
        public T GetComponent<T>() where T : Component => GameObject == null ? null : GameObject.GetComponent<T>();

        public void OnBeforeSerialize()
        {
            if (guid != Guid.Empty)
                serializedGuid = guid.ToByteArray();
        }

        public void OnAfterDeserialize()
        {
            guid = Guid.Empty;
            gameObject = null;
            if (serializedGuid != null && serializedGuid.Length == 16)
                guid = new Guid(serializedGuid);
        }

        private void OnAdded(GameObject go)
        {
            gameObject = go;
            OnTargetAvailable?.Invoke(go);
        }

        private void OnRemoved()
        {
            gameObject = null;
            OnTargetRemoved?.Invoke();
        }

        /// <summary>Two references are equal when their guids match.</summary>
        public bool Equals(GuidReference other)
        {
            if (other is null) return false;
            return Guid == other.Guid;
        }

        public override bool Equals(object obj) => obj is GuidReference other && Equals(other);

        public override int GetHashCode() => Guid.GetHashCode();

        public override string ToString() => Guid.ToString();
    }
}
