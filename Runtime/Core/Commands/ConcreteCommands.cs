using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Records a grab; unexecute forces an ungrab.</summary>
    public sealed class GrabCommand : CommandBase
    {
        /// <summary>Reference to the grabbed subject.</summary>
        public ObservableSubject Subject;

        public GrabCommand(string subjectId) : base(subjectId) { }

        
        public override void Execute() { }

        
        public override void Unexecute()
        {
            Subject?.GetComponent<Grabbable>()?.Ungrab();
        }
    }

    /// <summary>Snaps a subject into a snapzone; unexecute unsnaps it.</summary>
    public sealed class SnapCommand : CommandBase
    {
        /// <summary>Subject being snapped.</summary>
        public ObservableSubject Subject;

        /// <summary>Zone to snap into.</summary>
        public Snapzone Zone;

        public SnapCommand(ObservableSubject subject, Snapzone zone) : base(subject?.Id)
        {
            Subject = subject;
            Zone = zone;
        }

        
        public override void Execute() => Zone?.Snap(Subject?.gameObject);

        
        public override void Unexecute() => Zone?.Unsnap(Subject?.gameObject);
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

        
        public override void Unexecute() => Args?.TeleportLocationMiddleman?.Unexecute();
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

    /// <summary>Command that only marks that a step's timeline was skipped (no world state).</summary>
    public sealed class SkipTimelineCommand : CommandBase
    {
        public SkipTimelineCommand(string subjectId) : base(subjectId) { }

        
        public override void Execute() { }

        
        public override void Unexecute() { }
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
