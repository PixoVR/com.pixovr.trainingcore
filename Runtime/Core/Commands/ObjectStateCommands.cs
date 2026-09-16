using System;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.HandMenu;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Sets the material colour on a target's renderer; unexecute restores the initial colour.</summary>
    [Serializable]
    public sealed class SetColorCommand : CommandBase
    {
        /// <summary>Object owning the renderer.</summary>
        public GameObject Target;

        /// <summary>Colour applied on execute.</summary>
        public Color TargetColor;

        private Color initialColor;
        private bool captured;

        public SetColorCommand(GameObject target, Color color) : base(target.GetGuidString())
        {
            Target = target;
            TargetColor = color;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            var r = Target?.GetComponentInChildren<Renderer>();
            if (r == null)
                return;
            if (!captured)
            {
                initialColor = r.material.color;
                captured = true;
            }
            r.material.color = TargetColor;
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            var r = Target?.GetComponentInChildren<Renderer>();
            if (r != null && captured)
                r.material.color = initialColor;
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new SetColorCommand(Target, initialColor);
    }

    /// <summary>Swaps a renderer's shared material; unexecute restores the initial material.</summary>
    [Serializable]
    public sealed class SetObjectMaterialCommand : CommandBase
    {
        /// <summary>Object owning the renderer.</summary>
        public GameObject Target;

        /// <summary>Material applied on execute.</summary>
        public Material TargetMaterial;

        private Material initialMaterial;
        private bool captured;

        public SetObjectMaterialCommand(GameObject target, Material material) : base(target.GetGuidString())
        {
            Target = target;
            TargetMaterial = material;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            var r = Target?.GetComponentInChildren<Renderer>();
            if (r == null)
                return;
            if (!captured)
            {
                initialMaterial = r.sharedMaterial;
                captured = true;
            }
            r.sharedMaterial = TargetMaterial;
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            var r = Target?.GetComponentInChildren<Renderer>();
            if (r != null && captured)
                r.sharedMaterial = initialMaterial;
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new SetObjectMaterialCommand(Target, initialMaterial);
    }

    /// <summary>Moves a target to a new position; unexecute restores the initial position.</summary>
    [Serializable]
    public sealed class SetObjectPositionCommand : CommandBase
    {
        /// <summary>Object being moved.</summary>
        public GameObject Target;

        /// <summary>Position applied on execute.</summary>
        public Vector3 NewPosition;

        /// <summary>Position captured on first execute.</summary>
        public Vector3 InitialPosition { get; private set; }

        private bool captured;

        public SetObjectPositionCommand(GameObject target, Vector3 newPosition) : base(target.GetGuidString())
        {
            Target = target;
            NewPosition = newPosition;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Target == null)
                return;
            if (!captured)
            {
                InitialPosition = Target.transform.position;
                captured = true;
            }
            Target.transform.position = NewPosition;
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (Target != null && captured)
                Target.transform.position = InitialPosition;
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new SetObjectPositionCommand(Target, InitialPosition);

        /// <inheritdoc/>
        public override string ToString() => $"SetObjectPosition {TargetId} -> {NewPosition}";
    }

    /// <summary>Enables/disables a Behaviour; unexecute restores the initial state.</summary>
    [Serializable]
    public sealed class SetComponentStateCommand : CommandBase
    {
        /// <summary>Behaviour being toggled.</summary>
        public Behaviour BehaviourToToggle;

        /// <summary>State applied on execute.</summary>
        public bool State;

        private bool initialState;
        private bool captured;

        public SetComponentStateCommand(Behaviour behaviour, bool state)
            : base(behaviour != null ? behaviour.gameObject.GetGuidString() : string.Empty)
        {
            BehaviourToToggle = behaviour;
            State = state;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (BehaviourToToggle == null)
                return;
            if (!captured)
            {
                initialState = BehaviourToToggle.enabled;
                captured = true;
            }
            BehaviourToToggle.enabled = State;
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (BehaviourToToggle != null && captured)
                BehaviourToToggle.enabled = initialState;
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new SetComponentStateCommand(BehaviourToToggle, initialState);

        /// <inheritdoc/>
        public override string ToString() => $"SetComponentState {BehaviourToToggle?.GetType().Name} -> {State}";
    }

    /// <summary>Toggles a highlight on a <see cref="HighlightBase"/>; unexecute restores the initial state.</summary>
    [Serializable]
    public sealed class HighlightObjectCommand : CommandBase
    {
        /// <summary>Highlight component being toggled.</summary>
        public HighlightBase TargetObject;

        /// <summary>Highlight state applied on execute.</summary>
        public bool HighlightState;

        private bool initialState;
        private bool captured;

        public HighlightObjectCommand(HighlightBase target, bool state)
            : base(target != null ? target.gameObject.GetGuidString() : string.Empty)
        {
            TargetObject = target;
            HighlightState = state;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (TargetObject == null)
                return;
            if (!captured)
            {
                initialState = TargetObject.IsHighlighted;
                captured = true;
            }
            TargetObject.ToggleHighlight(HighlightState);
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (TargetObject != null && captured)
                TargetObject.ToggleHighlight(initialState);
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new HighlightObjectCommand(TargetObject, initialState);
    }

    /// <summary>Shows display data on the hand menu; unexecute restores the previously displayed data.</summary>
    [Serializable]
    public sealed class SetHandMenuTextCommand : CommandBase
    {
        /// <summary>Data displayed on execute.</summary>
        public DisplayData Data;

        private DisplayData initialData;
        private bool captured;

        public SetHandMenuTextCommand(DisplayData data) : base(string.Empty)
        {
            Data = data;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            var menu = HandMenuBase.Instance;
            if (menu == null)
                return;
            if (!captured)
            {
                initialData = menu.GetDisplayedData();
                captured = true;
            }
            menu.SetText(Data);
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            var menu = HandMenuBase.Instance;
            if (menu != null && captured)
                menu.SetText(initialData);
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new SetHandMenuTextCommand(initialData);
    }

    /// <summary>Invokes a UnityEvent pair — action on execute, undo on unexecute.</summary>
    [Serializable]
    public sealed class GenericActionCommand : CommandBase
    {
        /// <summary>Event invoked on execute.</summary>
        public UnityEvent FunctionalityAction;

        /// <summary>Event invoked on unexecute.</summary>
        public UnityEvent FunctionalityUndo;

        public GenericActionCommand(UnityEvent action, UnityEvent undo) : base(string.Empty)
        {
            FunctionalityAction = action;
            FunctionalityUndo = undo;
        }

        /// <inheritdoc/>
        public override void Execute() => FunctionalityAction?.Invoke();

        /// <inheritdoc/>
        public override void Unexecute() => FunctionalityUndo?.Invoke();

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new GenericActionCommand(FunctionalityUndo, FunctionalityAction);
    }

    /// <summary>Fades the screen via <see cref="FadeManager"/>; unexecute fades back to the initial opacity.</summary>
    [Serializable]
    public sealed class FadeCommand : CommandBase
    {
        /// <summary>Fade settings applied on execute.</summary>
        public FadeSettings Settings;

        /// <summary>Optional completion callback.</summary>
        public Action OnComplete;

        private float initialOpacity;
        private bool captured;

        public FadeCommand(FadeSettings settings, Action onComplete = null) : base(string.Empty)
        {
            Settings = settings;
            OnComplete = onComplete;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!FadeManager.InstanceExists)
            {
                Log.Warning("FadeCommand: no FadeManager in scene", LogCategory.GameManagerLogic);
                return;
            }
            if (!captured)
            {
                initialOpacity = FadeManager.Instance.CurrentOpacity;
                captured = true;
            }
            FadeManager.Instance.FadeCanvasGroup(Settings, OnComplete);
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (!FadeManager.InstanceExists || !captured)
                return;
            var back = new FadeSettings(Settings) { TargetAlpha = initialOpacity };
            FadeManager.Instance.FadeCanvasGroup(back);
        }

        /// <inheritdoc/>
        public override CommandBase GetInverse() =>
            new FadeCommand(new FadeSettings(Settings) { TargetAlpha = captured ? initialOpacity : 0f });
    }

    /// <summary>Record-only marker for a float property change (used by undo history).</summary>
    [Serializable]
    public sealed class FloatChangedCommand : CommandBase
    {
        /// <summary>Value after the change.</summary>
        public float NewValue;

        /// <summary>Value before the change.</summary>
        public float OldValue;

        public FloatChangedCommand(float value, float oldValue) : base(string.Empty)
        {
            NewValue = value;
            OldValue = oldValue;
        }

        /// <inheritdoc/>
        public override void Execute() { }

        /// <inheritdoc/>
        public override void Unexecute() { }

        /// <inheritdoc/>
        public override string ToString() => $"FloatChanged {OldValue} -> {NewValue}";
    }

    /// <summary>Record-only marker that a display interaction happened on an interactable.</summary>
    [Serializable]
    public sealed class DisplayInteractionCommand : CommandBase
    {
        /// <summary>Guid of the interactable.</summary>
        public string InteractableGuid;

        public DisplayInteractionCommand(string interactableGuid) : base(interactableGuid)
        {
            InteractableGuid = interactableGuid;
        }

        /// <inheritdoc/>
        public override void Execute() { }

        /// <inheritdoc/>
        public override void Unexecute() { }

        /// <inheritdoc/>
        public override string ToString() => $"DisplayInteraction {InteractableGuid}";
    }

    /// <summary>Record-only command capturing a generic interaction event for undo grouping.</summary>
    [Serializable]
    public sealed class GenericInteractionCommand : CommandBase
    {
        /// <summary>The interaction event recorded.</summary>
        public GenericInteractionEventArgs Args;

        public GenericInteractionCommand(GenericInteractionEventArgs args) : base(args?.SubjectId ?? string.Empty)
        {
            Args = args;
        }

        /// <inheritdoc/>
        public override void Execute() { }

        /// <inheritdoc/>
        public override void Unexecute() { }

        /// <inheritdoc/>
        public override string ToString() => $"GenericInteraction {Args?.EventId} on {SubjectId}";
    }
}
