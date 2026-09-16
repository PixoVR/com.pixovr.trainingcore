using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility.Display;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>Graph entry point; feeds the first steps of the main flow.</summary>
    [Serializable]
    [NodeMenuItem("Legacy/Core/Start", null)]
    public class StartNode : StepBaseNode
    {
        /// <summary>Execution output port.</summary>
        [Output(name = "Complete", allowMultiple = true)]
        public ExecutionLink executes;

        /// <inheritdoc/>
        public override string name => "Start";

        /// <inheritdoc/>
        public override StepBase Create() => new StepBase { GUID = GUID, Name = name, IsSkipPoint = IsSkipPoint };

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() => GetNodesOnPort<StepBaseNode>("executes");
    }

    /// <summary>Fail-handler branch node; the default handler is used when no specific one is selected.</summary>
    [NodeMenuItem(null, null, menuTitle = "Current/Fail/Failure Handler")]
    public class FailHandlerNode : EnumConditionalStepNode
    {
        /// <summary>True for the graph's default fail handler.</summary>
        [HideInInspector]
        public bool IsDefault = false;

        /// <summary>Output port carrying fail-reason exposed parameters.</summary>
        [Output(name = "Fail Reason")]
        public string FailReasonParametersPort;

        private string _failNumber = string.Empty;

        /// <summary>Fail-handler index (display order).</summary>
        [HideInInspector]
        public string FailIndex
        {
            get => _failNumber;
            set
            {
                if (_failNumber == value)
                    return;
                _failNumber = value;
                OnFailNumberChanged?.Invoke();
            }
        }

        /// <inheritdoc/>
        public override Type SelectedEnumType => typeof(GameMode);

        /// <inheritdoc/>
        public override string name => "Failure Handler";

        /// <summary>Fired when <see cref="FailIndex"/> changes.</summary>
        public event Action OnFailNumberChanged;

        /// <inheritdoc/>
        public override StepBase Create() => new FailHandlerStep { GUID = GUID, Name = name, IsDefault = IsDefault, IsSkipPoint = IsSkipPoint };

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> ChooseOutputNodes() => GetStepOutputsFor((int)GameModeManager.CurrentMode);

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() => ChooseOutputNodes();
    }

    /// <summary>Global fail-exceptions node (one per graph).</summary>
    public class GlobalExceptionNode : BaseNode
    {
        /// <summary>Serialized exceptions data.</summary>
        [HideInInspector]
        public FailData Data;

        /// <inheritdoc/>
        public override string name => "Global Exceptions";
    }

    /// <summary>Pass-through step node (no behaviour).</summary>
    [Serializable]
    [NodeMenuItem("Current/Core/Relay", null)]
    public class StepRelayNode : TrainingBaseNode
    {
        /// <summary>Flow input.</summary>
        [Input(name = "Start", allowMultiple = true)]
        public ExecutionLink executed;

        /// <summary>Flow output.</summary>
        [Output(name = "Complete", allowMultiple = true)]
        public ExecutionLink executes;

        /// <inheritdoc/>
        public override string name => "Step Relay";
    }

    /// <summary>"Tap Object" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Tap Object", null)]
    public class TapObjectNode : ObjectInteractionStepNodeBase
    {
        /// <summary>Duration requirement.</summary>
        [HideInInspector]
        public DurationSettings DurationSettings;

        /// <summary>Bound tappable component.</summary>
        [HideInInspector]
        public Tappable TapObjectComponent
        {
            get => GetSavedComponent<Tappable>(TargetInteractedObjectPropertyName);
            set => SetSavedComponent(TargetInteractedObjectPropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Tap Object";

        /// <inheritdoc/>
        public override StepBase Create() => new TapObjectStep(this);
    }

    /// <summary>"Grab Object" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Grab Object", null)]
    public class GrabObjectNode : ObjectInteractionStepNodeBase
    {
        /// <summary>Bound grabbable component.</summary>
        [HideInInspector]
        public Grabbable GrabObjectComponent
        {
            get => GetSavedComponent<Grabbable>(TargetInteractedObjectPropertyName);
            set => SetSavedComponent(TargetInteractedObjectPropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Grab Object";

        /// <inheritdoc/>
        public override StepBase Create() => new GrabObjectStep(this);
    }

    /// <summary>"Snap on Zone" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Snap on Zone", null)]
    public class SnapOnZoneStepNode : ObjectInteractionStepNodeBase
    {
        /// <summary>Expected snappable id.</summary>
        [HideInInspector]
        public int SnapObjectId = 0;

        /// <summary>Target snapzone.</summary>
        [HideInInspector]
        public Snapzone Snapzone
        {
            get => GetSavedComponent<Snapzone>(TargetInteractedObjectPropertyName);
            set => SetSavedComponent(TargetInteractedObjectPropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Snap on Zone";

        /// <inheritdoc/>
        public override StepBase Create() => new SnapOnZoneStep(this);
    }

    /// <summary>"Snap Object" step node (legacy variant).</summary>
    [Serializable]
    [NodeMenuItem("Legacy/Steps/Snap Object (LEGACY)", null)]
    public class SnapObjectStepNode : ObjectInteractionStepNodeBase
    {
        /// <summary>Expected snapzone id.</summary>
        [HideInInspector]
        public int SnapZoneId;

        /// <summary>Bound snappable.</summary>
        [HideInInspector]
        public Snappable SnapComponent
        {
            get => GetSavedComponent<Snappable>(TargetInteractedObjectPropertyName);
            set => SetSavedComponent(TargetInteractedObjectPropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Snap Object";

        /// <inheritdoc/>
        public override StepBase Create() => new SnapObjectStep(this);
    }

    /// <summary>"Valve Turn" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Valve Turn", null)]
    public class ValveTurnStepNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the valve.</summary>
        protected readonly string ValvePropertyName = "TargetValve";

        /// <summary>Saved valve reference (serialized).</summary>
        [SerializeField]
        [HideInInspector]
        private NodeSavedProperty guidProperty;

        /// <summary>Rotation state that completes the step.</summary>
        [HideInInspector]
        public ValveState CompletionState = ValveState.Open;

        /// <summary>Completion range for <see cref="ValveState.Other"/>.</summary>
        [HideInInspector]
        public Vector2 CompletionRange = Vector2.negativeInfinity;

        /// <summary>Bound valve component.</summary>
        [HideInInspector]
        public Valve ValveComponent
        {
            get => GetSavedComponent<Valve>(ValvePropertyName);
            set => SetSavedComponent(ValvePropertyName, value);
        }

        /// <inheritdoc/>
        public override string name => "Valve Turn";

        /// <inheritdoc/>
        public override StepBase Create() => new ValveTurnStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(ValvePropertyName);
    }

    /// <summary>"Generic Step" node driven by a <see cref="GenericStepTrigger"/>.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Generic Step", null)]
    public class GenericStepNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the trigger.</summary>
        private readonly string functionalityProperty = "FunctionalityProperty";

        /// <summary>Bound trigger component.</summary>
        public GenericStepTrigger Functionality
        {
            get => GetSavedComponent<GenericStepTrigger>(functionalityProperty);
            set => SetSavedComponent(functionalityProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Generic Step";

        /// <inheritdoc/>
        public override StepBase Create() => new GenericStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(functionalityProperty);
    }

    /// <summary>"Show Display" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Show Display Step", null)]
    public class ShowDisplayNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the display.</summary>
        private readonly string TargetDisplayName = "TargetDisplay";

        /// <summary>Display prefab/object.</summary>
        [HideInInspector]
        public GameObject DisplayObject;

        /// <summary>Display content.</summary>
        [HideInInspector]
        public Data.DisplayData StepDisplayData;

        /// <summary>Placement settings.</summary>
        [HideInInspector]
        public PlacerSettings DisplaySettings;

        /// <summary>Editor foldout.</summary>
        [HideInInspector]
        public bool DataFoldout = true;

        /// <summary>Editor foldout.</summary>
        [HideInInspector]
        public bool PlacerFoldout = true;

        /// <summary>Use config defaults.</summary>
        [HideInInspector]
        public bool UseDefaultSettings = true;

        /// <summary>Bound placer.</summary>
        [HideInInspector]
        public DisplayObjectPlacer TargetDisplay
        {
            get => GetSavedComponent<DisplayObjectPlacer>(TargetDisplayName);
            set => SetSavedComponent(TargetDisplayName, value);
        }

        /// <inheritdoc/>
        public override string name => "Show Display";

        /// <inheritdoc/>
        public override StepBase Create() => new ShowDisplayStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(TargetDisplayName);
    }

    /// <summary>"Wait Duration" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Wait Duration (Step)", null)]
    public class WaitDurationStepNode : SingleFlowStepNode
    {
        /// <summary>Seconds to wait.</summary>
        [HideInInspector]
        public float Duration = 2f;

        /// <inheritdoc/>
        public override string name => "Wait Duration";

        /// <inheritdoc/>
        public override StepBase Create() => new WaitDurationStep(this);
    }

    /// <summary>"Teleport to Location" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Teleport to Location", null)]
    public class TeleportLocationNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the target.</summary>
        private readonly string TeleportLocationProperty = "TargetLocationProperty";

        /// <summary>Teleport every user (multiuser).</summary>
        [HideInInspector]
        public bool ForceAllUsers = true;

        /// <summary>Bound teleporter.</summary>
        public Teleporter TargetLocation
        {
            get => GetSavedComponent<Teleporter>(TeleportLocationProperty);
            set => SetSavedComponent(TeleportLocationProperty, value);
        }

        /// <inheritdoc/>
        public override string name => "Teleport to Location";

        /// <inheritdoc/>
        public override StepBase Create() => new TeleportLocationStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(TeleportLocationProperty);
    }

    /// <summary>"Gaze Object" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Gaze Object", null)]
    public class GazeObjectStepNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for the gaze collider.</summary>
        private readonly string gazeColliderName = "GazeColliderName";

        /// <summary>Duration requirement.</summary>
        [HideInInspector]
        public DurationSettings DurationSettings;

        /// <inheritdoc/>
        public override string name => "Gaze Object";

        /// <summary>Bound gaze target collider.</summary>
        public Collider GazeCollider
        {
            get => GetSavedComponent<Collider>(gazeColliderName);
            set => SetSavedComponent(gazeColliderName, value);
        }

        /// <inheritdoc/>
        public override StepBase Create() => new GazeObjectStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(gazeColliderName);
    }

    /// <summary>"Collide Objects" step node.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Collide Objects", null)]
    public class CollideObjectsStepNode : SingleFlowStepNode
    {
        /// <summary>Saved-property name for collider A.</summary>
        private readonly string ColliderAReferenceName = "ColliderObjectA";

        /// <summary>Saved-property name for the other colliders list.</summary>
        private readonly string ColliderListReferenceName = "OtherColliderObjects";

        /// <summary>Duration requirement.</summary>
        [HideInInspector]
        public DurationSettings DurationSettings;

        /// <summary>First collider.</summary>
        public Collider ColliderA
        {
            get => GetSavedComponent<Collider>(ColliderAReferenceName);
            set => SetSavedComponent(ColliderAReferenceName, value);
        }

        /// <summary>Colliders it must hit.</summary>
        public List<Collider> OtherColliders
        {
            get
            {
                var prop = Data?.Find(ColliderListReferenceName);
                return prop?.ObjectReferences?
                    .Select(r => r.GetComponent<Collider>())
                    .Where(c => c != null).ToList() ?? new List<Collider>();
            }
            set
            {
                if (Data == null)
                    Data = new NodeData();
                var prop = Data.Find(ColliderListReferenceName) ?? new NodeSavedProperty(ColliderListReferenceName);
                prop.ObjectReferences = (value ?? new List<Collider>())
                    .Where(c => c != null).Select(c => new Identity.GuidReference(c.gameObject)).ToList();
                Data.Add(prop);
            }
        }

        /// <inheritdoc/>
        public override string name => "Collide Objects";

        /// <inheritdoc/>
        public override StepBase Create() => new CollideObjectsStep(this);

        /// <inheritdoc/>
        protected override void AddReferences()
        {
            Data?.Add(ColliderAReferenceName);
            Data?.Add(ColliderListReferenceName);
        }
    }

    /// <summary>"AND Group" step node: completes when the grouped steps complete.</summary>
    [Serializable]
    [NodeMenuItem("Current/Group Steps/And", null)]
    public class AndGroupStepNode : SingleFlowStepNode, IGroupNode
    {
        /// <summary>Group membership bookkeeping.</summary>
        [HideInInspector]
        public GroupNodeFunctionality groupNodeFunctionality;

        /// <summary>Completion settings.</summary>
        [HideInInspector]
        public AndGroupSettings Settings;

        /// <inheritdoc/>
        public override string name => "AND Group";

        /// <summary>Whether every grouped step must complete.</summary>
        [HideInInspector]
        public bool CompleteAll
        {
            get => Settings == null || Settings.NeedToCompleteAll;
            set => (Settings ??= new AndGroupSettings()).NeedToCompleteAll = value;
        }

        /// <summary>Number of grouped steps required when not completing all.</summary>
        [HideInInspector]
        public int StepsToComplete
        {
            get => Settings?.NumberOfStepsToComplete ?? 0;
            set => (Settings ??= new AndGroupSettings()).NumberOfStepsToComplete = value;
        }

        /// <inheritdoc/>
        public override StepBase Create() => new AndGroupStep(this);

        /// <summary>Grouped step nodes.</summary>
        public List<List<StepBaseNode>> GetStepNodeGroups() => new List<List<StepBaseNode>> { GetStepOutputs().ToList() };
    }

    /// <summary>Marker interface for group nodes.</summary>
    public interface IGroupNode { }

    /// <summary>Step node whose outputs split into correct and incorrect branches.</summary>
    public abstract class CorrectIncorrectStepBaseNode : StepExecutionNode
    {
        /// <summary>Actions run when the step completes incorrectly.</summary>
        [Output(null, true, name = "Incorrect Complete Actions", allowMultiple = true)]
        public ActionLink OnFinishActionsIncorrect;

        /// <summary>Actions run when the step completes correctly.</summary>
        [Output(null, true, name = "Correct Complete Actions", allowMultiple = true)]
        public ActionLink OnFinishActionsCorrect;

        /// <summary>Actions run when the step starts.</summary>
        [Output(null, true, name = "Start Actions", allowMultiple = true)]
        public ActionLink OnStartActions;

        /// <summary>Flow output taken on an incorrect answer.</summary>
        [Output(null, true, name = "Incorrect")]
        public ExecutionLink IncorrectOutput;

        /// <summary>Flow output taken on a correct answer.</summary>
        [Output(null, true, name = "Correct")]
        public ExecutionLink CorrectOutput;

        /// <summary>Step nodes on the correct output.</summary>
        public IEnumerable<StepBaseNode> GetCorrectOutputs() => GetNodesOnPort<StepBaseNode>("CorrectOutput");

        /// <summary>Step nodes on the incorrect output.</summary>
        public IEnumerable<StepBaseNode> GetIncorrectOutputs() => GetNodesOnPort<StepBaseNode>("IncorrectOutput");

        /// <inheritdoc/>
        public override IEnumerable<StepBaseNode> GetStepOutputs() =>
            GetCorrectOutputs().Concat(GetIncorrectOutputs());

        /// <inheritdoc/>
        public override IEnumerable<ActionNode> GetOnStartActionNodes(GameMode gameMode) =>
            GetNodesOnPort<ActionNode>("OnStartActions").Where(n => n.IsIncludedInMode(gameMode));

        /// <inheritdoc/>
        public override IEnumerable<ActionNode> GetOnFinishActionNodes(GameMode gameMode) =>
            GetNodesOnPort<ActionNode>("OnFinishActionsCorrect")
                .Concat(GetNodesOnPort<ActionNode>("OnFinishActionsIncorrect"))
                .Where(n => n.IsIncludedInMode(gameMode));
    }

    /// <summary>Legacy multiple-choice question step.</summary>
    [Serializable]
    [NodeMenuItem("Legacy/Steps/Question", null)]
    public class QuestionNode : CorrectIncorrectStepBaseNode
    {
        /// <summary>Question UI prefab.</summary>
        [HideInInspector]
        public GameObject QuestionPrefab;

        /// <summary>Question content.</summary>
        [HideInInspector]
        public Data.DisplayData QuestionData;

        /// <summary>Placement settings for the question UI.</summary>
        [HideInInspector]
        public PlacerSettings DisplaySettings;

        /// <summary>Placer foldout state.</summary>
        [HideInInspector]
        public bool PlacerFoldout = true;

        /// <summary>Use the default placement settings.</summary>
        [HideInInspector]
        public bool UseDefaultSettings = true;

        /// <summary>Prefab spawned per answer.</summary>
        [HideInInspector]
        public GameObject AnswerPrefab;

        /// <summary>Candidate answers.</summary>
        [SerializeField]
        [HideInInspector]
        public List<Data.Answer> Answers = new List<Data.Answer>();

        /// <summary>Data foldout state.</summary>
        [HideInInspector]
        public bool DataFoldout = false;

        /// <summary>Number of answers displayed.</summary>
        [HideInInspector]
        public int AnswerCount = 3;

        /// <summary>Show answers in a random order.</summary>
        [HideInInspector]
        public bool RandomOrder = true;

        private readonly string TargetDisplayName = "QuestionDisplay";

        /// <inheritdoc/>
        public override string name => "Question";

        /// <summary>Resolved question display object.</summary>
        public GameObject QuestionDisplay => Data?.Find(TargetDisplayName)?.ObjectReference?.GameObject;

        /// <inheritdoc/>
        public override StepBase Create() => new Flow.QuestionStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(TargetDisplayName);
    }

    /// <summary>Plays a PlayableDirector timeline as a step.</summary>
    [Serializable]
    [NodeMenuItem("Legacy/Steps/Play Timeline", null)]
    public class PlayTimelineStepNode : SingleFlowStepNode
    {
        private readonly string directorSavedReferenceName = "DirectorReference";

        /// <summary>Timeline to play.</summary>
        [SerializeField]
        [HideInInspector]
        public UnityEngine.Timeline.TimelineAsset TimelineAsset;

        /// <summary>Director resolved via the "DirectorReference" saved property.</summary>
        public UnityEngine.Playables.PlayableDirector PlayableDirector
        {
            get => GetSavedComponent<UnityEngine.Playables.PlayableDirector>(directorSavedReferenceName);
            set => SetSavedComponent(directorSavedReferenceName, value);
        }

        /// <inheritdoc/>
        public override string name => "Play Timeline";

        /// <inheritdoc/>
        public override StepBase Create() => new Flow.PlayTimelineStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(directorSavedReferenceName);
    }
}
