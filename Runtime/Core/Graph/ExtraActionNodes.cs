using System;
using System.Collections.Generic;
using GraphProcessor;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>"Fade" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Fade", null)]
    public class FadeActionNode : ActionNode
    {
        /// <summary>Fade configuration.</summary>
        [HideInInspector]
        public FadeSettings FadeSettings;

        /// <inheritdoc/>
        public override string name => "Fade";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            FadeSettings ??= new FadeSettings();
        }

        /// <inheritdoc/>
        public override ActionBase Create() => new FadeAction(this);
    }

    /// <summary>"Log" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Log", null)]
    public class LogActionNode : ActionNode
    {
        /// <summary>Message to log.</summary>
        [HideInInspector]
        public string Message;

        /// <summary>Severity.</summary>
        [HideInInspector]
        public LogType LogType;

        /// <inheritdoc/>
        public override string name => "Log";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            LogType = LogType.Log;
        }

        /// <inheritdoc/>
        public override ActionBase Create() => new LogAction(this);
    }

    /// <summary>"Fail" action node: forces the fail-handler flow.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Fail", null)]
    public class FailActionNode : ActionNode
    {
        /// <summary>Reason passed to the fail handler.</summary>
        [HideInInspector]
        public string FailReason;

        /// <inheritdoc/>
        public override string name => "Fail";

        /// <inheritdoc/>
        public override ActionBase Create() => new FailAction(this);
    }

    /// <summary>"Set Color" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Color", null)]
    public class SetColorActionNode : ActionNode
    {
        private readonly string targetObjectPropertyName = "TargetObject";

        /// <summary>Colour to apply.</summary>
        [HideInInspector]
        public Color TargetColor;

        /// <summary>Target object.</summary>
        [HideInInspector]
        public GameObject TargetObject
        {
            get => Data?.Find(targetObjectPropertyName)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetObjectPropertyName,
                value == null ? null : value.transform);
        }

        /// <inheritdoc/>
        public override string name => "Set Color";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetColorAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(targetObjectPropertyName);
    }

    /// <summary>"Set Object Material" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Object Material", null)]
    public class SetGameObjectMaterialActionNode : ActionNode
    {
        private readonly string targetObjectProperty = "TargetObject";

        /// <summary>Material to apply.</summary>
        [HideInInspector]
        public Material TargetMaterial;

        /// <summary>Target object.</summary>
        [HideInInspector]
        public GameObject TargetObject
        {
            get => Data?.Find(targetObjectProperty)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetObjectProperty,
                value == null ? null : value.transform);
        }

        /// <inheritdoc/>
        public override string name => "Set Object Material";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetGameObjectMaterialAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(targetObjectProperty);
    }

    /// <summary>"Set Object Position" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Object Position", null)]
    public class SetGameObjectPositionActionNode : ActionNode
    {
        private readonly string targetObjectProperty = "TargetObject";
        private readonly string targetLocationProperty = "TargetLocation";

        /// <summary>Literal target position.</summary>
        [HideInInspector]
        public Vector3 ObjectPostion;

        /// <summary>Read the position from <see cref="ObjectTransform"/>.</summary>
        [HideInInspector]
        public bool UseTransform = true;

        /// <summary>Object to move.</summary>
        [HideInInspector]
        public GameObject TargetObject
        {
            get => Data?.Find(targetObjectProperty)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetObjectProperty,
                value == null ? null : value.transform);
        }

        /// <summary>Object whose transform supplies the position.</summary>
        [HideInInspector]
        public GameObject ObjectTransform
        {
            get => Data?.Find(targetLocationProperty)?.ObjectReference?.GameObject;
            set => SetSavedComponent<Transform>(targetLocationProperty,
                value == null ? null : value.transform);
        }

        /// <inheritdoc/>
        public override string name => "Set Object Position";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetGameObjectPositionAction(this);

        /// <inheritdoc/>
        protected override void AddReferences()
        {
            Data?.Add(targetObjectProperty);
            Data?.Add(targetLocationProperty);
        }
    }

    /// <summary>"Set Highlight Zones" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Set Highlight Zones", null)]
    public class SetHighlightZonesActionNode : ActionNode
    {
        private readonly string targetObjectsProperty = "TargetObjects";

        [SerializeField]
        [HideInInspector]
        private List<HighlightZoneData> highlightData = new List<HighlightZoneData>();

        /// <summary>Zone targets + states.</summary>
        [HideInInspector]
        public List<HighlightZoneData> HighlightData
        {
            get => highlightData;
            set => highlightData = value ?? new List<HighlightZoneData>();
        }

        /// <inheritdoc/>
        public override string name => "Set Highlight Zones";

        /// <inheritdoc/>
        public override ActionBase Create() => new SetHighlightZonesAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(targetObjectsProperty);
    }

    /// <summary>"Play Timeline" action node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Play Timeline", null)]
    public class PlayTimelineActionNode : ActionNode
    {
        private readonly string directorSavedReferenceName = "DirectorReference";

        /// <summary>Timeline override.</summary>
        [SerializeField]
        [HideInInspector]
        public TimelineAsset TimelineToPlay;

        /// <summary>Loop playback.</summary>
        [SerializeField]
        [HideInInspector]
        public bool Loop;

        /// <summary>Skip-to-end-on-complete settings.</summary>
        [HideInInspector]
        public SkipOnCompleteSettings SkipOnCompleteSettings;

        /// <summary>Bound director.</summary>
        public PlayableDirector PlayableDirector
        {
            get => GetSavedComponent<PlayableDirector>(directorSavedReferenceName);
            set => SetSavedComponent(directorSavedReferenceName, value);
        }

        /// <inheritdoc/>
        public override string name => "Play Timeline";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            SkipOnCompleteSettings ??= new SkipOnCompleteSettings();
        }

        /// <inheritdoc/>
        public override ActionBase Create() => new PlayTimelineAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(directorSavedReferenceName);
    }

    /// <summary>"Hand Coach" action node: spawns a hand coach and plays an animation.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Hand Coach", null)]
    public class HandCoachActionNode : ActionNode
    {
        /// <summary>Seconds before the animation plays.</summary>
        [HideInInspector]
        public float StartDelay = 5f;

        [HideInInspector]
        private GameObject SelectedHandCoachPrefab;

        /// <summary>Prefab path/name (serialized for migration compat).</summary>
        [HideInInspector]
        public string SelectedHandCoachPrefabString;

        private readonly string spawnLocationSavedPropertyName = "SpawnLocationProperty";

        /// <summary>Animation to play.</summary>
        [HideInInspector]
        public string AnimationToPlay;

        /// <summary>Hand coach prefab to spawn.</summary>
        [HideInInspector]
        public HandCoachBase HandCoachMiddlemanPrefab
        {
            get => SelectedHandCoachPrefab != null
                ? SelectedHandCoachPrefab.GetComponent<HandCoachBase>()
                : null;
            set => SelectedHandCoachPrefab = value != null ? value.gameObject : null;
        }

        /// <summary>Spawn location.</summary>
        [HideInInspector]
        public Transform SpawnLocation
        {
            get => GetSavedComponent<Transform>(spawnLocationSavedPropertyName);
            set => SetSavedComponent(spawnLocationSavedPropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Hand Coach";

        /// <inheritdoc/>
        public override ActionBase Create() => new HandCoachAction(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(spawnLocationSavedPropertyName);
    }

    /// <summary>"Instructional Arrow Action" node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Action/Instructional Arrow Action", null)]
    public class InstructionArrowActionNode : ActionNode
    {
        /// <summary>Saved-property name for the target transform.</summary>
        [HideInInspector]
        public string SavedTransformGuidName = "SavedTrasnformObject";

        private readonly string arrowPlacerProperty = "ArrowPlacer";

        /// <summary>Arrow placement settings.</summary>
        [HideInInspector]
        public ArrowPlacerSettings PlacerSettings = new ArrowPlacerSettings();

        /// <summary>Colour applied when the owning step completes.</summary>
        [HideInInspector]
        public Color CompleteColor = Color.green;

        /// <summary>Target the arrow points at.</summary>
        [HideInInspector]
        public Transform TargetTransform
        {
            get => GetSavedComponent<Transform>(SavedTransformGuidName);
            set => SetSavedComponent(SavedTransformGuidName, value);
        }

        /// <summary>Explicit placer; falls back to the first in the scene.</summary>
        [HideInInspector]
        public Utility.Display.ArrowPlacer Placer
        {
            get => GetSavedComponent<Utility.Display.ArrowPlacer>(arrowPlacerProperty);
            set => SetSavedComponent(arrowPlacerProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Instructional Arrow Action";

        /// <inheritdoc/>
        public override ActionBase Create() => new InstructionalArrowAction(this);

        /// <inheritdoc/>
        protected override void AddReferences()
        {
            Data?.Add(SavedTransformGuidName);
            Data?.Add(arrowPlacerProperty);
        }
    }

    /// <summary>Base for exposed-parameter setter action nodes.</summary>
    public abstract class SetExposedParameterActionNodeBase<T> : ActionNode
    {
        /// <summary>Connected parameter input.</summary>
        [Input(name = "Parameter")]
        public T ParameterNode;

        /// <summary>Value to set.</summary>
        [HideInInspector]
        public T Value;

        /// <summary>The connected exposed parameter.</summary>
        public ExposedParameter ExposedParameter =>
            ExposedParameterManager.ResolveConnectedParameter(this, "ParameterNode");

        /// <inheritdoc/>
        public override ActionBase Create() => new SetExposedParameterAction<T>(this);
    }

    /// <summary>"Bool Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Bool Setter", null)]
    public class SetBoolParameterActionNode : SetExposedParameterActionNodeBase<bool>
    {
        /// <inheritdoc/>
        public override string name => "Bool Setter";
    }

    /// <summary>"Int Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Int Setter", null)]
    public class SetIntParameterActionNode : SetExposedParameterActionNodeBase<int>
    {
        /// <inheritdoc/>
        public override string name => "Int Setter";
    }

    /// <summary>"Float Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Float Setter", null)]
    public class SetFloatParameterActionNode : SetExposedParameterActionNodeBase<float>
    {
        /// <inheritdoc/>
        public override string name => "Float Setter";
    }

    /// <summary>"Double Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Double Setter", null)]
    public class SetDoubleParameterActionNode : SetExposedParameterActionNodeBase<double>
    {
        /// <inheritdoc/>
        public override string name => "Double Setter";
    }

    /// <summary>"Long Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Long Setter", null)]
    public class SetLongParameterActionNode : SetExposedParameterActionNodeBase<long>
    {
        /// <inheritdoc/>
        public override string name => "Long Setter";
    }

    /// <summary>"String Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/String Setter", null)]
    public class SetStringParameterActionNode : SetExposedParameterActionNodeBase<string>
    {
        /// <inheritdoc/>
        public override string name => "String Setter";
    }

    /// <summary>"Color Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Color Setter", null)]
    public class SetColorParameterActionNode : SetExposedParameterActionNodeBase<Color>
    {
        /// <inheritdoc/>
        public override string name => "Color Setter";
    }

    /// <summary>"Vector2 Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Vector2 Setter", null)]
    public class SetVector2ParameterActionNode : SetExposedParameterActionNodeBase<Vector2>
    {
        /// <inheritdoc/>
        public override string name => "Vector2 Setter";
    }

    /// <summary>"Vector3 Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Vector3 Setter", null)]
    public class SetVector3ParameterActionNode : SetExposedParameterActionNodeBase<Vector3>
    {
        /// <inheritdoc/>
        public override string name => "Vector3 Setter";
    }

    /// <summary>"Vector4 Setter" parameter action.</summary>
    [Serializable]
    [NodeMenuItem("Current/Parameters/Vector4 Setter", null)]
    public class SetVector4ParameterActionNode : SetExposedParameterActionNodeBase<Vector4>
    {
        /// <inheritdoc/>
        public override string name => "Vector4 Setter";
    }
}
