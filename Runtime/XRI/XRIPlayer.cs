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
            var id = Multiuser.NetworkManager.Instance?.CurrentRoom?.GetLocalPlayer()?.Id;
            return int.TryParse(id, out var n) ? n : -1;
        }

        private void HandleGrab() => OnGrab?.Invoke(LocalActorNumber());

        private void HandleRelease() => OnRelease?.Invoke(LocalActorNumber());

        /// <summary>Request local ownership of the object through the network manager.</summary>
        public virtual void RequestOwnership()
        {
            Multiuser.NetworkManager.Instance?.SyncSpawnedObject(gameObject);
            OnOwnerChange?.Invoke();
        }
    }

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

        /// <summary>True once the object has been moved from its spawn pose.</summary>
        [NonSerialized]
        public bool IsSanpped;

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
            LostObjectManager.Instance?.TrackObject(this);
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

    /// <summary>Tracks <see cref="LostObject"/>s and resets them when they fall out of range (replaces LostObjectManager).</summary>
    public class LostObjectManager : Utility.SingletonBehaviour<LostObjectManager>
    {
        /// <summary>Vertical offset below the manager that counts as out of range.</summary>
        public float VerticalResetOffsetDistance = -3f;

        /// <summary>Tracked objects.</summary>
        public List<LostObject> TrackedObjects { get; } = new List<LostObject>();

        /// <summary>Register an object for fall-reset tracking.</summary>
        public void TrackObject(LostObject obj)
        {
            if (obj != null && !TrackedObjects.Contains(obj))
                TrackedObjects.Add(obj);
        }

        /// <summary>Remove an object from tracking.</summary>
        public void UntrackObject(LostObject obj) => TrackedObjects.Remove(obj);

        /// <summary>Test hook: process tracked objects once.</summary>
        public void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Process tracked objects for <paramref name="deltaTime"/> seconds (separated for testability).</summary>
        public void Tick(float deltaTime)
        {
            foreach (var obj in TrackedObjects)
            {
                if (obj == null || obj.IsResetting)
                    continue;
                if (obj.ResetWhenOutOfRange &&
                    obj.transform.position.y < transform.position.y + VerticalResetOffsetDistance - obj.OutOfRangeDistance)
                {
                    obj.ResetObject();
                    continue;
                }
                if (obj.ResetWhenDropped && !obj.IsHeld)
                {
                    obj.DroppedResetTimer += deltaTime;
                    if (obj.DroppedResetTimer >= obj.DroppedWaitTime)
                        obj.ResetObject();
                }
                else
                {
                    obj.DroppedResetTimer = 0f;
                }
            }
        }
    }

    /// <summary>Scene registry of controller + headset references (replaces DeviceReferences).</summary>
    public class DeviceReferences : Utility.SingletonBehaviour<DeviceReferences>
    {
        /// <summary>Right-hand controller.</summary>
        public XRBaseController RightController;

        /// <summary>Left-hand controller.</summary>
        public XRBaseController LeftController;

        /// <summary>Headset camera.</summary>
        public Camera Headset;
    }

    /// <summary>Player interaction toggle driven by the step flow (replaces PlayerNetworkControlsOpenXR).</summary>
    public class PlayerNetworkControls : MonoBehaviour
    {
        /// <summary>Direct interactors enabled/disabled together.</summary>
        public List<XRDirectInteractor> DirectInteractorsToToggle = new List<XRDirectInteractor>();

        /// <summary>GameObjects enabled/disabled together.</summary>
        public List<GameObject> ObjectsToToggle = new List<GameObject>();

        /// <summary>Fired when interaction state changes.</summary>
        public Interactions.UnityBoolEvent OnSetInteractionState;

        /// <summary>Enable or disable player interaction.</summary>
        public virtual void SetInteractionState(bool state)
        {
            foreach (var interactor in DirectInteractorsToToggle)
                if (interactor != null)
                    interactor.enabled = state;
            foreach (var obj in ObjectsToToggle)
                if (obj != null)
                    obj.SetActive(state);
            OnSetInteractionState?.Invoke(state);
        }
    }

    /// <summary>Gaze raycaster used by gaze steps (replaces GazeManager).</summary>
    public class GazeManager : Utility.SingletonBehaviour<GazeManager>
    {
        /// <summary>Radius of the gaze cylinder.</summary>
        public float GazeSize = 1f;

        /// <summary>Max gaze distance.</summary>
        public float GazeDistance = 50f;

        /// <summary>Wire-disc divisions used by the gizmo.</summary>
        public float GizmoDivisions = 10f;

        /// <summary>Origin of the gaze ray (headset).</summary>
        public Transform Origin;

        /// <summary>Layers that block gaze.</summary>
        public LayerMask BlockingLayers;

        /// <summary>Optional eye tracker providing the gaze ray.</summary>
        public MonoBehaviour EyeTracker;

        /// <summary>Cast a gaze ray; returns the first unblocked hit.</summary>
        public virtual bool TryGaze(out RaycastHit hit)
        {
            var origin = Origin != null ? Origin : (Camera.main != null ? Camera.main.transform : transform);
            if (Physics.SphereCast(origin.position, GazeSize, origin.forward, out hit, GazeDistance))
            {
                if ((BlockingLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
                    return true;
            }
            hit = default;
            return false;
        }
    }

    /// <summary>
    /// Enables the pointer ray when the user points at UI or a ray interactable (replaces AutoRayManager).
    /// </summary>
    public class AutoRayManager : MonoBehaviour, IUIInteractor
    {
        /// <summary>The target controller manager.</summary>
        public MonoBehaviour ControllerManager;

        /// <summary>Tracked pointer object for the pointer.</summary>
        public Transform RayRoot;

        /// <summary>Layers to react to.</summary>
        public LayerMask UiLayers;

        /// <summary>Auto ray checking distance.</summary>
        public float AutoRayDistance = 10f;

        private bool hittingUi;

        /// <summary>True while the ray is hitting UI.</summary>
        public bool HittingUi => hittingUi;

        private Ray ray;
        private RaycastHit[] hits = new RaycastHit[10];
        private int hitCounts;
        private bool uiHit;
        private XRUIInputModule xrUiInputModule;

        private void Start()
        {
            xrUiInputModule = FindObjectOfType<XRUIInputModule>();
            xrUiInputModule?.RegisterInteractor(this);
        }

        private void Update()
        {
            if (RayRoot == null)
                return;
            ray.origin = RayRoot.position;
            ray.direction = RayRoot.forward;
            hitCounts = Physics.RaycastNonAlloc(ray, hits, AutoRayDistance, UiLayers);
            uiHit = hitCounts > 0;
            if (TryGetUIModel(out var model))
                uiHit = uiHit || model.currentRaycast.isValid;
            hittingUi = uiHit;
        }

        /// <summary>Manually updates the XR UI input module with this pointer's state.</summary>
        public void UpdateUIModel(ref TrackedDeviceModel model)
        {
            if (RayRoot == null)
                return;
            var position = RayRoot.position;
            model.position = position;
            model.orientation = RayRoot.rotation;
            model.select = true;
            model.raycastLayerMask = UiLayers;
            var raycastPoints = model.raycastPoints;
            raycastPoints.Clear();
            raycastPoints.Add(position);
            raycastPoints.Add(position + RayRoot.forward * AutoRayDistance);
        }

        /// <summary>Reads this pointer's UI model from the input module.</summary>
        public bool TryGetUIModel(out TrackedDeviceModel model)
        {
            if (xrUiInputModule != null)
                return xrUiInputModule.GetTrackedDeviceModel(this, out model);
            model = new TrackedDeviceModel(-1);
            return false;
        }
    }
}
