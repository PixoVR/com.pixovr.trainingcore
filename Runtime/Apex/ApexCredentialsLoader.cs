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
            if (Config == null) return;
            var session = PlatformSessionBase.Instance as ApexPlatformSession;
            if (session == null) session = FindObjectOfType<ApexPlatformSession>();
            if (session != null && session.Config == null)
                session.Config = Config;
            Initialised = true;
        }
    }
}
