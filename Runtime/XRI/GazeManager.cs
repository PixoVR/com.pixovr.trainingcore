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

        private GazeTarget currentTarget;

        private void Update()
        {
            GazeTarget next = null;
            if (TryGaze(out var hit))
                next = hit.collider.GetComponentInParent<GazeTarget>();
            if (next == currentTarget)
                return;
            currentTarget?.OnGazeExit();
            currentTarget = next;
            currentTarget?.OnGazeEnter();
        }

        /// <summary>Cast a gaze ray; returns the first unblocked hit.</summary>
        public virtual bool TryGaze(out RaycastHit hit)
        {
            var origin = Origin != null ? Origin : (Camera.main != null ? Camera.main.transform : transform);
            var originPosition = origin.position;
            var direction = origin.forward;
            if (EyeTracker is IGazeSource gaze && gaze.ActiveAndEnabled)
            {
                originPosition = gaze.GazeOrigin;
                direction = gaze.GazeDirection;
            }
            if (Physics.SphereCast(originPosition, GazeSize, direction, out hit, GazeDistance))
            {
                if ((BlockingLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
                    return true;
            }
            hit = default;
            return false;
        }
    }

    /// <summary>Eye-tracking providers implement this to supply the gaze ray.</summary>
    public interface IGazeSource
    {
        /// <summary>Gaze ray origin in world space.</summary>
        Vector3 GazeOrigin { get; }

        /// <summary>Gaze ray direction in world space.</summary>
        Vector3 GazeDirection { get; }

        /// <summary>Whether the tracker is currently producing valid data.</summary>
        bool ActiveAndEnabled { get; }
    }
}
