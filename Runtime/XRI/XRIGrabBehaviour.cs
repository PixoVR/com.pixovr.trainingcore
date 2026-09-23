using System;
using System.Collections;
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
    /// <summary>Axis selector for valve rotation.</summary>
    public enum Axis { x, y, z }

    /// <summary>Float UnityEvent used by valve/snap components.</summary>
    [Serializable]
    public class UnityFloatEvent : UnityEvent<float> { }

    /// <summary>Teleport spreading pattern for multiuser spread offsets.</summary>
    public enum TeleportSpreadingType { Circular, Linear }

    /// <summary>
    /// <see cref="IGrabBehaviour"/> over <see cref="XRGrabInteractable"/>.
    /// Replaces GrabbableOpenXR/TwoHandedGrabbableOpenXR (two-handed via <see cref="TwoHanded"/>).
    /// Snap-zone support is trigger-based (Luminous parity), see <see cref="XRISnapZone"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable), typeof(Tappable))]
    public class XRIGrabBehaviour : XRGrabInteractable, IGrabBehaviour
    {
        /// <summary>Keep the grab position where the hand attached instead of the attach point.</summary>
        public bool PreciseGrab = true;

        /// <summary>Require two interactor selections to hold the object (two-handed grab).</summary>
        public bool TwoHanded;

        /// <summary>Parent the object had before grabbing.</summary>
        [NonSerialized]
        public Transform OriginalParent;

        /// <summary>Scale the object had before grabbing.</summary>
        [NonSerialized]
        public Vector3 OriginalScale;

        /// <summary>Attach transform position before grabbing.</summary>
        [NonSerialized]
        public Vector3 OriginalAttachTransformPosition;

        /// <summary>Attach transform rotation before grabbing.</summary>
        [NonSerialized]
        public Quaternion OriginalAttachTransformRotation;

        /// <summary>Snap zone this object is currently held by, if any.</summary>
        [NonSerialized]
        public XRISnapZone CurrentSnapZone;

        /// <summary>True while the object sits in a snap zone.</summary>
        public bool IsSnapped => CurrentSnapZone != null;

        /// <summary>Audio played on grab.</summary>
        public AudioSource Audio;

        /// <summary>Fired when this object is snapped into a zone.</summary>
        public UnityEvent OnSnapped;

        /// <summary>Fired while a snap is in progress.</summary>
        public UnityEvent OnSnapping;

        /// <summary>Fired while an unsnap is in progress.</summary>
        public UnityEvent OnUnsnapping;

        /// <summary>Fired when this object leaves a zone.</summary>
        public UnityEvent OnUnsnapped;

        /// <summary>Controller currently holding the object.</summary>
        [NonSerialized]
        public XRBaseControllerInteractor CurrentController;

        /// <summary>Fired on grab.</summary>
        public UnityEvent OnGrab;

        /// <summary>Fired on release.</summary>
        public UnityEvent OnGrabExit;

        /// <summary>True while the object is selected.</summary>
        public bool IsGrabbed => isSelected;

        private readonly List<XRISnapZone> possibleSnapZones = new List<XRISnapZone>();
        private XRBaseControllerInteractor lastController;
        private Grabbable grabbable;
        private Tappable tappable;
        private Usable usable;
        private Rigidbody grabbableRigidbody;
        private bool selected;
        private bool storedGravity = true;
        private bool storedKinematic;
        private bool hasStored;

        /// <summary>See the interface/base contract.</summary>
        protected override void Awake()
        {
            OriginalParent = transform.parent;
            OriginalScale = transform.localScale;
            TryGetComponent(out Audio);
            grabbableRigidbody = GetComponent<Rigidbody>();

            if (attachTransform == null || attachTransform == transform)
            {
                var attachPoint = new GameObject("Auto Generated Attach Point");
                attachPoint.transform.parent = transform;
                attachPoint.transform.localPosition = Vector3.zero;
                attachPoint.transform.localRotation = Quaternion.identity;
                attachTransform = attachPoint.transform;
            }

            OriginalAttachTransformPosition = attachTransform.localPosition;
            OriginalAttachTransformRotation = attachTransform.localRotation;

            OnSnapping.AddListener(StoreRigidData);
            OnSnapping.AddListener(ForceHoverExit);
            usable = GetComponent<Usable>();
            if (usable != null)
            {
                activated.AddListener(_ => usable.OnStartUsing());
                deactivated.AddListener(_ => usable.OnStopUsing());
            }
            base.Awake();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            grabbable = GetComponent<Grabbable>();
            tappable = GetComponent<Tappable>();
        }

        private void StoreRigidData()
        {
            if (grabbableRigidbody == null)
                return;
            storedGravity = grabbableRigidbody.useGravity;
            storedKinematic = false;
            hasStored = true;
        }

        private void ForceHoverExit()
        {
            foreach (var snapZone in possibleSnapZones)
                snapZone.OnHoverExit?.Invoke();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            tappable?.OnStartTap();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            tappable?.OnEndTap();
        }

        private IEnumerator StartTracking(SelectEnterEventArgs args)
        {
            var interactorTransform = (args.interactorObject as Component)?.transform;
            if (interactorTransform == null)
                yield break;

            OnGrab?.Invoke();

            while (CurrentSnapZone != null && CurrentSnapZone.CheckWithinRange(interactorTransform.position) && selected)
                yield return null;

            if (!selected || CurrentSnapZone == null)
                yield break;

            OnSelectEntering(args);
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            selected = true;

            if (CurrentSnapZone != null)
            {
                var interactorTransform = (args.interactorObject as Component)?.transform;
                if (CurrentSnapZone.HasDetachRange && interactorTransform != null && CurrentSnapZone.CheckWithinRange(interactorTransform.position))
                {
                    StartCoroutine(StartTracking(args));
                    return;
                }

                CurrentSnapZone.Unsnap(this);
                CurrentSnapZone = null;
            }

            var interactorT = (args.interactorObject as Component)?.transform;
            if (PreciseGrab && interactorT != null)
            {
                attachTransform.rotation = interactorT.rotation;
                attachTransform.position = interactorT.position;
            }

            CurrentController = args.interactorObject as XRBaseControllerInteractor;
            base.OnSelectEntering(args);
            OnGrab?.Invoke();
            if (Audio != null)
                Audio.Play();
        }

        /// <summary>
        /// XRI 3 records <c>transform.parent</c> in <see cref="XRGrabInteractable.Grab"/> and
        /// restores it on drop. If we're still parented under a snap zone, reparent to
        /// <see cref="OriginalParent"/> first so a dropped object is never re-parented
        /// into the zone it was pulled from.
        /// </summary>
        protected override void Grab()
        {
            if (transform.parent != null && transform.parent != OriginalParent)
                transform.SetParent(OriginalParent, true);
            base.Grab();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            grabbable?.OnGrabbedObjectEvent();
            XRIDiagnostics.Log($"Grab '{name}' by '{args.interactorObject}' pos={transform.position} parent={(transform.parent != null ? transform.parent.name : "null")} kinematic={(grabbableRigidbody != null && grabbableRigidbody.isKinematic)} gravity={(grabbableRigidbody != null && grabbableRigidbody.useGravity)} snapZone={(CurrentSnapZone != null ? CurrentSnapZone.name : "null")} activeInHierarchy={gameObject.activeInHierarchy}", this);
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            selected = false;

            XRIDiagnostics.Log($"Release '{name}' by '{args.interactorObject}' pos={transform.position} parent={(transform.parent != null ? transform.parent.name : "null")} kinematic={(grabbableRigidbody != null && grabbableRigidbody.isKinematic)} gravity={(grabbableRigidbody != null && grabbableRigidbody.useGravity)} snapZone={(CurrentSnapZone != null ? CurrentSnapZone.name : "null")} activeInHierarchy={gameObject.activeInHierarchy}", this);

            base.OnSelectExited(args);

            if (PreciseGrab)
            {
                attachTransform.localPosition = OriginalAttachTransformPosition;
                attachTransform.localRotation = OriginalAttachTransformRotation;
            }

            if (Audio != null)
                Audio.Stop();

            OnGrabExit?.Invoke();

            if (IsSnapped && grabbableRigidbody != null)
            {
                grabbableRigidbody.isKinematic = true;
            }
            else if (!IsSnapped && grabbableRigidbody != null)
            {
                if (hasStored)
                {
                    grabbableRigidbody.useGravity = storedGravity;
                    grabbableRigidbody.isKinematic = storedKinematic;
                    hasStored = false;
                }
                else if (trackPosition && trackRotation)
                {
                    grabbableRigidbody.useGravity = true;
                    grabbableRigidbody.isKinematic = false;
                }

                possibleSnapZones.RemoveAll(zone => zone == null);
                foreach (var snapZone in possibleSnapZones.Where(snapZone => !snapZone.HasSnappedObject))
                {
                    CurrentSnapZone = snapZone;
                    snapZone.Snap(this);
                    CurrentController = null;
                    return;
                }
            }

            CurrentController = null;
        }

        /// <summary>Adds a possible snap zone to the tracked list.</summary>
        public void AddPossibleSnapZone(XRISnapZone snapZone)
        {
            if (!possibleSnapZones.Contains(snapZone))
                possibleSnapZones.Add(snapZone);
        }

        /// <summary>Removes a snap zone from the tracked list.</summary>
        public void RemovePossibleSnapZone(XRISnapZone snapZone)
        {
            possibleSnapZones.RemoveAll(zone => zone == snapZone);
        }

        /// <summary>Sets the current snap zone (snapping without direct user input).</summary>
        public void SetCurrentSnapZone(XRISnapZone snapZone)
        {
            CurrentSnapZone = snapZone;
        }

        /// <summary>Force the object out of the grabber's hand.</summary>
        public virtual void ForceUngrab()
        {
            if (interactionManager == null)
                return;
            foreach (var interactor in interactorsSelecting.ToList())
                interactionManager.SelectExit(interactor, this);
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnDisable()
        {
            if (lastController != null)
            {
                lastController.enabled = true;
                lastController = null;
            }
            base.OnDisable();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnDestroy()
        {
            OnSnapping.RemoveListener(StoreRigidData);
            OnSnapping.RemoveListener(ForceHoverExit);
            base.OnDestroy();
        }
    }




}

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>Freezes an XR user by disabling the interaction manager and UI event systems.</summary>
    public class OculusFreezeBehaviour : Multiuser.IFreezeBehaviour
    {
        private readonly XRInteractionManager xrInteractionManager;

        /// <summary>Create for a given interaction manager.</summary>
        public OculusFreezeBehaviour(XRInteractionManager xrIntMan)
        {
            xrInteractionManager = xrIntMan;
        }

        /// <inheritdoc/>
        public override void Freeze()
        {
            if (!IsJoiningUser)
            {
                System.Array.ForEach(EventSystems, x => x.enabled = false);
                if (xrInteractionManager != null)
                    xrInteractionManager.enabled = false;
            }
            Utility.FadeManager.Instance?.FadeCanvasGroup(new PixoVR.TrainingCore.Settings.FadeSettings(false, Color.black, IsJoiningUser ? 0f : 0.6f, IsJoiningUser ? 0.0f : 0.8f, true));
            Time.timeScale = 0;
        }

        /// <inheritdoc/>
        public override void Unfreeze()
        {
            System.Array.ForEach(EventSystems, x => x.enabled = true);
            if (xrInteractionManager != null)
                xrInteractionManager.enabled = true;
            Utility.FadeManager.Instance?.FadeCanvasGroup(new PixoVR.TrainingCore.Settings.FadeSettings(false, Color.black, 0.6f, 0.0f, true));
            Time.timeScale = 1;
        }
    }
}
