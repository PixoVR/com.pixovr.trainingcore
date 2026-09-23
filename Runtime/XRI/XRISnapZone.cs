using System.Collections;
using System.Collections.Generic;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>
    /// Trigger-collider snap zone (Luminous <c>SnapZoneOpenXR</c> parity); replaces the
    /// earlier <see cref="UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor"/>-based
    /// implementation, which no migrated asset carries. A zone accepts
    /// <see cref="XRIGrabBehaviour"/> objects whose colliders enter its trigger.
    /// </summary>
    [RequireComponent(typeof(Snapzone))]
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

        /// <summary>Whether a valid object is currently within attach range.</summary>
        public bool WithinAttachRange { get; private set; }

        private Snapzone snapzone;
        private Material originalMaterial;
        private Renderer snapzoneRenderer;
        private string snapObjectID;
        private float originalScaleX = 1.0f;

        private float ScaleAdjustedShrunkenSize =>
            ShrunkenSize * (transform.lossyScale.x / originalScaleX);

        private void Awake()
        {
            snapzone = GetComponent<Snapzone>();
        }

        private void Start()
        {
            originalScaleX = transform.lossyScale.x;

            if (TryGetComponent(out snapzoneRenderer))
                originalMaterial = snapzoneRenderer.material;

            if (AttachPoint == null)
            {
                AttachPoint = new GameObject("AttachPoint").transform;
                AttachPoint.parent = transform;
                AttachPoint.localPosition = Vector3.zero;
                AttachPoint.localRotation = Quaternion.identity;
            }

            if (PartneredPrefab != null)
                snapObjectID = PartneredPrefab.SnapZone;

            if (CurrentSnappedObject != null)
            {
                PositionSnappedObject(CurrentSnappedObject, false);
                CurrentSnappedObject.SetCurrentSnapZone(this);
                if (CurrentSnappedObject.TryGetComponent(out ObservableSubject subject))
                    subject.SetStartingSnapzone(snapzone);
            }
        }

        private void Update()
        {
            if (CurrentSnappedObject != null && HasDetachRange)
                CheckWithinRange(CurrentSnappedObject.transform.position);
        }

        /// <summary>True when <paramref name="position"/> is within detach distance of the zone.</summary>
        public bool CheckWithinRange(Vector3 position)
        {
            if (!HasDetachRange)
                return true;

            bool previousRange = WithinAttachRange;
            WithinAttachRange = Vector3.Distance(transform.position, position) < DetachRange;

            if (!WithinAttachRange && WithinAttachRange != previousRange)
                OnDetachRangeExit?.Invoke(this);

            return WithinAttachRange;
        }

        private void OnTriggerEnter(Collider other)
        {
            var interactable = other.GetComponentInParent<XRIGrabBehaviour>();
            if (interactable == null)
                return;

            XRIDiagnostics.Log($"SnapZone '{name}': OnTriggerEnter '{interactable.name}'", this);

            if (interactable.CurrentSnapZone != null)
                return;
            if (CurrentSnappedObject == interactable)
                return;

            XRISnapBehaviour snappable = null;
            if (OnlyAllowPartneredPrefabObjectId)
                interactable.TryGetComponent(out snappable);

            if (OnlyAllowPartneredPrefabObjectId && (snappable == null || snappable.SnapZone != snapObjectID))
                return;

            interactable.AddPossibleSnapZone(this);

            if (CurrentSnappedObject != null)
                return;

            if (MustBePlacedInSnapZone)
            {
                if (HighlightOnHover)
                    Highlight();
                OnHoverEnter?.Invoke();
            }
            else
            {
                NetworkGrabManager grabManager = null;
                if (Multiuser.NetworkManager.Instance != null)
                    grabManager = Multiuser.NetworkManager.Instance.InRoom ? interactable.GetComponent<NetworkGrabManager>() : null;

                if ((grabManager == null && !interactable.isSelected) || (grabManager != null && !grabManager.IsGrabbed))
                {
                    interactable.SetCurrentSnapZone(this);
                    Snap(interactable);
                }
                else
                {
                    OnHoverEnter?.Invoke();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var interactable = other.GetComponentInParent<XRIGrabBehaviour>();
            if (interactable == null)
                return;

            interactable.RemovePossibleSnapZone(this);
            if (interactable.CurrentSnapZone != null)
                return;

            if (!HasSnappedObject)
            {
                XRISnapBehaviour snappable = null;
                if (OnlyAllowPartneredPrefabObjectId)
                    interactable.TryGetComponent(out snappable);

                if (!OnlyAllowPartneredPrefabObjectId || (snappable != null && snappable.SnapZone == snapObjectID))
                {
                    if (HighlightOnHover)
                        Unhighlight();
                    OnHoverExit?.Invoke();
                }
            }
        }

        /// <inheritdoc/>
        public void Snap(GameObject snapObject)
        {
            if (snapObject.TryGetComponent<XRIGrabBehaviour>(out var interactable))
                Snap(interactable, false, false, false);
            else
                Log.Error("No XRIGrabBehaviour component attached to object:" + snapObject.name, LogCategory.Interaction);
        }

        /// <summary>Snap a grabbable into this zone.</summary>
        public void Snap(XRIGrabBehaviour interactable, bool animateSnap = true, bool invisible = false, bool invokeMiddleman = true)
        {
            if (interactable == null)
                return;

            XRIDiagnostics.Log($"SnapZone '{name}': Snap '{interactable.name}'", this);

            if (HasSnappedObject && !DestroyObjectOnSnap && !CloneOnUnsnap)
            {
                XRIDiagnostics.Log($"SnapZone '{name}': already occupied, snap refused", this);
                return;
            }

            if (interactable == CurrentSnappedObject)
            {
                PositionSnappedObject(interactable, animateSnap);
                return;
            }

            interactable.TryGetComponent(out XRISnapBehaviour snappable);
            interactable.OnSnapping?.Invoke();
            if (OnlyAllowPartneredPrefabObjectId && (snappable == null || snappable.SnapZone != snapObjectID))
                return;

            if (DestroyObjectOnSnap && !invisible)
            {
                if (CloneOnUnsnap)
                {
                    Destroy(interactable.gameObject);
                    return;
                }
                CurrentSnappedObject = null;
                interactable.gameObject.SetActive(false);
                interactable.SetCurrentSnapZone(null);
            }

            if (CloneOnUnsnap && CurrentSnappedObject != null)
            {
                Destroy(CurrentSnappedObject.gameObject);
                CurrentSnappedObject = null;
            }

            CurrentSnappedObject = interactable;
            interactable.SetCurrentSnapZone(this);
            PositionSnappedObject(interactable, animateSnap);

            if (invokeMiddleman)
                snapzone?.OnObjectSnapped(interactable.gameObject);

            if (!invisible)
            {
                OnSnap?.Invoke(interactable.gameObject);
                interactable.OnSnapped?.Invoke();
            }

            if (HighlightOnHover)
                Unhighlight();

            if (EmptySnapZoneOnSnap)
            {
                CurrentSnappedObject = null;
                interactable.SetCurrentSnapZone(null);
            }
        }

        /// <summary>Release a grabbable from this zone.</summary>
        public void Unsnap(XRIGrabBehaviour interactable)
        {
            if (interactable == null)
                return;

            XRIDiagnostics.Log($"SnapZone '{name}': Unsnap '{interactable.name}'", this);

            if (!HasSnappedObject && !CloneOnUnsnap)
                return;

            CurrentSnappedObject = null;
            OnUnsnapping?.Invoke(interactable.gameObject);

            interactable.transform.SetParent(interactable.OriginalParent, true);

            if (ResizeOnSnap)
                interactable.transform.localScale = interactable.OriginalScale;

            if (CloneOnUnsnap && PartneredPrefab != null)
            {
                var newObject = Instantiate(PartneredPrefab, AttachPoint.position, Quaternion.identity);
                newObject.GetComponent<ObservableSubject>()?.SetStartingSnapzone(snapzone);
                var grabbableObject = newObject.GetComponent<XRIGrabBehaviour>();
                if (grabbableObject != null)
                {
                    PositionToSnapzone(newObject.gameObject);
                    CurrentSnappedObject = grabbableObject;
                }

                if (Multiuser.NetworkManager.Instance != null && Multiuser.NetworkManager.Instance.InRoom)
                    Multiuser.NetworkManager.Instance.SyncSpawnedObject(newObject.gameObject);
            }

            interactable.CurrentSnapZone = null;
            OnUnsnap?.Invoke();
            interactable.OnUnsnapped?.Invoke();
        }

        /// <inheritdoc/>
        public void Unsnap(GameObject unsnapObject)
        {
            unsnapObject.SetActive(true);
            var grabbableObject = unsnapObject.GetComponent<XRIGrabBehaviour>();
            if (grabbableObject != null)
            {
                grabbableObject.RemovePossibleSnapZone(this);
                Unsnap(grabbableObject);
                OnHoverExit?.Invoke();
            }
            else
            {
                Log.Error("No XRIGrabBehaviour component attached to object:" + unsnapObject.name, LogCategory.Interaction);
            }
        }

        /// <inheritdoc/>
        public void PositionToSnapzone(GameObject snappedObject)
        {
            var grabbable = snappedObject.GetComponent<XRIGrabBehaviour>();
            if (grabbable == null)
                return;
            grabbable.SetCurrentSnapZone(this);
            CurrentSnappedObject = grabbable;
            PositionSnappedObject(grabbable, false);
        }

        private void PositionSnappedObject(XRIGrabBehaviour interactable, bool animateSnap)
        {
            var rigidbody = interactable.GetComponent<Rigidbody>();
            if (rigidbody != null)
                rigidbody.isKinematic = true;
            var interactableTransform = interactable.transform;
            interactableTransform.parent = transform;
            if (ResizeOnSnap)
            {
                if (animateSnap)
                {
                    StartCoroutine(ResizeSnappedObject(interactable, ScaleAdjustedShrunkenSize));
                    StartCoroutine(PositionSnappedObject(interactable));
                }
                else
                {
                    InstantResizeSnappedObject(interactable, ScaleAdjustedShrunkenSize);
                    InstantPositionSnappedObject(interactable);
                }
            }
            else
            {
                if (animateSnap)
                {
                    StartCoroutine(PositionSnappedObject(interactable));
                }
                else
                {
                    InstantPositionSnappedObject(interactable);
                }
            }

            if (HighlightOnHover)
                Unhighlight();

            OnHoverExit?.Invoke();

            if (EmptySnapZoneOnSnap)
            {
                CurrentSnappedObject = null;
                interactable.SetCurrentSnapZone(null);
            }
        }

        private IEnumerator PositionSnappedObject(XRIGrabBehaviour interactable)
        {
            interactable.enabled = false;
            Transform interactableTransform = interactable.transform;

            Vector3 targetPosition = AttachPoint.localPosition + FindAttachPointOffset(interactable, ShrunkenSize);
            Quaternion targetRotation = AttachPoint.localRotation;

            Vector3 startPosition = interactableTransform.localPosition;
            Quaternion startRotation = interactableTransform.localRotation;

            float elapsedTime = 0f;
            while (elapsedTime < PlaceAnimationDuration)
            {
                float percent = elapsedTime / PlaceAnimationDuration;
                interactableTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, percent);
                interactableTransform.localRotation = Quaternion.Lerp(startRotation, targetRotation, percent);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            interactableTransform.localPosition = targetPosition;
            interactableTransform.localRotation = targetRotation;
            interactable.enabled = true;
        }

        private void InstantResizeSnappedObject(XRIGrabBehaviour interactable, float targetSize)
        {
            interactable.transform.localScale = CalculateFinalScale(interactable, targetSize);
        }

        private Vector3 CalculateFinalScale(XRIGrabBehaviour interactable, float targetSize)
        {
            return interactable.transform.localScale / (GetLargestSize(interactable) / targetSize);
        }

        private void InstantPositionSnappedObject(XRIGrabBehaviour interactable)
        {
            Transform interactableTransform;
            (interactableTransform = interactable.transform).localRotation = AttachPoint.localRotation;
            interactableTransform.localPosition = AttachPoint.localPosition + FindAttachPointOffset(interactable, ScaleAdjustedShrunkenSize);
        }

        private IEnumerator ResizeSnappedObject(XRIGrabBehaviour interactable, float targetSize)
        {
            interactable.enabled = false;
            Transform interactableTransform = interactable.transform;

            Vector3 targetScale = interactableTransform.localScale / (GetLargestSize(interactable) / targetSize);
            Vector3 startingScale = interactableTransform.localScale;

            float elapsedTime = 0f;
            while (elapsedTime < PlaceAnimationDuration)
            {
                float percent = elapsedTime / PlaceAnimationDuration;
                interactableTransform.localScale = Vector3.Lerp(startingScale, targetScale, percent);
                elapsedTime += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            interactableTransform.localScale = targetScale;
            interactable.enabled = true;
        }

        private float GetLargestSize(XRIGrabBehaviour interactable)
        {
            var meshFilter = interactable.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                var lossyScale = meshFilter.transform.lossyScale;
                var boundsSize = meshFilter.mesh.bounds.size;
                return Mathf.Max(boundsSize.x * lossyScale.x, boundsSize.y * lossyScale.y, boundsSize.z * lossyScale.z);
            }

            Log.Error("interactable does not have a renderer attached, will use local scale value instead", LogCategory.Interaction);
            return interactable.transform.localScale.x;
        }

        private Vector3 FindAttachPointOffset(XRIGrabBehaviour interactable, float targetSize)
        {
            var interactableTransform = interactable.transform;
            Quaternion transformRotation = interactableTransform.rotation;
            Vector3 originalPosition = interactableTransform.position;
            interactableTransform.rotation = AttachPoint.rotation;
            interactableTransform.position = AttachPoint.position;
            Vector3 offsetDistance;

            if (UseInteractablesAttachPoint && interactable.attachTransform != null)
            {
                offsetDistance = transform.InverseTransformVector(AttachPoint.position - interactable.attachTransform.position);
                if (ResizeOnSnap)
                {
                    var currentScale = interactableTransform.localScale;
                    var finalScale = CalculateFinalScale(interactable, targetSize);
                    if (currentScale.x != 0f)
                        offsetDistance *= finalScale.x / currentScale.x;
                }
            }
            else
            {
                Renderer[] renderers = interactable.GetComponentsInChildren<Renderer>();
                Bounds bounds = new Bounds();
                bool boundSet = false;
                foreach (Renderer r in renderers)
                {
                    if (r.gameObject.activeSelf)
                    {
                        if (!boundSet)
                        {
                            bounds = r.bounds;
                            boundSet = true;
                        }
                        else
                        {
                            bounds.Encapsulate(r.bounds);
                        }
                    }
                }

                offsetDistance = transform.InverseTransformVector(AttachPoint.position - bounds.center);
            }

            interactableTransform.rotation = transformRotation;
            interactableTransform.position = originalPosition;

            return offsetDistance;
        }

        /// <summary>Apply the hover highlight material.</summary>
        public void Highlight()
        {
            if (snapzoneRenderer != null)
                snapzoneRenderer.material = HighlightMaterial;
            else
                XRIDiagnostics.Log($"SnapZone '{name}': no renderer for highlight", this);
        }

        /// <summary>Remove the hover highlight.</summary>
        public void Unhighlight()
        {
            if (snapzoneRenderer != null)
                snapzoneRenderer.material = originalMaterial;
            else
                XRIDiagnostics.Log($"SnapZone '{name}': no renderer for unhighlight", this);
        }

        /// <inheritdoc/>
        public bool IsFree() => !HasSnappedObject;

        /// <inheritdoc/>
        bool ISnapBehaviour.HasDetachRange() => HasDetachRange;

        private void OnDrawGizmos()
        {
            if (ResizeOnSnap)
                Gizmos.DrawWireSphere(transform.position, ShrunkenSize * 0.5f);
        }
    }
}
