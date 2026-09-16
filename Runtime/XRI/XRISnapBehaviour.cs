using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>Marker pairing a snappable object to a named snap zone (replaces SnappableObject).</summary>
    public class XRISnapBehaviour : MonoBehaviour
    {
        /// <summary>Name of the <see cref="XRISnapZone"/> this object belongs to.</summary>
        public string SnapZone;
    }
}
