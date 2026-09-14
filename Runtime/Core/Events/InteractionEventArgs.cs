using System;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Events
{
    /// <summary>
    /// Base payload for every interaction/flow event. <see cref="StepNumber"/> is stamped at
    /// creation from <see cref="StepCounter.Current"/> when not supplied.
    /// </summary>
    public class InteractionEventArgs : EventArgs
    {
        /// <summary>Id (guid string) of the <see cref="ObservableSubject"/> this event is about.</summary>
        public string SubjectId;

        /// <summary>Current step number at the time the event was raised.</summary>
        public int StepNumber;

        /// <summary>True when raising this event must never trigger a failure path.</summary>
        public bool IgnoreFailure;

        /// <summary>The command that records (and can undo) the world change caused by this event.</summary>
        public virtual ICommand ToCommand() => null;

        
        public override string ToString() => $"{GetType().Name}(subject={SubjectId}, step={StepNumber})";
    }

    /// <summary>Args for events bound to a concrete interactable component.</summary>
    public class ObjectInteractionEventArgs : InteractionEventArgs
    {
        /// <summary>The subject component the event belongs to.</summary>
        public ObservableSubject Subject { get; }

        public ObjectInteractionEventArgs(ObservableSubject subject)
        {
            Subject = subject;
            SubjectId = subject != null ? subject.Id : null;
        }
    }

    /// <summary>Raised when an object is grabbed.</summary>
    public class GrabInteractionEventArgs : ObjectInteractionEventArgs
    {
        public GrabInteractionEventArgs(ObservableSubject subject) : base(subject) { }

        
        public override ICommand ToCommand() => new GrabCommand(SubjectId);
    }

    /// <summary>Raised when a snappable enters/holds a snapzone.</summary>
    public class SnapInteractionEventArgs : ObjectInteractionEventArgs
    {
        /// <summary>The zone the object was snapped into.</summary>
        public Snapzone Snapzone { get; }

        /// <summary>The snappable component involved.</summary>
        public Snappable SnappedObject { get; }

        public SnapInteractionEventArgs(ObservableSubject subject, Snappable snappable, Snapzone snapzone) : base(subject)
        {
            Snapzone = snapzone;
            SnappedObject = snappable;
        }

        
        public override ICommand ToCommand() => new SnapCommand(Subject, Snapzone);
    }

    /// <summary>Raised while an object is tapped (repeats at the configured rate).</summary>
    public class TapInteractionEventArgs : ObjectInteractionEventArgs
    {
        /// <summary>Seconds the tap has been held.</summary>
        public float TapDuration;

        public TapInteractionEventArgs(ObservableSubject subject, float duration) : base(subject)
        {
            TapDuration = duration;
        }
    }

    /// <summary>Raised when the user "uses" an object for a duration (valve-like holds without rotation).</summary>
    public class UseInteractionEventArgs : ObjectInteractionEventArgs
    {
        /// <summary>Seconds the object has been used.</summary>
        public float Duration;

        /// <summary>Where the use interaction took place (for command merge rules).</summary>
        public Transform InteractionLocation;

        public UseInteractionEventArgs(ObservableSubject subject, Transform location, float duration) : base(subject)
        {
            InteractionLocation = location;
            Duration = duration;
        }

        
        public override ICommand ToCommand() => new UseObjectCommand(Subject, InteractionLocation, Duration);
    }

    /// <summary>Raised when the player teleports onto a teleport target.</summary>
    public class TeleportEventArgs : InteractionEventArgs
    {
        /// <summary>Guid string of the teleported object (usually the player).</summary>
        public string teleportedObjectGuid;

        /// <summary>Position before teleporting.</summary>
        public Vector3 previousLocation;

        /// <summary>Rotation before teleporting.</summary>
        public Quaternion previousRotation;

        /// <summary>The teleporter that was entered.</summary>
        public Teleporter TeleportLocationMiddleman { get; }

        /// <summary>The teleported GameObject.</summary>
        public GameObject TeleportedObject { get; }

        public TeleportEventArgs(Teleporter teleporter, GameObject teleportedObject, Vector3 prevPos, Quaternion prevRot)
        {
            TeleportLocationMiddleman = teleporter;
            TeleportedObject = teleportedObject;
            teleportedObjectGuid = teleportedObject.GetGuidString();
            previousLocation = prevPos;
            previousRotation = prevRot;
            SubjectId = teleporter != null ? teleporter.GetComponent<ObservableSubject>()?.Id : null;
        }

        
        public override ICommand ToCommand() => new TeleportMovementCommand(this);
    }

    /// <summary>Raised when a valve is turned.</summary>
    public class ValveTurnEventArgs : InteractionEventArgs
    {
        /// <summary>Rotation the valve had when the interaction started.</summary>
        public float StartRotation;

        /// <summary>Current absolute rotation.</summary>
        public float RotationAmount = 0f;

        /// <summary>Fully-open rotation.</summary>
        public float OpenRotation;

        /// <summary>Fully-closed rotation.</summary>
        public float ClosedRotation;

        /// <summary>Whether the rotation sits inside the fail padding band.</summary>
        public bool WithinFailPadding;

        /// <summary>Override fail reason supplied by the valve.</summary>
        public string CustomFailReason;

        /// <summary>The valve component involved.</summary>
        public Valve ValveMiddleman { get; }

        public ValveTurnEventArgs(Valve valve, float rotation, bool withinFailPadding, string customFailReason = null)
        {
            ValveMiddleman = valve;
            SubjectId = valve?.Id;
            RotationAmount = rotation;
            StartRotation = rotation;
            OpenRotation = valve != null ? valve.OpenRotation : 0f;
            ClosedRotation = valve != null ? valve.ClosedRotation : 0f;
            WithinFailPadding = withinFailPadding;
            CustomFailReason = customFailReason;
        }

        
        public override ICommand ToCommand() => new ValveTurnCommand(this);
    }

    /// <summary>Raised when a ColliderTrigger sees a tagged/colliding object enter or exit.</summary>
    public class CollisionInteractionEventArgs : ObjectInteractionEventArgs
    {
        /// <summary>The other collider's GameObject.</summary>
        public GameObject OtherObject { get; }

        /// <summary>True for enter, false for exit.</summary>
        public bool Entered;

        public CollisionInteractionEventArgs(ObservableSubject subject, GameObject other, bool entered) : base(subject)
        {
            OtherObject = other;
            Entered = entered;
        }
    }

    /// <summary>Raised on gaze enter/dwell/exit of a GazeTarget.</summary>
    public class GazeInteractionEventArgs : ObjectInteractionEventArgs
    {
        /// <summary>Gaze dwell time in seconds when the event fired.</summary>
        public float GazeDuration;

        public GazeInteractionEventArgs(ObservableSubject subject, float duration) : base(subject)
        {
            GazeDuration = duration;
        }
    }

    /// <summary>Freeform interaction event carrying an id and optional string payload.</summary>
    public class GenericInteractionEventArgs : InteractionEventArgs
    {
        /// <summary>Caller-defined event id.</summary>
        public string EventId;

        /// <summary>Optional payload.</summary>
        public string Payload;

        public GenericInteractionEventArgs(string subjectId, string eventId, string payload = null)
        {
            SubjectId = subjectId;
            EventId = eventId;
            Payload = payload;
        }
    }

    /// <summary>Raised when a display element (info point / UI) is interacted with.</summary>
    public class DisplayInteractionEventArgs : InteractionEventArgs
    {
        /// <summary>Guid string of the object interacted with.</summary>
        public string InteractedObjectGuid;

        public DisplayInteractionEventArgs(string subjectId, string interactedObjectGuid)
        {
            SubjectId = subjectId;
            InteractedObjectGuid = interactedObjectGuid;
        }
    }
}
