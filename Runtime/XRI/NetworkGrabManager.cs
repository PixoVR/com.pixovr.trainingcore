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
        /// <summary>True while the object is held.</summary>
        public bool IsGrabbed => grab != null && grab.IsGrabbed;

        /// <summary>Additional network views transferred with this object's ownership.</summary>
        public List<MonoBehaviour> OwnAdditionalPhotonViews = new List<MonoBehaviour>();

        /// <summary>Fired when ownership of this object changes.</summary>
        public UnityEvent OnOwnerChange;

        /// <summary>Fired on release; argument is the releasing player's actor number.</summary>
        public UnityEvent<int> OnRelease;

        /// <summary>Fired on grab; argument is the grabbing player's actor number.</summary>
        public UnityEvent<int> OnGrab;

        private XRIGrabBehaviour grab;

        private void Awake()
        {
            grab = GetComponent<XRIGrabBehaviour>();
            if (grab != null)
            {
                grab.OnGrab.AddListener(HandleGrab);
                grab.OnGrabExit.AddListener(HandleRelease);
            }
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
        }

        private void HandleRelease() => OnRelease?.Invoke(LocalActorNumber());

        /// <summary>Request local ownership of the object through the network manager.</summary>
        public virtual void RequestOwnership()
        {
            Multiuser.NetworkManager.Instance?.RequestOwnership(gameObject, OwnAdditionalPhotonViews);
            OnOwnerChange?.Invoke();
        }
    }




    /// <summary>Controls for the player to manage its interaction state.</summary>
    public interface IPlayerInteractionControls
    {
        /// <summary>Set active state of player interaction.</summary>
        void SetInteractionState(bool active);
    }



}
