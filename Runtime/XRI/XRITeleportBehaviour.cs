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
        private bool hasLastPose;
        private bool teleportPending;
        private int pendingFrames;
        private Teleporter teleporter;

        private Teleporter TeleporterComponent =>
            teleporter != null ? teleporter : teleporter = GetComponent<Teleporter>();

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            teleporting.AddListener(OnTeleporting);
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            teleporting.RemoveListener(OnTeleporting);
            base.OnDisable();
        }

        /// <inheritdoc/>
        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                lastPosition = origin.transform.position;
                lastRotation = origin.transform.rotation;
                hasLastPose = true;
            }
            if (Anchor)
            {
                var anchor = GetAnchor();
                teleportRequest.destinationPosition = anchor.position;
                teleportRequest.destinationRotation = anchor.rotation;
                XRIDiagnostics.Log($"Teleport '{name}': request by '{interactor}' anchor -> {anchor.position}", this);
                return true;
            }

            if (raycastHit.collider == null)
            {
                XRIDiagnostics.Log($"Teleport '{name}': request by '{interactor}' rejected (no hit)", this);
                return false;
            }

            teleportRequest.destinationPosition = raycastHit.point;
            teleportRequest.destinationRotation = transform.rotation;
            XRIDiagnostics.Log($"Teleport '{name}': request by '{interactor}' area -> {raycastHit.point}", this);
            return true;
        }

        private void OnTeleporting(TeleportingEventArgs args)
        {
            teleportPending = true;
            pendingFrames = 0;
        }

        /// <summary>Waits for the XRI body transformer to apply the queued move before firing <see cref="OnTeleported"/>.</summary>
        private void LateUpdate()
        {
            if (!teleportPending)
                return;
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin == null)
            {
                teleportPending = false;
                return;
            }
            pendingFrames++;
            if ((origin.transform.position - lastPosition).sqrMagnitude <= 1e-6f && pendingFrames < 5)
                return;
            teleportPending = false;
            TeleporterComponent?.OnObjectEntered(origin.gameObject, lastPosition, lastRotation);
            OnTeleported?.Invoke();
        }

        /// <summary>
        /// Only ray interactors may hover/select teleport interactables — the hands'
        /// XRDirectInteractor shares interaction layer bit 1 with the floor, so a grip
        /// near a snapped tool would otherwise select the teleport surface.
        /// </summary>
        public override bool IsHoverableBy(IXRHoverInteractor interactor) =>
            base.IsHoverableBy(interactor) && interactor is XRRayInteractor;

        /// <inheritdoc cref="IsHoverableBy"/>
        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && interactor is XRRayInteractor;

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
            hasLastPose = true;
            TeleportPlayer(origin.transform, GetAnchor());
            TeleporterComponent?.OnObjectEntered(origin.gameObject, lastPosition, lastRotation);
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
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null && hasLastPose)
                origin.transform.SetPositionAndRotation(lastPosition, lastRotation);
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
