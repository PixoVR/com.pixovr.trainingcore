using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>Scene component holding a reference to the active <see cref="SessionContext"/> for a scene.</summary>
    public class PlatformSessionBehaviour : MonoBehaviour
    {
        /// <summary>Credentials/config driving the session.</summary>
        public ScriptableObject CredentialsConfig;

        /// <summary>Active session context.</summary>
        public SessionContext Context { get; private set; }

        /// <summary>See the interface/base contract.</summary>
        protected virtual void Awake()
        {
            Context = SessionContext.Instance;
        }
    }
}
