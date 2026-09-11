using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Grab interaction: delegates physics to an <see cref="IGrabBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    [RequireComponent(typeof(Snappable))]
    public class Grabbable : InteractableBase
    {
        /// <summary>Grab implementation; auto-fetched from a sibling component in Awake.</summary>
        public IGrabBehaviour GrabBehaviour;

        protected override void Awake()
        {
            if (GrabBehaviour == null)
            {
                GrabBehaviour = GetComponent<IGrabBehaviour>();
                if (GrabBehaviour == null)
                    LogOrError("Grab interaction requires a component implementing IGrabBehaviour");
            }
            base.Awake();
        }

        /// <summary>Called by the grab implementation when the object is grabbed.</summary>
        public void OnGrabbedObjectEvent() => Publish(new GrabInteractionEventArgs(Subject));

        /// <summary>Force release.</summary>
        public void Ungrab() => GrabBehaviour?.ForceUngrab();

        private void LogOrError(string message) => Utility.Log.Error(message, LogCategory.Interaction);
    }

    /// <summary>Static registry of all <see cref="Snappable"/> components (for unique SnapId assignment).</summary>
    public static class SnappableRegistry
    {
        /// <summary>All known snappables in the loaded scenes.</summary>
        public static readonly List<Snappable> SnappableList = new List<Snappable>();

        /// <summary>Register a snappable.</summary>
        public static void Add(Snappable s)
        {
            if (s != null && !SnappableList.Contains(s))
                SnappableList.Add(s);
        }

        /// <summary>Unregister a snappable.</summary>
        public static void Remove(Snappable s) => SnappableList.Remove(s);
    }

    /// <summary>Marks an object as snappable into a <see cref="Snapzone"/>. Serialized <see cref="SnapId"/> matches the Luminous field name.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ObservableSubject))]
    public class Snappable : InteractableBase
    {
        /// <summary>Id referenced by snap-type graph steps.</summary>
        public int SnapId;

        /// <summary>Fired (editor-time) whenever a SnapId may have changed.</summary>
        public static Action<Snappable> OnSnapIdChanged;

        protected override void Awake()
        {
            if (Application.isPlaying)
                base.Awake();
        }

        private void OnValidate()
        {
            OnSnapIdChanged?.Invoke(this);
            SnappableRegistry.Add(this);
        }

        private void Reset()
        {
            SnapId = GetUniqueSnapId();
            SnappableRegistry.Add(this);
        }

        private void OnDestroy() => SnappableRegistry.Remove(this);

        /// <summary>Returns the lowest unused SnapId in the registry.</summary>
        public int GetUniqueSnapId()
        {
            var list = SnappableRegistry.SnappableList;
            if (list.Count <= 0)
                return 1;
            int candidate = 1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != this && list[i].SnapId != candidate && list[i].SnapId != ++candidate)
                    return candidate;
            }
            return list[list.Count - 1].SnapId + 1;
        }

        /// <summary>Called by a <see cref="Snapzone"/> when this object snapped in.</summary>
        public void OnSnappedObjectEvent(Snapzone snapzone)
        {
            Publish(new SnapInteractionEventArgs(Subject, this, snapzone), alsoGlobal: true, toNetwork: false);
            Subject.CurrentSnapzone = snapzone;
        }
    }

    /// <summary>A zone a <see cref="Snappable"/> snaps into; delegates to an <see cref="ISnapBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Snapzone : InteractableBase
    {
        /// <summary>Id referenced by snap steps.</summary>
        public int SnapzoneID = 0;

        /// <summary>Snap implementation; auto-fetched from a sibling component in Awake.</summary>
        public ISnapBehaviour SnapBehaviour;

        /// <summary>True while the zone is free.</summary>
        public bool IsFree => SnapBehaviour == null || SnapBehaviour.IsFree();

        protected override void Awake()
        {
            base.Awake();
            if (SnapBehaviour == null)
            {
                SnapBehaviour = GetComponent<ISnapBehaviour>();
                if (SnapBehaviour == null)
                    Utility.Log.Error("Snapzone requires a component implementing ISnapBehaviour", LogCategory.Interaction);
            }
        }

        /// <summary>Entry point from the snap implementation when an object lands in the zone.</summary>
        public void OnObjectSnapped(GameObject snappedObject)
        {
            snappedObject.GetComponent<Snappable>()?.OnSnappedObjectEvent(this);
        }

        /// <summary>Snap an object in; unsnaps it from any previous zone first.</summary>
        public void Snap(GameObject objectToSnap)
        {
            var subject = objectToSnap.GetComponent<ObservableSubject>();
            if (subject == null || SnapBehaviour == null)
                return;
            if (subject.CurrentSnapzone != null)
                subject.CurrentSnapzone.Unsnap(subject.gameObject);
            subject.CurrentSnapzone = this;
            SnapBehaviour.Snap(objectToSnap);
        }

        /// <summary>Release an object from this zone.</summary>
        public void Unsnap(GameObject gameObject)
        {
            var subject = gameObject.GetComponent<ObservableSubject>();
            if (subject != null && subject.CurrentSnapzone == this)
                subject.CurrentSnapzone = null;
            SnapBehaviour?.Unsnap(gameObject);
        }

        /// <summary>Whether the occupant can be pulled out beyond a detach range.</summary>
        public bool HasDetachRange() => SnapBehaviour != null && SnapBehaviour.HasDetachRange();
    }

    /// <summary>Tap interaction: fires repeated tap events while held past the configured minimum duration.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Tappable : InteractableBase
    {
        private bool isTouched;
        private float duration;
        private float remainingTime;
        private const float invokeRepetitionRate = 0.3f;

        private void OnEnable() => GameModes.GameModeManager.OnFail += OnFail;

        private void OnDisable() => GameModes.GameModeManager.OnFail -= OnFail;

        private void OnFail(List<Flow.StepBase> steps, string reason, int handlerIndex) => OnEndTap();

        private void Update()
        {
            if (!isTouched)
                return;
            duration += Time.deltaTime;
            remainingTime -= Time.deltaTime;
            if (duration >= (TrainingConfig.Instance?.MinTapDuration ?? 0f) && remainingTime <= 0f)
            {
                Publish(new TapInteractionEventArgs(Subject, duration));
                remainingTime = invokeRepetitionRate;
            }
        }

        /// <summary>Begin a tap (called by the input layer).</summary>
        public void OnStartTap()
        {
            if (isTouched)
                return;
            isTouched = true;
            remainingTime = 0f;
            duration = 0f;
        }

        /// <summary>End the tap.</summary>
        public void OnEndTap() => isTouched = false;
    }

    /// <summary>Teleport target: delegates the actual teleport to an <see cref="ITeleportBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Teleporter : InteractableBase
    {
        private ITeleportBehaviour teleportBehaviour;

        /// <summary>Guid string id.</summary>
        public string Id => Subject != null ? Subject.Id : null;

        protected override void Awake()
        {
            base.Awake();
            if (teleportBehaviour == null)
            {
                teleportBehaviour = GetComponent<ITeleportBehaviour>();
                if (teleportBehaviour == null)
                    Utility.Log.Error("Teleport requires a component implementing ITeleportBehaviour", LogCategory.Interaction);
            }
        }

        /// <summary>Teleport the player onto this target.</summary>
        public void Teleport() => teleportBehaviour?.Teleport();

        /// <summary>Undo the teleport.</summary>
        public void Unexecute() => teleportBehaviour?.Unexecute();

        /// <summary>Record + execute a teleport as a command (used when skipping over steps).</summary>
        public void SkipForwards()
        {
            var args = ConstructTeleportArgs();
            var command = args.ToCommand();
            if (command != null)
            {
                command.Execute();
                Commands.CommandHistory.Instance.Record(command);
            }
        }

        private TeleportEventArgs ConstructTeleportArgs()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return new TeleportEventArgs(this, player, player.transform.position, player.transform.rotation);
        }

        /// <summary>Called by the teleport implementation when an object arrives on this target.</summary>
        public void OnObjectEntered(GameObject teleportedObject, Vector3 previousPosition, Quaternion previousRotation)
        {
            Publish(new TeleportEventArgs(this, teleportedObject, previousPosition, previousRotation));
        }
    }

    /// <summary>Valve interaction: delegates rotation to an <see cref="IValveBehaviour"/> and reports turn events.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Valve : InteractableBase
    {
        /// <summary>Fully open rotation (degrees).</summary>
        [HideInInspector]
        public float OpenRotation = 0f;

        /// <summary>Fully closed rotation (degrees).</summary>
        [HideInInspector]
        public float ClosedRotation = 360f;

        /// <summary>Current rotation (degrees).</summary>
        [HideInInspector]
        public float CurrentRotation = 0f;

        /// <summary>Lower bound of the fail padding band.</summary>
        [HideInInspector]
        public float MinFailPadding;

        /// <summary>Upper bound of the fail padding band.</summary>
        [HideInInspector]
        public float MaxFailPadding;

        /// <summary>Degrees around the current rotation tolerated before counting as failure.</summary>
        public float FailurePadding = 40f;

        /// <summary>Valve implementation; auto-fetched from a sibling component in Awake.</summary>
        public IValveBehaviour ValveBehaviour;

        /// <summary>Whether to use <see cref="CustomFailReason"/> instead of the default.</summary>
        public bool HasCustomFailReason;

        /// <summary>Custom fail reason text.</summary>
        public string CustomFailReason;

        /// <summary>Guid string id.</summary>
        public string Id => Subject != null ? Subject.Id : null;

        protected override void Awake()
        {
            base.Awake();
            if (ValveBehaviour == null)
            {
                ValveBehaviour = GetComponent<IValveBehaviour>();
                if (ValveBehaviour == null)
                    Utility.Log.Error("Valve requires a component implementing IValveBehaviour", LogCategory.Interaction);
            }
        }

        private void OnEnable() => SetFailurePadding();

        /// <summary>Called by the valve implementation when rotation changes.</summary>
        public void OnValveTurnEvent(float rotation)
        {
            CurrentRotation = rotation;
            Publish(new ValveTurnEventArgs(this, rotation, WithinFailPaddingRange(rotation),
                HasCustomFailReason ? CustomFailReason : null));
        }

        /// <summary>Set absolute rotation; <paramref name="inverse"/> requests an animated return.</summary>
        public void SetRotation(float value, bool inverse = false)
        {
            CurrentRotation = value;
            ValveBehaviour?.SetRotation(value, inverse);
        }

        /// <summary>Freeze or unfreeze the valve.</summary>
        public void SetFreeze(bool state) => ValveBehaviour?.SetFreeze(state);

        /// <summary>Recompute the fail padding band around the current rotation.</summary>
        public void SetFailurePadding()
        {
            MinFailPadding = CurrentRotation - FailurePadding;
            MaxFailPadding = CurrentRotation + FailurePadding;
        }

        private bool WithinFailPaddingRange(float value) => value > MinFailPadding && value < MaxFailPadding;
    }

    /// <summary>Gaze interaction target: accumulates gaze time and fires events at thresholds.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GazeTarget : InteractableBase
    {
        /// <summary>Seconds of gaze before the first event fires.</summary>
        public float DwellTime = 1f;

        private bool gazing;
        private float gazeTime;
        private bool fired;

        private void Update()
        {
            if (!gazing || fired)
                return;
            gazeTime += Time.deltaTime;
            if (gazeTime >= DwellTime)
            {
                fired = true;
                Publish(new GazeInteractionEventArgs(Subject, gazeTime));
            }
        }

        /// <summary>Begin gaze.</summary>
        public void OnGazeEnter()
        {
            gazing = true;
            gazeTime = 0f;
            fired = false;
        }

        /// <summary>End gaze.</summary>
        public void OnGazeExit()
        {
            gazing = false;
            gazeTime = 0f;
            fired = false;
        }
    }

    /// <summary>Publishes a collision event when a matching collider enters/exits this trigger.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class ColliderTrigger : InteractableBase
    {
        /// <summary>Optional tag filter; empty accepts everything.</summary>
        public string TagFilter = string.Empty;

        private void OnTriggerEnter(Collider other)
        {
            if (Accepts(other))
                Publish(new CollisionInteractionEventArgs(Subject, other.gameObject, true));
        }

        private void OnTriggerExit(Collider other)
        {
            if (Accepts(other))
                Publish(new CollisionInteractionEventArgs(Subject, other.gameObject, false));
        }

        private bool Accepts(Collider other) =>
            string.IsNullOrEmpty(TagFilter) || other.CompareTag(TagFilter);
    }

    /// <summary>UnityEvent bridge used by generic step nodes: fires inspector-wired callbacks on step lifecycle.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GenericStepTrigger : InteractableBase
    {
        /// <summary>Fired when the owning step starts.</summary>
        public UnityEngine.Events.UnityEvent StepEntered;

        /// <summary>Fired when the owning step completes.</summary>
        public UnityEngine.Events.UnityEvent StepCompleted;

        /// <summary>Fired when the owning step is skipped forwards.</summary>
        public UnityEngine.Events.UnityEvent SkippedForwards;

        /// <summary>Fired when the owning step is skipped backwards.</summary>
        public UnityEngine.Events.UnityEvent SkippedBackwards;

        /// <summary>Publish a complete event to end the owning step.</summary>
        public void CompleteStep() => Publish(new GenericInteractionEventArgs(SubjectId, "Complete"));

        /// <summary>Fire the skipped-forwards callbacks.</summary>
        public void SkipForwards() => SkippedForwards?.Invoke();

        /// <summary>Fire the skipped-backwards callbacks.</summary>
        public void SkipBackwards() => SkippedBackwards?.Invoke();

        /// <summary>Invoke <see cref="StepEntered"/>.</summary>
        public void OnStepEntered() => StepEntered?.Invoke();

        /// <summary>Invoke <see cref="StepCompleted"/>.</summary>
        public void OnStepCompleted() => StepCompleted?.Invoke();

        /// <summary>Invoke <see cref="SkippedForwards"/>.</summary>
        public void OnSkippedForwards() => SkippedForwards?.Invoke();

        /// <summary>Invoke <see cref="SkippedBackwards"/>.</summary>
        public void OnSkippedBackwards() => SkippedBackwards?.Invoke();
    }

    /// <summary>UnityEvent bridge for generic action nodes: Execute/Undo callbacks.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GenericActionTrigger : InteractableBase
    {
        /// <summary>Fired when the owning action executes.</summary>
        public UnityEngine.Events.UnityEvent Execute;

        /// <summary>Fired when the owning action is undone.</summary>
        public UnityEngine.Events.UnityEvent Undo;

        /// <summary>Invoke <see cref="Execute"/>.</summary>
        public void Act() => Execute?.Invoke();

        /// <summary>Invoke <see cref="Undo"/>.</summary>
        public void OnUndo() => Undo?.Invoke();
    }
}
