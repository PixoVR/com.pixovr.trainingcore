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
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
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

        /// <summary>See the interface/base contract.</summary>
        protected override void Awake()
        {
            base.Awake();
            OriginalParent = transform.parent;
            OriginalScale = transform.localScale;
            if (attachTransform != null)
            {
                OriginalAttachTransformPosition = attachTransform.localPosition;
                OriginalAttachTransformRotation = attachTransform.localRotation;
            }
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);
            CurrentController = args.interactorObject as XRBaseControllerInteractor;
            OnGrab?.Invoke();
            if (Audio != null)
                Audio.Play();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            CurrentController = null;
            OnGrabExit?.Invoke();
        }

        /// <summary>Force the object out of the grabber's hand.</summary>
        public virtual void ForceUngrab()
        {
            if (interactionManager == null)
                return;
            foreach (var interactor in interactorsSelecting.ToList())
                interactionManager.SelectExit(interactor, this);
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
