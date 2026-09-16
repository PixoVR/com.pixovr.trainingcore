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
    /// <summary>
    /// <see cref="IValveBehaviour"/> hand wheel; replaces ValveTurnOpenXR.
    /// </summary>
    public class XRIValveBehaviour : MonoBehaviour, IValveBehaviour
    {
        /// <summary>(open,close) degree clamp pairs.</summary>
        public List<Vector2> Clamps = new List<Vector2>();

        /// <summary>Local axis the valve rotates around.</summary>
        public Axis RotationAxis = Axis.z;

        /// <summary>Rotation where the valve is fully open.</summary>
        public float Open = 0f;

        /// <summary>Rotation where the valve is fully closed.</summary>
        public float Close = 1080f;

        /// <summary>Rotation applied on startup.</summary>
        public float InitialRotation = 1080f;

        /// <summary>Snap zones that accept tools on this valve.</summary>
        public List<XRISnapZone> ToolSnapzones = new List<XRISnapZone>();

        /// <summary>Grab points on the wheel.</summary>
        public List<XRIGrabBehaviour> HandGrabzones = new List<XRIGrabBehaviour>();

        /// <summary>Fired when the rotation changes (degrees).</summary>
        public UnityFloatEvent OnRotationChanged;

        /// <summary>Current accumulated rotation in degrees.</summary>
        public float TotalRotation { get; private set; }

        /// <summary>Reapply <see cref="InitialRotation"/> whenever the component enables.</summary>
        public bool ReinitializeOnEnable = true;

        private bool frozen;

        private void OnEnable()
        {
            if (ReinitializeOnEnable)
                SetRotation(InitialRotation, false);
        }

        /// <inheritdoc/>
        public virtual void SetRotation(float value, bool inverse = true)
        {
            if (frozen)
                return;
            foreach (var clamp in Clamps)
                value = Mathf.Clamp(value, Mathf.Min(clamp.x, clamp.y), Mathf.Max(clamp.x, clamp.y));
            TotalRotation = value;
            var axis = RotationAxis == Axis.x ? Vector3.right : RotationAxis == Axis.y ? Vector3.up : Vector3.forward;
            transform.localRotation = Quaternion.AngleAxis(value, axis);
            OnRotationChanged?.Invoke(value);
        }

        /// <inheritdoc/>
        public virtual void SetFreeze(bool state) => frozen = state;
    }
}
