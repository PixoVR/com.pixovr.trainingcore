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
    /// <summary>Gaze raycaster used by gaze steps (replaces GazeManager).</summary>
    public class GazeManager : Utility.SingletonBehaviour<GazeManager>
    {
        /// <summary>Radius of the gaze cylinder.</summary>
        public float GazeSize = 1f;

        /// <summary>Max gaze distance.</summary>
        public float GazeDistance = 50f;

        /// <summary>Wire-disc divisions used by the gizmo.</summary>
        public float GizmoDivisions = 10f;

        /// <summary>Origin of the gaze ray (headset).</summary>
        public Transform Origin;

        /// <summary>Layers that block gaze.</summary>
        public LayerMask BlockingLayers;

        /// <summary>Optional eye tracker providing the gaze ray.</summary>
        public MonoBehaviour EyeTracker;

        /// <summary>Cast a gaze ray; returns the first unblocked hit.</summary>
        public virtual bool TryGaze(out RaycastHit hit)
        {
            var origin = Origin != null ? Origin : (Camera.main != null ? Camera.main.transform : transform);
            if (Physics.SphereCast(origin.position, GazeSize, origin.forward, out hit, GazeDistance))
            {
                if ((BlockingLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
                    return true;
            }
            hit = default;
            return false;
        }
    }
}
