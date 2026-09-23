using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Records a grab; captures the initial pose on execute, restores it and forces an ungrab on unexecute.</summary>
    public sealed class GrabCommand : CommandBase
    {
        /// <summary>Reference to the grabbed subject.</summary>
        public ObservableSubject Subject;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private Transform initialParent;
        private Snapzone previousSnapzone;
        private bool captured;

        public GrabCommand(string subjectId) : base(subjectId) { }

        /// <summary>Bind the subject and capture its initial pose and snapzone.</summary>
        public GrabCommand(ObservableSubject subject) : base(subject?.Id)
        {
            Subject = subject;
            var t = Subject?.transform;
            if (t != null)
            {
                initialPosition = t.position;
                initialRotation = t.rotation;
                initialScale = t.localScale;
                initialParent = t.parent;
                captured = true;
            }
            previousSnapzone = subject?.CurrentSnapzone;
        }

        
        public override void Execute()
        {
            if (previousSnapzone != null && Subject != null)
                previousSnapzone.Unsnap(Subject.gameObject);
        }

        
        public override void Unexecute()
        {
            var t = Subject?.transform;
            Subject?.GetComponent<Grabbable>()?.Ungrab();
            if (previousSnapzone != null && Subject != null)
            {
                previousSnapzone.Snap(Subject.gameObject);
            }
            else if (t != null && captured)
            {
                t.SetParent(initialParent);
                t.SetPositionAndRotation(initialPosition, initialRotation);
                t.localScale = initialScale;
            }
            if (Subject != null)
                Subject.CurrentSnapzone = previousSnapzone;
        }
    }

    /// <summary>Snaps a subject into a snapzone; unexecute unsnaps it.</summary>
    public sealed class SnapCommand : CommandBase
    {
        /// <summary>Subject being snapped.</summary>
        public ObservableSubject Subject;

        /// <summary>Zone to snap into.</summary>
        public Snapzone Zone;

        private Snapzone previousSnapzone;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;

        public SnapCommand(ObservableSubject subject, Snapzone zone) : base(subject?.Id)
        {
            Subject = subject;
            Zone = zone;
            previousSnapzone = subject?.CurrentSnapzone;
            var t = subject?.transform;
            if (t != null)
            {
                previousPosition = t.position;
                previousRotation = t.rotation;
                previousScale = t.localScale;
            }
        }

        
        public override void Execute() => Zone?.Snap(Subject?.gameObject);

        
        public override void Unexecute()
        {
            Zone?.Unsnap(Subject?.gameObject);
            if (Subject == null)
                return;
            if (previousSnapzone != null)
            {
                previousSnapzone.Snap(Subject.gameObject);
            }
            else
            {
                Subject.transform.SetPositionAndRotation(previousPosition, previousRotation);
                Subject.transform.localScale = previousScale;
            }
            Subject.CurrentSnapzone = previousSnapzone;
        }
    }

    /// <summary>Holds "use" duration on an object; consecutive uses on the same target merge in history.</summary>
    public sealed class UseObjectCommand : CommandBase
    {
        /// <summary>The object being used.</summary>
        public ObservableSubject UsedSubject;

        /// <summary>Where the use happens.</summary>
        public Transform InteractionLocation;

        /// <summary>Seconds used.</summary>
        public float Duration;

        public UseObjectCommand(ObservableSubject subject, Transform location, float duration) : base(subject?.Id)
        {
            UsedSubject = subject;
            InteractionLocation = location;
            Duration = duration;
        }

        
        public override void Execute() { }

        
        public override void Unexecute() { }
    }

    /// <summary>Teleports the player; unexecute returns it to the previous pose.</summary>
    public sealed class TeleportMovementCommand : CommandBase
    {
        /// <summary>Teleport event data (previous pose + target).</summary>
        public TeleportEventArgs Args;

        private GameObject Player => Args?.TeleportedObject;

        public TeleportMovementCommand(TeleportEventArgs args) : base(args?.SubjectId)
        {
            Args = args;
        }

        
        public override void Execute() => Args?.TeleportLocationMiddleman?.Teleport();

        
        public override void Unexecute()
        {
            if (Player != null)
                Player.transform.SetPositionAndRotation(Args.previousLocation, Args.previousRotation);
            Args?.TeleportLocationMiddleman?.Unexecute();
        }
    }

    /// <summary>Sets a valve's rotation; unexecute restores the start rotation.</summary>
    public sealed class ValveTurnCommand : CommandBase
    {
        /// <summary>Valve turn data.</summary>
        public ValveTurnEventArgs Args;

        public ValveTurnCommand(ValveTurnEventArgs args) : base(args?.SubjectId)
        {
            Args = args;
        }

        
        public override void Execute() => Args?.ValveMiddleman?.SetRotation(Args.RotationAmount);

        
        public override void Unexecute() => Args?.ValveMiddleman?.SetRotation(Args.StartRotation, true);
    }

    /// <summary>Sets a GameObject active state; unexecute restores the previous state.</summary>
    public sealed class SetObjectActiveStateCommand : CommandBase
    {
        /// <summary>Target object.</summary>
        public GameObject Target;

        /// <summary>State to apply on execute.</summary>
        public bool State;

        private bool previousState;

        public SetObjectActiveStateCommand(GameObject target, bool state) : base(target.GetGuidString())
        {
            Target = target;
            State = state;
        }

        
        public override void Execute()
        {
            if (Target == null) return;
            previousState = Target.activeSelf;
            Target.SetActive(State);
        }

        
        public override void Unexecute()
        {
            if (Target != null)
                Target.SetActive(previousState);
        }
    }

    /// <summary>Jumps a timeline to its last frame; unexecute returns to the first frame.</summary>
    public sealed class SkipTimelineCommand : CommandBase
    {
        /// <summary>Director being skipped.</summary>
        public UnityEngine.Playables.PlayableDirector Director;

        /// <summary>Optional asset override.</summary>
        public UnityEngine.Timeline.TimelineAsset Asset;

        public SkipTimelineCommand(string subjectId) : base(subjectId) { }

        public SkipTimelineCommand(UnityEngine.Playables.PlayableDirector director,
            UnityEngine.Timeline.TimelineAsset asset) : base(string.Empty)
        {
            Director = director;
            Asset = asset;
        }

        
        public override void Execute()
        {
            if (Director != null)
                Utility.TimelinePlayer.SetToLastFrame(Director, Asset);
        }

        
        public override void Unexecute()
        {
            if (Director != null)
                Utility.TimelinePlayer.SetToFirstFrame(Director, Asset);
        }
    }

    /// <summary>Command tracking a UI/menu visibility toggle.</summary>
    public sealed class HandMenuStateChangedCommand : CommandBase
    {
        /// <summary>Menu root object.</summary>
        public GameObject MenuObject;

        /// <summary>Desired visible state on execute.</summary>
        public bool Visible;

        public HandMenuStateChangedCommand(GameObject menuObject, bool visible) : base(menuObject.GetGuidString())
        {
            MenuObject = menuObject;
            Visible = visible;
        }

        
        public override void Execute() => MenuObject?.SetActive(Visible);

        
        public override void Unexecute() => MenuObject?.SetActive(!Visible);
    }
}
