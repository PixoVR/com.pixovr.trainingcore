using System;
using System.Collections.Generic;
using GraphProcessor;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility.Display;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>Base for all action nodes: attached to a step's start/finish ports.</summary>
    public abstract class ActionNode : TrainingBaseNode
    {
        /// <summary>Port name steps use for their start-actions output.</summary>
        public const string OnStartActionPortName = "OnStartActions";

        /// <summary>Action input port.</summary>
        [Input(name = "Act", allowMultiple = true)]
        public ActionLink ActionLink;

        /// <summary>Step-boundary undo registrations.</summary>
        [HideInInspector]
        public List<UndoOnStepNodeEntry> UndoEntries = new List<UndoOnStepNodeEntry>();

        /// <summary>Show undo points in the editor.</summary>
        [HideInInspector]
        public bool ShowUndoPoints = true;

        /// <summary>Step nodes wired to this action's input.</summary>
        public IEnumerable<StepBaseNode> GetConnectedStepNodes() => GetNodesOnPort<StepBaseNode>("ActionLink", false);

        /// <summary>True when any step connects.</summary>
        public bool IsConnectedToStep() => GetNodesOnPort<StepBaseNode>("ActionLink", false).GetEnumerator().MoveNext();

        /// <summary>Create the runtime action twin.</summary>
        public abstract ActionBase Create();
    }

    /// <summary>"Show Display Action" node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Show Display Action", null)]
    public class DisplayObjectActionNode : ActionNode
    {
        /// <summary>Saved-property name for the display placer.</summary>
        private readonly string TargetDisplayName = "TargetDisplay";

        /// <summary>Display content.</summary>
        [HideInInspector]
        public DisplayData StepDisplayData = new DisplayData();

        /// <summary>Placement settings.</summary>
        [HideInInspector]
        public PlacerSettings DisplaySettings = new PlacerSettings();

        /// <summary>Editor foldout.</summary>
        [HideInInspector]
        public bool DataFoldout = true;

        /// <summary>Editor foldout.</summary>
        [HideInInspector]
        public bool PlacerFoldout = true;

        /// <summary>Use config defaults.</summary>
        [HideInInspector]
        public bool UseDefaultSettings = true;

        /// <summary>Display prefab/object.</summary>
        [HideInInspector]
        public GameObject DisplayObject;

        /// <inheritdoc/>
        public override string name => "Show Display";

        /// <inheritdoc/>
        public override ActionBase Create() => new DisplayObjectAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(TargetDisplayName);
    }

    /// <summary>"Highlight Object" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Highlight Object", null)]
    public class HighlightActionNode : ActionNode
    {
        /// <summary>Saved-property name.</summary>
        private readonly string highlightObjectProperty = "HighlightObject";

        /// <summary>Target highlight state.</summary>
        [HideInInspector]
        public bool HighlightState;

        /// <summary>Bound highlightable.</summary>
        [HideInInspector]
        public PixoVR.TrainingCore.Utility.HighlightBase HighlightObject
        {
            get => GetSavedComponent<PixoVR.TrainingCore.Utility.HighlightBase>(highlightObjectProperty);
            set => SetSavedComponent<PixoVR.TrainingCore.Utility.HighlightBase>(highlightObjectProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Highlight Object";

        /// <inheritdoc/>
        public override ActionBase Create() => new HighlightAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(highlightObjectProperty);
    }

    /// <summary>"Set Hand Menu Text" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Hand Menu Text", null)]
    public class SetHandMenuTextActionNode : ActionNode
    {
        /// <summary>Text payload.</summary>
        [HideInInspector]
        public DisplayData DisplayData;

        /// <inheritdoc/>
        public override string name => "Set Hand Menu Text";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetHandMenuTextAction(this);
    }

    /// <summary>"Set GameObject Active State" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Game Object Active State", null)]
    public class SetGameObjectActiveStateNode : ActionNode
    {
        /// <summary>Saved-property name for the single target.</summary>
        private readonly string targetObjectProperty = "TargetObject";

        /// <summary>Saved-property name for the multi target list.</summary>
        private readonly string targetObjectsProperty = "TargetObjects";

        /// <summary>Target active state.</summary>
        [HideInInspector]
        public bool State;

        /// <summary>Single target object.</summary>
        [HideInInspector]
        public GameObject TargetObject
        {
            get => Data?.Find(targetObjectProperty)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetObjectProperty,
                value == null ? null : value.transform);
        }

        /// <summary>Multiple target objects.</summary>
        [HideInInspector]
        public List<GameObject> TargetObjects
        {
            get
            {
                var prop = Data?.Find(targetObjectsProperty);
                var list = new List<GameObject>();
                if (prop?.ObjectReferences != null)
                    foreach (var r in prop.ObjectReferences)
                        if (r.GameObject != null)
                            list.Add(r.GameObject);
                return list;
            }
            set
            {
                if (Data == null)
                    Data = new NodeData();
                var prop = Data.Find(targetObjectsProperty) ?? new NodeSavedProperty(targetObjectsProperty);
                prop.ObjectReferences = new List<GuidReference>();
                foreach (var go in value ?? new List<GameObject>())
                    if (go != null)
                        prop.ObjectReferences.Add(new GuidReference(go));
                Data.Add(prop);
            }
        }

        /// <inheritdoc/>
        public override string name => "Set Object Active State";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetGameObjectActiveStateAction(this);

        /// <inheritdoc/>
        protected override void AddReferences()
        {
            Data?.Add(targetObjectProperty);
            Data?.Add(targetObjectsProperty);
        }
    }

    /// <summary>"Set Component State" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Component State", null)]
    public class SetComponentStateActionNode : ActionNode
    {
        /// <summary>Saved-property name.</summary>
        private readonly string targetObjectPropertyName = "TargetObject";

        /// <summary>Assembly-qualified behaviour type name.</summary>
        [SerializeField]
        [HideInInspector]
        private string assemblyQualifiedName;

        /// <summary>Editor dropdown index.</summary>
        [HideInInspector]
        public int CurrentSelectedIndex = 0;

        /// <summary>Target enabled state.</summary>
        [HideInInspector]
        public bool State;

        /// <summary>Target object.</summary>
        [HideInInspector]
        public GameObject TargetObject
        {
            get => Data?.Find(targetObjectPropertyName)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetObjectPropertyName,
                value == null ? null : value.transform);
        }

        /// <summary>Behaviour type resolved from <see cref="assemblyQualifiedName"/>.</summary>
        [HideInInspector]
        public Type BehaviorType
        {
            get => string.IsNullOrEmpty(assemblyQualifiedName) ? null : Type.GetType(assemblyQualifiedName);
            set => assemblyQualifiedName = value?.AssemblyQualifiedName;
        }

        /// <summary>The target behaviour instance.</summary>
        public Behaviour TargetBehavior =>
            BehaviorType == null || TargetObject == null
                ? null
                : TargetObject.GetComponent(BehaviorType) as Behaviour;

        /// <inheritdoc/>
        public override string name => "Set Component State";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetComponentStateAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(targetObjectPropertyName);
    }

    /// <summary>"Generic Action" node bound to a <see cref="GenericActionTrigger"/>.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Generic Action", null)]
    public class GenericActionNode : ActionNode
    {
        /// <summary>Saved-property name.</summary>
        private readonly string functionalityProperty = "FunctionalityProperty";

        /// <summary>Bound trigger.</summary>
        public GenericActionTrigger Functionality
        {
            get => GetSavedComponent<GenericActionTrigger>(functionalityProperty);
            set => SetSavedComponent(functionalityProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Generic Action";

        /// <inheritdoc/>
        public override ActionBase Create() => new GenericAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(functionalityProperty);
    }

    /// <summary>"Play Audio" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Play Audio", null)]
    public class AudioActionNode : ActionNode
    {
        /// <summary>Saved-property name for the audio source.</summary>
        private readonly string AudioSourceProperty = "AudioSourceProperty";

        /// <summary>Clips + playback params.</summary>
        [HideInInspector]
        public AudioClipSettings AudioSettings;

        /// <summary>World position to play from.</summary>
        [HideInInspector]
        public Vector3 LocationToPlayFrom;

        /// <summary>Skip-owning-step-on-complete flag.</summary>
        [HideInInspector]
        public SkipOnCompleteSettings SkipOnCompleteSettings;

        /// <summary>Bound audio source.</summary>
        [HideInInspector]
        public AudioSource AudioSource
        {
            get => GetSavedComponent<AudioSource>(AudioSourceProperty);
            set => SetSavedComponent(AudioSourceProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Play Audio";

        /// <inheritdoc/>
        public override ActionBase Create() => new AudioAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(AudioSourceProperty);
    }

    /// <summary>"Load Scene" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Load Scene", null)]
    public class LoadSceneActionNode : ActionNode
    {
        /// <summary>Scene path.</summary>
        [HideInInspector]
        public string ScenePath;

        /// <summary>Scene name.</summary>
        [HideInInspector]
        public string SceneName;

        /// <summary>Fade out before loading.</summary>
        [HideInInspector]
        public bool FadeOut = true;

        /// <summary>Load additively.</summary>
        [HideInInspector]
        public bool Additive = false;

        /// <summary>Fade duration.</summary>
        [HideInInspector]
        public float Duration = 2f;

        /// <inheritdoc/>
        public override string name => "Load Scene";

        /// <inheritdoc/>
        public override ActionBase Create() => new LoadSceneAction(this);
    }

    /// <summary>"Add Global Exception" action node.</summary>
    [Serializable]
    [NodeMenuItem(null, null, menuTitle = "Current/Action/Add Global Exception")]
    public class AddGlobalExceptionActionNode : ActionNode
    {
        /// <summary>Exceptions to add.</summary>
        [HideInInspector]
        public FailData FailData;

        /// <inheritdoc/>
        public override string name => "Add Global Exception";

        /// <inheritdoc/>
        public override ActionBase Create() => new AddGlobalExceptionAction(this);
    }

    /// <summary>"Remove Global Exception" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Remove Global Exception", null)]
    public class RemoveGlobalExceptionActionNode : ActionNode
    {
        /// <summary>Exceptions to remove.</summary>
        [HideInInspector]
        public FailData FailData;

        /// <inheritdoc/>
        public override string name => "Remove Global Exception";

        /// <inheritdoc/>
        public override ActionBase Create() => new RemoveGlobalExceptionAction(this);
    }
}
