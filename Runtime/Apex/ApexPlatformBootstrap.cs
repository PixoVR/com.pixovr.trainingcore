using System;
using System.Net.Http;
using System.Threading.Tasks;
using PixoVR.Apex;
using PixoVR.Apex.XAPI;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Platform;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Apex
{
    /// <summary>Installs an <see cref="ApexPlatformSession"/> as the active <see cref="PlatformSessionBase.Instance"/>.</summary>
    public class ApexPlatformBootstrap : MonoBehaviour
    {
        /// <summary>The session component to install.</summary>
        public ApexPlatformSession Session;

        /// <summary>Optional config asset to apply.</summary>
        public ApexCredentialsConfig Config;

        private void Awake()
        {
            var session = Session != null ? Session : GetComponent<ApexPlatformSession>();
            if (session == null)
            {
                session = gameObject.AddComponent<ApexPlatformSession>();
            }
            if (Config != null)
                session.Config = Config;
            // PlatformSessionBase.Awake sets Instance; ensure this component exists before the flow starts.
        }
    }
}
