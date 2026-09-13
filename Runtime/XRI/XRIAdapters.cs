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

    /// <summary>Marker pairing a snappable object to a named snap zone (replaces SnappableObject).</summary>
    public class XRISnapBehaviour : MonoBehaviour
    {
        /// <summary>Name of the <see cref="XRISnapZone"/> this object belongs to.</summary>
        public string SnapZone;
    }

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

    /// <summary>
    /// <see cref="ITeleportBehaviour"/> over <see cref="BaseTeleportationInteractable"/>; replaces TeleportAreaOpenXR.
    /// </summary>
    public class XRITeleportBehaviour : BaseTeleportationInteractable, ITeleportBehaviour
    {
        /// <summary>Teleport anchor transform.</summary>
        public Transform Anchor;

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
            Anchor != null ? Anchor : (TeleportAnchorTransform != null ? TeleportAnchorTransform : transform);
    }

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
