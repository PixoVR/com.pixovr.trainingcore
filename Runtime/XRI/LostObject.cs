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
    /// <summary>Respawns an interactable when it falls out of range or is dropped (replaces LostObject).</summary>
    public class LostObject : MonoBehaviour
    {
        /// <summary>Reset when the object is further than <see cref="OutOfRangeDistance"/> below the manager.</summary>
        public bool ResetWhenOutOfRange = true;

        /// <summary>Fall distance that triggers a reset.</summary>
        public float OutOfRangeDistance = 2f;

        /// <summary>Reset when the object is dropped on the ground.</summary>
        public bool ResetWhenDropped;

        /// <summary>Seconds after a drop before resetting.</summary>
        public float DroppedWaitTime = 5f;

        /// <summary>Time since the object was dropped.</summary>
        public float DroppedResetTimer;

        /// <summary>True while the object is held.</summary>
        public bool IsHeld => GrabInteractable != null && GrabInteractable.IsGrabbed;

        /// <summary>The grab interactable this object wraps.</summary>
        public XRIGrabBehaviour GrabInteractable;

        /// <summary>Track a partnered snap zone for reset destination.</summary>
        public bool UsePartneredSnapZone;

        /// <summary>Snap zone the object resets into.</summary>
        public XRISnapZone PartneredSnapZone;

        /// <summary>True while the object is snapped into a zone.</summary>
        public bool IsSanpped => GrabInteractable != null && GrabInteractable.IsSnapped;

        /// <summary>True once the object has moved.</summary>
        [NonSerialized]
        public bool HasMoved;

        /// <summary>Restore kinematic state on reset.</summary>
        public bool IsKinematicOnReset;

        /// <summary>True while a reset is running.</summary>
        public bool IsResetting { get; private set; }

        /// <summary>Velocity captured for reset.</summary>
        [NonSerialized]
        public Vector3 ItemVelocity;

        /// <summary>Spawn pose captured on start.</summary>
        public Vector3 SpawnPosition { get; private set; }

        /// <summary>Spawn rotation captured on start.</summary>
        public Quaternion SpawnRotation { get; private set; }

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (GrabInteractable == null)
                GrabInteractable = GetComponent<XRIGrabBehaviour>();
            SpawnPosition = transform.position;
            SpawnRotation = transform.rotation;
            if (GrabInteractable != null)
                GrabInteractable.OnGrab.AddListener(OnGrabbed);
            LostObjectManager.Instance?.TrackObject(this);
        }

        private void OnGrabbed()
        {
            HasMoved = true;
            DroppedResetTimer = 0f;
        }

        private void OnDestroy()
        {
            if (LostObjectManager.Instance != null)
                LostObjectManager.Instance.UntrackObject(this);
        }

        /// <summary>Return the object to its spawn pose (or partnered snap zone).</summary>
        public virtual void ResetObject()
        {
            IsResetting = true;
            if (GrabInteractable != null && GrabInteractable.IsGrabbed)
                GrabInteractable.ForceUngrab();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                if (IsKinematicOnReset)
                    body.isKinematic = true;
            }
            var target = UsePartneredSnapZone && PartneredSnapZone != null
                ? PartneredSnapZone.transform
                : null;
            if (target != null)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
                PartneredSnapZone.Snap(gameObject);
            }
            else
            {
                transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);
            }
            HasMoved = false;
            DroppedResetTimer = 0f;
            IsResetting = false;
        }
    }
}
