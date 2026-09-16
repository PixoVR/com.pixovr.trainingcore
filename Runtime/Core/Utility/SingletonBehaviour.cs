using UnityEngine;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>MonoBehaviour singleton: lazily resolved, destroys duplicates.</summary>
    /// <typeparam name="T">Concrete component type.</typeparam>
    public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        /// <summary>The singleton instance, or null until the first instance is initialised.</summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance != null)
                        instance.SendMessage("OnSingletonInitialised", SendMessageOptions.DontRequireReceiver);
                }
                return instance;
            }
        }

        /// <summary>True once an instance exists.</summary>
        public static bool InstanceExists => instance != null;

        /// <summary>Assigns the static instance; destroys the GameObject of duplicate instances.</summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Clears the static instance when this instance is destroyed.</summary>
        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
