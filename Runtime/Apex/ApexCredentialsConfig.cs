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
    /// <summary>Configuration for <see cref="ApexPlatformSession"/> — holds no secrets.</summary>
    [CreateAssetMenu(fileName = "ApexCredentialsConfig", menuName = "TrainingCore/Apex Credentials Config")]
    public class ApexCredentialsConfig : ScriptableObject
    {
        /// <summary>Apex scenario id.</summary>
        public string ScenarioId;

        /// <summary>Device serial used by QuickID login.</summary>
        public string DeviceSerialNumber;

        /// <summary>Scenario name (informational).</summary>
        public string ScenarioName;

        /// <summary>Optional login token override.</summary>
        public string Token;
    }
}
