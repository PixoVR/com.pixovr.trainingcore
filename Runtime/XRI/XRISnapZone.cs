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
    /// <see cref="ISnapBehaviour"/> over <see cref="XRSocketInteractor"/>; replaces SnapZoneOpenXR.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class XRISnapZone : MonoBehaviour, ISnapBehaviour
    {
        /// <summary>Destroy the snapped object on unsnap and respawn its source prefab.</summary>
        public bool CloneOnUnsnap;

        /// <summary>Shrink the occupant to <see cref="ShrunkenSize"/> while snapped.</summary>
        public bool ResizeOnSnap;

        /// <summary>Scale applied when <see cref="ResizeOnSnap"/> is set.</summary>
        public float ShrunkenSize = 1f;

        /// <summary>The occupant must be placed by the player (no auto-snap).</summary>
        public bool MustBePlacedInSnapZone;

        /// <summary>Attach point for snapped objects.</summary>
        public Transform AttachPoint;

        /// <summary>Use the interactable's own attach transform.</summary>
        public bool UseInteractablesAttachPoint;

        /// <summary>Only accept an object whose partnered prefab id matches.</summary>
        public bool OnlyAllowPartneredPrefabObjectId;

        /// <summary>Prefab spawned when <see cref="CloneOnUnsnap"/> destroys the occupant.</summary>
        public XRISnapBehaviour PartneredPrefab;

        /// <summary>Highlight the zone while a valid object hovers.</summary>
        public bool HighlightOnHover;

        /// <summary>Material applied while hovering when <see cref="HighlightOnHover"/> is set.</summary>
        public Material HighlightMaterial;

        /// <summary>Seconds the occupancy animation takes.</summary>
        public float PlaceAnimationDuration = 0.25f;

        /// <summary>Destroy the occupant on snap.</summary>
        public bool DestroyObjectOnSnap;

        /// <summary>Unsnap the current occupant when a new object snaps in.</summary>
        public bool EmptySnapZoneOnSnap;

        /// <summary>Allow pulling the occupant out beyond <see cref="DetachRange"/>.</summary>
        public bool HasDetachRange;

        /// <summary>Distance the occupant must be pulled before detaching.</summary>
        public float DetachRange = 0.5f;

        /// <summary>Currently snapped object.</summary>
        public XRIGrabBehaviour CurrentSnappedObject;

        /// <summary>True while an object occupies the zone.</summary>
        public bool HasSnappedObject => CurrentSnappedObject != null;

        /// <summary>Fired when an object snaps in.</summary>
        public UnityEvent<GameObject> OnSnap = new UnityEvent<GameObject>();

        /// <summary>Fired when the occupant is released.</summary>
        public UnityEvent OnUnsnap;

        /// <summary>Fired while unsnapping is in progress.</summary>
        public UnityEvent<GameObject> OnUnsnapping = new UnityEvent<GameObject>();

        /// <summary>Fired when a valid object hovers.</summary>
        public UnityEvent OnHoverEnter;

        /// <summary>Fired when a hover ends.</summary>
        public UnityEvent OnHoverExit;

        /// <inheritdoc cref="ISnapBehaviour.OnDetachRangeExit"/>
        public event ISnapBehaviour.SnapEventHandler OnDetachRangeExit;

        private XRSocketInteractor socket;
        private bool detached;

        /// <summary>Underlying socket interactor.</summary>
        public XRSocketInteractor Socket => socket;

        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();
            socket.selectEntered.AddListener(OnSocketSelect);
            socket.selectExited.AddListener(OnSocketDeselect);
            socket.hoverEntered.AddListener(OnSocketHoverEnter);
            socket.hoverExited.AddListener(OnSocketHoverExit);
        }

        private void OnSocketSelect(SelectEnterEventArgs args)
        {
            var obj = (args.interactableObject as Component)?.gameObject;
            if (obj == null)
                return;
            var grab = obj.GetComponent<XRIGrabBehaviour>();
            if (EmptySnapZoneOnSnap && CurrentSnappedObject != null && CurrentSnappedObject.gameObject != obj)
                Unsnap(CurrentSnappedObject);
            CurrentSnappedObject = grab;
            if (grab != null)
                grab.CurrentSnapZone = this;
            if (ResizeOnSnap)
                obj.transform.localScale = Vector3.one * ShrunkenSize;
            if (DestroyObjectOnSnap)
                Destroy(obj);
            OnSnap?.Invoke(obj);
        }

        private void OnSocketDeselect(SelectExitEventArgs args)
        {
            var obj = (args.interactableObject as Component)?.gameObject;
            if (CloneOnUnsnap && PartneredPrefab != null && obj != null)
            {
                Instantiate(PartneredPrefab.gameObject, obj.transform.position, obj.transform.rotation);
                Destroy(obj);
            }
            CurrentSnappedObject = null;
            OnUnsnap?.Invoke();
        }

        private void OnSocketHoverEnter(HoverEnterEventArgs args) => OnHoverEnter?.Invoke();

        private void OnSocketHoverExit(HoverExitEventArgs args) => OnHoverExit?.Invoke();

        /// <inheritdoc/>
        public void Snap(GameObject snappedObject)
        {
            var grab = snappedObject.GetComponent<XRIGrabBehaviour>();
            CurrentSnappedObject = grab;
            if (grab != null)
                grab.CurrentSnapZone = this;
            PositionToSnapzone(snappedObject);
            OnSnap?.Invoke(snappedObject);
        }

        /// <inheritdoc/>
        public void Unsnap(GameObject snappedObject)
        {
            OnUnsnapping?.Invoke(snappedObject);
            if (CurrentSnappedObject != null && CurrentSnappedObject.gameObject == snappedObject)
            {
                CurrentSnappedObject.CurrentSnapZone = null;
                CurrentSnappedObject = null;
            }
            OnUnsnap?.Invoke();
        }

        /// <summary>Snap a grabbable into this zone.</summary>
        public void Snap(XRIGrabBehaviour interactable, bool animateSnap = true, bool invisible = false, bool invokeMiddleman = true)
        {
            if (interactable == null)
                return;
            Snap(interactable.gameObject);
        }

        /// <summary>Release a grabbable from this zone.</summary>
        public void Unsnap(XRIGrabBehaviour interactable)
        {
            if (interactable == null)
                return;
            Unsnap(interactable.gameObject);
        }

        /// <summary>Apply the hover highlight material.</summary>
        public void Highlight() { }

        /// <summary>Remove the hover highlight.</summary>
        public void Unhighlight() { }

        /// <summary>True when <paramref name="position"/> is within attach distance of the zone.</summary>
        public bool CheckWithinRange(Vector3 position) =>
            Vector3.Distance(position, (AttachPoint != null ? AttachPoint : transform).position) <= DetachRange;

        /// <summary>Whether a valid object is currently within attach range.</summary>
        public bool WithinAttachRange { get; private set; }

        /// <inheritdoc/>
        public bool IsFree() => CurrentSnappedObject == null;

        /// <inheritdoc/>
        bool ISnapBehaviour.HasDetachRange() => HasDetachRange;

        /// <inheritdoc/>
        public void PositionToSnapzone(GameObject snappedObject)
        {
            if (snappedObject == null)
                return;
            var attach = AttachPoint != null ? AttachPoint : transform;
            snappedObject.transform.SetPositionAndRotation(attach.position, attach.rotation);
        }

        private void Update()
        {
            if (!HasDetachRange || CurrentSnappedObject == null || detached)
                return;
            if (Vector3.Distance(CurrentSnappedObject.transform.position, transform.position) > DetachRange)
            {
                detached = true;
                OnDetachRangeExit?.Invoke(this);
            }
        }
    }
}
