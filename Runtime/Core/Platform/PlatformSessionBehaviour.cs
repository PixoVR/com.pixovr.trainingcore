using System;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>
    /// Scene component holding the active <see cref="SessionContext"/> (replaces the legacy
    /// LuminousPlatformPersistence behaviour). Exposes the context's persisted fields under
    /// the legacy names so migrated components keep their serialized data shape.
    /// </summary>
    public class PlatformSessionBehaviour : SingletonBehaviour<PlatformSessionBehaviour>
    {
        /// <summary>Credentials/config driving the session.</summary>
        public ScriptableObject CredentialsConfig;

        /// <summary>Active session context.</summary>
        public SessionContext Context { get; private set; }

        /// <summary>See the interface/base contract.</summary>
        protected override void Awake()
        {
            base.Awake();
            Context = SessionContext.Instance;
            PlatformSessionBase.EnsureActive();
        }

        /// <summary>Fired once the context has been populated/initialised.</summary>
        public event Action PersistenceInitialised
        {
            add => Context.OnInitialised += value;
            remove => Context.OnInitialised -= value;
        }

        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool Initialised { get => Context.Initialised; set => Context.Initialised = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public int AdminMode { get => Context.AdminMode; set => Context.AdminMode = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool bypassComplete { get => Context.bypassComplete; set => Context.bypassComplete = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string username { get => Context.username; set => Context.username = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public int userPin { get => Context.userPin; set => Context.userPin = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string displayName { get => Context.displayName; set => Context.displayName = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool isMultiuser { get => Context.isMultiuser; set => Context.isMultiuser = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool isInModule { get => Context.isInModule; set => Context.isInModule = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool isLoggedIn { get => Context.isLoggedIn; set => Context.isLoggedIn = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool firstTimeLobby { get => Context.firstTimeLobby; set => Context.firstTimeLobby = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool isPotentialLeadUser { get => Context.isPotentialLeadUser; set => Context.isPotentialLeadUser = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public bool isCurrentLeadUser { get => Context.isCurrentLeadUser; set => Context.isCurrentLeadUser = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string RoomId { get => Context.RoomId; set => Context.RoomId = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string SessionId { get => Context.SessionId; set => Context.SessionId = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string Scenario { get => Context.Scenario; set => Context.Scenario = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string Module { get => Context.Module; set => Context.Module = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string Mode { get => Context.Mode; set => Context.Mode = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string ScheduledTime { get => Context.ScheduledTime; set => Context.ScheduledTime = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string SceneToLoad { get => Context.SceneToLoad; set => Context.SceneToLoad = value; }
        /// <summary>See <see cref="SessionContext"/>.</summary>
        public string ImageURL { get => Context.ImageURL; set => Context.ImageURL = value; }

        /// <summary>Reset the fields written by portal/session traffic.</summary>
        public void ResetPortalCommsFields() => Context.ResetPortalCommsFields();

        /// <summary>Full reset including login state.</summary>
        public void ResetSessionInfoFields() => Context.ResetSessionInfoFields();
    }
}
