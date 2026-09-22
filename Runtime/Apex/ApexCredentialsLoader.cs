using PixoVR.TrainingCore.Platform;
using UnityEngine;

namespace PixoVR.TrainingCore.Apex
{
    /// <summary>
    /// Scene component replacing the legacy Luminous CredentialLoader. Keeps the legacy serialized
    /// fields so migrated scenes retain their data and hands <see cref="Config"/> to the active
    /// <see cref="ApexPlatformSession"/>.
    /// </summary>
    public class ApexCredentialsLoader : MonoBehaviour
    {
        public ApexCredentialsConfig Config;

        /// <summary>Optional project-authored scenario catalog to apply to the session.</summary>
        public ScenarioCatalog ScenarioCatalog;

        public bool Initialised;
        public string ServerBaseAddress;
        public string PhotonRealtimeAppId;
        public string PhotonChatAppId;
        public string PhotonVoiceAppId;
        public string PhotonRegion;
        public string PhotonIPAddress;
        public int PhotonPort;
        public string MachineLearningBaseAddress;
        public bool FirstTimeLobby;
        public int LoginPin;
        public string DisplayName;
        public string PhotonRoomId;
        public string PhotonSceneToLoad;

        private void Start()
        {
            var session = PlatformSessionBase.Instance as ApexPlatformSession;
            if (session == null) session = FindObjectOfType<ApexPlatformSession>();
            if (ScenarioCatalog != null && session != null && session.ScenarioCatalog == null)
                session.ApplyScenarioCatalog(ScenarioCatalog);
            if (Config == null) return;
            if (session != null && session.Config == null)
                session.Config = Config;
            Initialised = true;
        }
    }
}
