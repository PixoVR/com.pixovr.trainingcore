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
        private bool applyingProgrammatic;
        private Valve valve;
        private bool hasDriver;
        private Vector3 previousProjected;

        private void Awake()
        {
            valve = GetComponent<Valve>();
        }

        private void OnEnable()
        {
            if (ReinitializeOnEnable)
                SetRotation(InitialRotation, false);
        }

        private Vector3 AxisVector() =>
            RotationAxis == Axis.x ? Vector3.right : RotationAxis == Axis.y ? Vector3.up : Vector3.forward;

        private void Update()
        {
            if (frozen || applyingProgrammatic)
            {
                hasDriver = false;
                return;
            }
            var driver = FindDriver();
            if (driver == null)
            {
                hasDriver = false;
                return;
            }
            var space = transform.parent != null ? transform.parent : transform;
            var local = space.InverseTransformPoint(driver.position);
            var projected = Vector3.ProjectOnPlane(local, AxisVector());
            if (projected.sqrMagnitude < 1e-8f)
                return;
            if (!hasDriver)
            {
                previousProjected = projected;
                hasDriver = true;
                return;
            }
            float delta = Vector3.SignedAngle(previousProjected, projected, AxisVector());
            previousProjected = projected;
            if (Mathf.Abs(delta) < 1e-4f)
                return;
            SetRotation(TotalRotation + delta);
            valve?.OnValveTurnEvent(TotalRotation);
        }

        private Transform FindDriver()
        {
            foreach (var zone in ToolSnapzones)
            {
                var snapped = zone != null ? zone.CurrentSnappedObject : null;
                if (snapped != null && snapped.IsGrabbed)
                {
                    var t = GrabbingTransform(snapped);
                    if (t != null)
                        return t;
                }
            }
            foreach (var hand in HandGrabzones)
            {
                if (hand != null && hand.IsGrabbed)
                {
                    var t = GrabbingTransform(hand);
                    if (t != null)
                        return t;
                }
            }
            return null;
        }

        private static Transform GrabbingTransform(XRIGrabBehaviour grab)
        {
            var interactor = grab.interactorsSelecting.Count > 0 ? grab.interactorsSelecting[0] : null;
            if (interactor == null)
                return null;
            var attach = interactor.GetAttachTransform(grab);
            return attach != null ? attach : (interactor as Component)?.transform;
        }

        /// <inheritdoc/>
        public virtual void SetRotation(float value, bool inverse = true)
        {
            if (frozen)
                return;
            applyingProgrammatic = true;
            foreach (var clamp in Clamps)
                value = Mathf.Clamp(value, Mathf.Min(clamp.x, clamp.y), Mathf.Max(clamp.x, clamp.y));
            TotalRotation = value;
            var axis = RotationAxis == Axis.x ? Vector3.right : RotationAxis == Axis.y ? Vector3.up : Vector3.forward;
            transform.localRotation = Quaternion.AngleAxis(value, axis);
            applyingProgrammatic = false;
            OnRotationChanged?.Invoke(value);
        }

        /// <inheritdoc/>
        public virtual void SetFreeze(bool state) => frozen = state;
    }
}
