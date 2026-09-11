using System;
using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>
    /// Persistent session context (previously LuminousPlatformPersistence). Serialized/persisted
    /// field names are kept identical to the legacy plugin so migrated data still binds.
    /// </summary>
    public class SessionContext : Utility.SingletonBehaviour<SessionContext>
    {
        /// <summary>True after platform initialisation completed.</summary>
        public bool Initialised;
        /// <summary>Admin override level (0 = off).</summary>
        public int AdminMode;
        /// <summary>Legacy bypass flag (serialized).</summary>
        public bool bypassComplete;
        /// <summary>Serialized username.</summary>
        public string username;
        /// <summary>Serialized user PIN.</summary>
        public int userPin;
        /// <summary>Display name shown to other users.</summary>
        public string displayName;
        /// <summary>True when the session is multi-user.</summary>
        public bool isMultiuser;
        /// <summary>True while inside a module run.</summary>
        public bool isInModule;
        /// <summary>True once logged in.</summary>
        public bool isLoggedIn;
        /// <summary>True before the user's first lobby visit.</summary>
        public bool firstTimeLobby = true;
        /// <summary>User could become session lead.</summary>
        public bool isPotentialLeadUser;
        /// <summary>User currently is session lead.</summary>
        public bool isCurrentLeadUser;
        /// <summary>Joined room id.</summary>
        public string RoomId;
        /// <summary>Platform session id.</summary>
        public string SessionId;
        /// <summary>Active scenario name.</summary>
        public string Scenario;
        /// <summary>Active module id.</summary>
        public string Module;
        /// <summary>Active game mode.</summary>
        public string Mode;
        /// <summary>Scheduled session time (ISO string).</summary>
        public string ScheduledTime;
        /// <summary>Scene to load when entering the module.</summary>
        public string SceneToLoad;
        /// <summary>Thumbnail URL for the session.</summary>
        public string ImageURL;

        /// <summary>Fired once the context has been populated/initialised.</summary>
        public event Action OnInitialised;

        /// <summary>Mark the context initialised and fire <see cref="OnInitialised"/>.</summary>
        public void MarkInitialised()
        {
            Initialised = true;
            OnInitialised?.Invoke();
        }

        /// <summary>Reset the fields written by portal/session traffic.</summary>
        public void ResetPortalCommsFields()
        {
            isMultiuser = false;
            firstTimeLobby = true;
            RoomId = string.Empty;
            SessionId = string.Empty;
            Scenario = string.Empty;
            Module = string.Empty;
            Mode = string.Empty;
            SceneToLoad = string.Empty;
            ImageURL = string.Empty;
            ScheduledTime = string.Empty;
        }

        /// <summary>Full reset including login state.</summary>
        public void ResetSessionInfoFields()
        {
            ResetPortalCommsFields();
            isInModule = false;
            isLoggedIn = false;
            isPotentialLeadUser = false;
            isCurrentLeadUser = false;
            AdminMode = 0;
        }
    }
}
