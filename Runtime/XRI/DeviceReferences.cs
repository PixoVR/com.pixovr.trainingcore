using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>Scene registry of controller + headset references (replaces DeviceReferences).</summary>
    public class DeviceReferences : Utility.SingletonBehaviour<DeviceReferences>
    {
        /// <summary>Right-hand controller.</summary>
        public XRBaseController RightController;

        /// <summary>Left-hand controller.</summary>
        public XRBaseController LeftController;

        /// <summary>Headset camera.</summary>
        public Camera Headset;
    }
}
