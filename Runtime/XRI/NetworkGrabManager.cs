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
    /// <summary>Ownership handoff for grabbed objects, using the Multiuser abstraction (replaces NetworkGrabManagerOpenXR).</summary>
    [RequireComponent(typeof(XRIGrabBehaviour))]
    public class NetworkGrabManager : MonoBehaviour
    {
        /// <summary>True while the object is held locally or remotely.</summary>
        public bool IsGrabbed => remotelyHeld || (grab != null && grab.IsGrabbed);

        /// <summary>Actor number currently holding this object remotely (-1 when none).</summary>
        public int RemoteHolder { get; private set; } = -1;

        /// <summary>All live instances, for room-level cleanup.</summary>
        public static readonly List<NetworkGrabManager> Instances = new List<NetworkGrabManager>();

        /// <summary>Fired (static) whenever any object is grabbed locally, for network relay.</summary>
        public static event Action<NetworkGrabManager> NetworkGrabbed;

        /// <summary>Fired (static) whenever any object is released locally, for network relay.</summary>
        public static event Action<NetworkGrabManager> NetworkReleased;

        /// <summary>Additional network views transferred with this object's ownership.</summary>
        public List<MonoBehaviour> OwnAdditionalPhotonViews = new List<MonoBehaviour>();

        /// <summary>Fired when ownership of this object changes.</summary>
        public UnityEvent OnOwnerChange;

        /// <summary>Fired on release; argument is the releasing player's actor number.</summary>
        public UnityEvent<int> OnRelease;

        /// <summary>Fired on grab; argument is the grabbing player's actor number.</summary>
        public UnityEvent<int> OnGrab;

        private XRIGrabBehaviour grab;
        private bool remotelyHeld;
        private InteractionLayerMask previousLayers;
        private Rigidbody body;

        private void Awake()
        {
            grab = GetComponent<XRIGrabBehaviour>();
            body = GetComponent<Rigidbody>();
            if (grab != null)
            {
                grab.OnGrab.AddListener(HandleGrab);
                grab.OnGrabExit.AddListener(HandleRelease);
            }
        }

        private void OnEnable() => Instances.Add(this);

        private void OnDisable() => Instances.Remove(this);

        /// <summary>Apply/clear remote-held state: kinematic + interaction-layer suppression while held.</summary>
        public void SetRemotelyHeld(bool held, int holderActor = -1)
        {
            if (held == remotelyHeld)
                return;
            remotelyHeld = held;
            if (held)
            {
                RemoteHolder = holderActor;
                if (body != null)
                    body.isKinematic = true;
                if (grab != null)
                {
                    previousLayers = grab.interactionLayers;
                    grab.interactionLayers = new InteractionLayerMask();
                }
            }
            else
            {
                RemoteHolder = -1;
                if (grab != null)
                    grab.interactionLayers = previousLayers;
                if (body != null && (grab == null || !grab.IsSnapped))
                    body.isKinematic = false;
            }
        }

        /// <summary>Clear a remote hold attributed to <paramref name="actor"/> (owner change / player left).</summary>
        public void ClearRemoteHoldFor(int actor)
        {
            if (remotelyHeld && (actor < 0 || RemoteHolder == actor))
                SetRemotelyHeld(false);
        }

        private static int LocalActorNumber()
        {
            return Multiuser.NetworkManager.Instance?.CurrentRoom?.GetLocalPlayer?.Id ?? -1;
        }

        private void HandleGrab()
        {
            if (Multiuser.NetworkManager.Instance != null && Multiuser.NetworkManager.Instance.InRoom)
                RequestOwnership();
            OnGrab?.Invoke(LocalActorNumber());
            NetworkGrabbed?.Invoke(this);
        }

        private void HandleRelease()
        {
            OnRelease?.Invoke(LocalActorNumber());
            NetworkReleased?.Invoke(this);
        }

        /// <summary>Request local ownership of the object through the network manager.</summary>
        public virtual void RequestOwnership()
        {
            Multiuser.NetworkManager.Instance?.RequestOwnership(gameObject, OwnAdditionalPhotonViews);
            OnOwnerChange?.Invoke();
            ClearRemoteHoldFor(-1);
        }
    }




    /// <summary>Controls for the player to manage its interaction state.</summary>
    public interface IPlayerInteractionControls
    {
        /// <summary>Set active state of player interaction.</summary>
        void SetInteractionState(bool active);
    }



}
