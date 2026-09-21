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
    /// <see cref="ITeleportBehaviour"/> over <see cref="BaseTeleportationInteractable"/>; replaces TeleportAreaOpenXR.
    /// </summary>
    public class XRITeleportBehaviour : BaseTeleportationInteractable, ITeleportBehaviour
    {
        /// <summary>When true, teleport to <see cref="TeleportAnchorTransform"/>; otherwise to the ray hit point (area).</summary>
        public bool Anchor;

        /// <summary>Anchor used for single-user teleports.</summary>
        public Transform TeleportAnchorTransform;

        /// <summary>Anchor used for multiuser teleports.</summary>
        public Transform MultiuserTeleportAnchorTransform;

        /// <summary>Line endpoint marker for multiuser teleports.</summary>
        public Transform MultiuserTeleportLineEnd;

        /// <summary>Fired after teleporting.</summary>
        public UnityEvent OnTeleported;

        /// <summary>Fired when the teleport is undone.</summary>
        public UnityEvent OnUnexecute;

        /// <summary>Radius of the multiuser spread pattern.</summary>
        public float MultiuserSpreadRadius = 0.4f;

        /// <summary>Spread pattern used for multiuser teleports.</summary>
        public TeleportSpreadingType SpreadingType;

        private Vector3 lastPosition;
        private Quaternion lastRotation;

        /// <inheritdoc/>
        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
        {
            if (Anchor)
            {
                var anchor = GetAnchor();
                teleportRequest.destinationPosition = anchor.position;
                teleportRequest.destinationRotation = anchor.rotation;
                return true;
            }

            if (raycastHit.collider == null)
                return false;

            teleportRequest.destinationPosition = raycastHit.point;
            teleportRequest.destinationRotation = transform.rotation;
            return true;
        }

        /// <summary>Disable this teleport point.</summary>
        public virtual void DisableTeleportPoint()
        {
            enabled = false;
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        /// <inheritdoc/>
        public virtual void Teleport()
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin == null)
                return;
            lastPosition = origin.transform.position;
            lastRotation = origin.transform.rotation;
            TeleportPlayer(origin.transform, GetAnchor());
            OnTeleported?.Invoke();
        }

        /// <summary>Teleport a transform to this point's anchor.</summary>
        public virtual void TeleportPlayer(Transform player) => TeleportPlayer(player, GetAnchor());

        /// <summary>Teleport a transform to an explicit anchor.</summary>
        public virtual void TeleportPlayer(Transform player, Transform anchor)
        {
            if (player == null || anchor == null)
                return;
            player.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        /// <summary>Test helper: resolve the anchor.</summary>
        public virtual Transform TeleportTest() => GetAnchor();

        /// <inheritdoc/>
        public virtual void Unexecute()
        {
            OnUnexecute?.Invoke();
        }

        /// <summary>Spread offsets in a circle for <paramref name="count"/> players.</summary>
        public virtual List<Vector3> CalculateCircularSpreading(int count)
        {
            var offsets = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, count);
                offsets.Add(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * MultiuserSpreadRadius);
            }
            return offsets;
        }

        /// <summary>Spread offsets in a line for <paramref name="count"/> players.</summary>
        public virtual List<Vector3> CalculateLinearSpreading(int count)
        {
            var offsets = new List<Vector3>();
            for (int i = 0; i < count; i++)
                offsets.Add(new Vector3(i * MultiuserSpreadRadius - (count - 1) * MultiuserSpreadRadius * 0.5f, 0, 0));
            return offsets;
        }

        /// <summary>Spread offsets using the configured pattern.</summary>
        public List<Vector3> CalculateSpreading(int count) =>
            SpreadingType == TeleportSpreadingType.Circular
                ? CalculateCircularSpreading(count)
                : CalculateLinearSpreading(count);

        private Transform GetAnchor() =>
            Anchor && TeleportAnchorTransform != null ? TeleportAnchorTransform : transform;
    }
}
