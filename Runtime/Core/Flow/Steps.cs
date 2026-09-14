using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Base for steps that complete when a single interactable produces an event.</summary>
    [Serializable]
    public abstract class SingleObjectInteractionStepBase : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Subject id this step listens to.</summary>
        public string SubjectId;

        /// <summary>Subscribe on enter.</summary>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Subscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Unsubscribe(SubjectId, this);
        }

        /// <summary>Handle the published event; completes on a match.</summary>
        public virtual void OnEvent(InteractionEventArgs args)
        {
            if (Matches(args))
                OnStepCompleted();
        }

        /// <summary>Whether the event completes this step.</summary>
        protected abstract bool Matches(InteractionEventArgs args);
    }

    /// <summary>Completes when the bound object is grabbed.</summary>
    [Serializable]
    public class GrabObjectStep : SingleObjectInteractionStepBase
    {
        /// <summary>Create from node.</summary>
        public GrabObjectStep(GrabObjectNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            var target = node.GrabObjectComponent;
            SubjectId = target != null ? target.SubjectId : null;
        }

        /// <inheritdoc/>
        protected override bool Matches(InteractionEventArgs args) => args is GrabInteractionEventArgs;
    }

    /// <summary>Completes when the bound object is tapped.</summary>
    [Serializable]
    public class TapObjectStep : SingleObjectInteractionStepBase
    {
        /// <summary>Required tap duration (0 = any).</summary>
        public float RequiredDuration;

        /// <summary>Create from node.</summary>
        public TapObjectStep(TapObjectNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            RequiredDuration = node.DurationSettings?.IsRequired == true ? node.DurationSettings.DurationNeeded : 0f;
            var target = node.TapObjectComponent;
            SubjectId = target != null ? target.SubjectId : null;
        }

        /// <inheritdoc/>
        protected override bool Matches(InteractionEventArgs args) =>
            args is TapInteractionEventArgs tap && tap.TapDuration >= RequiredDuration;
    }

    /// <summary>Completes when the bound object is used.</summary>
    [Serializable]
    public class UseObjectStep : SingleObjectInteractionStepBase
    {
        /// <summary>Required use duration.</summary>
        public float RequiredDuration;

        /// <summary>Create from node.</summary>
        public UseObjectStep(Graph.ObjectInteractionStepNodeBase node, string subjectId, float duration = 0f)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            SubjectId = subjectId;
            RequiredDuration = duration;
        }

        /// <inheritdoc/>
        protected override bool Matches(InteractionEventArgs args) =>
            args is UseInteractionEventArgs use && use.Duration >= RequiredDuration;
    }

    /// <summary>"Snap on Zone": completes when the expected object lands in the bound zone.</summary>
    [Serializable]
    public class SnapOnZoneStep : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Expected snappable id.</summary>
        public int SnapObjectId;

        /// <summary>Bound snapzone subject id.</summary>
        public string SnapzoneSubjectId;

        private Snapzone zone;

        /// <summary>Create from node.</summary>
        public SnapOnZoneStep(SnapOnZoneStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            SnapObjectId = node.SnapObjectId;
            zone = node.Snapzone;
            SnapzoneSubjectId = zone != null ? zone.SubjectId : null;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!string.IsNullOrEmpty(SnapzoneSubjectId))
                EventBus.Instance.Subscribe(SnapzoneSubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(SnapzoneSubjectId))
                EventBus.Instance.Unsubscribe(SnapzoneSubjectId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is SnapInteractionEventArgs snap &&
                (SnapObjectId == 0 || (snap.SnappedObject != null && snap.SnappedObject.SnapId == SnapObjectId)))
                OnStepCompleted();
        }
    }

    /// <summary>"Snap Object" (legacy): completes when the bound snappable snaps anywhere.</summary>
    [Serializable]
    public class SnapObjectStep : SingleObjectInteractionStepBase
    {
        /// <summary>Expected snapzone id.</summary>
        public int SnapZoneId;

        /// <summary>Create from node.</summary>
        public SnapObjectStep(SnapObjectStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            SnapZoneId = node.SnapZoneId;
            var target = node.SnapComponent;
            SubjectId = target != null ? target.SubjectId : null;
        }

        /// <inheritdoc/>
        protected override bool Matches(InteractionEventArgs args) =>
            args is SnapInteractionEventArgs snap &&
            (SnapZoneId == 0 || (snap.Snapzone != null && snap.Snapzone.SnapzoneID == SnapZoneId));
    }

    /// <summary>"Valve Turn": completes when the bound valve reaches the target rotation.</summary>
    [Serializable]
    public class ValveTurnStep : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Bound valve subject id.</summary>
        public string ValveSubjectId;

        /// <summary>Target state.</summary>
        public ValveState CompletionState = ValveState.Open;

        /// <summary>Rotation range used when <see cref="ValveState.Other"/>.</summary>
        public Vector2 CompletionRange = Vector2.negativeInfinity;

        private Valve valve;

        /// <summary>Create from node.</summary>
        public ValveTurnStep(ValveTurnStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            CompletionState = node.CompletionState;
            CompletionRange = node.CompletionRange;
            valve = node.ValveComponent;
            ValveSubjectId = valve != null ? valve.SubjectId : null;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!string.IsNullOrEmpty(ValveSubjectId))
                EventBus.Instance.Subscribe(ValveSubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(ValveSubjectId))
                EventBus.Instance.Unsubscribe(ValveSubjectId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is not ValveTurnEventArgs turn)
                return;
            bool done = CompletionState switch
            {
                ValveState.Open => Mathf.Approximately(turn.RotationAmount, valve != null ? valve.OpenRotation : turn.OpenRotation),
                ValveState.Close => Mathf.Approximately(turn.RotationAmount, valve != null ? valve.ClosedRotation : turn.ClosedRotation),
                _ => turn.RotationAmount >= CompletionRange.x && turn.RotationAmount <= CompletionRange.y
            };
            if (done)
                OnStepCompleted();
        }
    }

    /// <summary>"Generic Step": completed externally via its <see cref="GenericStepTrigger"/>.</summary>
    [Serializable]
    public class GenericStep : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Bound trigger.</summary>
        [NonSerialized]
        public GenericStepTrigger Functionality;

        /// <summary>Trigger's subject id.</summary>
        public string SubjectId;

        /// <summary>Create from node.</summary>
        public GenericStep(GenericStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            Functionality = node.Functionality;
            SubjectId = Functionality != null ? Functionality.SubjectId : null;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            Functionality?.OnStepEntered();
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Subscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Unsubscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is GenericInteractionEventArgs)
            {
                Functionality?.OnStepCompleted();
                OnStepCompleted();
            }
        }

        /// <inheritdoc/>
        public override void SkipForwards() => Functionality?.OnSkippedForwards();

        /// <inheritdoc/>
        public override void SkipBackwards() => Functionality?.OnSkippedBackwards();
    }

    /// <summary>"Wait Duration": completes after a delay (requires a coroutine runner to tick).</summary>
    [Serializable]
    public class WaitDurationStep : StepExecutionBase
    {
        /// <summary>Seconds to wait.</summary>
        public float Duration = 2f;

        /// <summary>Elapsed while entered.</summary>
        [NonSerialized]
        public float Elapsed;

        /// <summary>Create from node.</summary>
        public WaitDurationStep(WaitDurationStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            Duration = node.Duration;
        }

        /// <summary>Tick the timer; completes when the duration elapses.</summary>
        public void Tick(float deltaTime)
        {
            Elapsed += deltaTime;
            if (Elapsed >= Duration)
                OnStepCompleted();
        }
    }

    /// <summary>"Teleport to Location": completes when the user teleports onto the bound target.</summary>
    [Serializable]
    public class TeleportLocationStep : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Bound teleporter subject id.</summary>
        public string SubjectId;

        /// <summary>Teleport all users.</summary>
        public bool ForceAllUsers = true;

        /// <summary>Create from node.</summary>
        public TeleportLocationStep(TeleportLocationNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            ForceAllUsers = node.ForceAllUsers;
            var target = node.TargetLocation;
            SubjectId = target != null ? target.SubjectId : null;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Subscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Unsubscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is TeleportEventArgs)
                OnStepCompleted();
        }

        /// <inheritdoc/>
        public override void SkipForwards()
        {
            // simulate the teleport via a command so undo keeps working
        }
    }

    /// <summary>"Gaze Object": completes after the bound collider is gazed at long enough.</summary>
    [Serializable]
    public class GazeObjectStep : SingleObjectInteractionStepBase
    {
        /// <summary>Required gaze seconds.</summary>
        public float RequiredDuration;

        /// <summary>Create from node.</summary>
        public GazeObjectStep(GazeObjectStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            RequiredDuration = node.DurationSettings?.IsRequired == true ? node.DurationSettings.DurationNeeded : 0f;
            var target = node.GazeCollider;
            SubjectId = target != null ? target.gameObject.GetGuidString() : null;
        }

        /// <inheritdoc/>
        protected override bool Matches(InteractionEventArgs args) =>
            args is GazeInteractionEventArgs gaze && gaze.GazeDuration >= RequiredDuration;
    }

    /// <summary>"Collide Objects": completes when collider A hits one of the listed colliders.</summary>
    [Serializable]
    public class CollideObjectsStep : StepExecutionBase, Events.IEventObserver
    {
        /// <summary>Subject id of collider A's trigger.</summary>
        public string SubjectId;

        /// <summary>Acceptable other-object guids (empty = any).</summary>
        public List<string> OtherObjectIds = new List<string>();

        /// <summary>Create from node.</summary>
        public CollideObjectsStep(CollideObjectsStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            var a = node.ColliderA;
            SubjectId = a != null ? a.gameObject.GetGuidString() : null;
            OtherObjectIds = node.OtherColliders?.Where(c => c != null)
                .Select(c => c.gameObject.GetGuidString()).ToList() ?? new List<string>();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Subscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(SubjectId))
                EventBus.Instance.Unsubscribe(SubjectId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is CollisionInteractionEventArgs hit && hit.Entered &&
                (OtherObjectIds.Count == 0 ||
                 (hit.OtherObject != null && OtherObjectIds.Contains(hit.OtherObject.GetGuidString()))))
                OnStepCompleted();
        }
    }

    /// <summary>"Show Display": shows a display, completing immediately (or on confirm).</summary>
    [Serializable]
    public class ShowDisplayStep : StepExecutionBase
    {
        /// <summary>Display payload.</summary>
        public Data.DisplayData DisplayData;

        /// <summary>Placement settings.</summary>
        public Settings.PlacerSettings DisplaySettings;

        /// <summary>Display object.</summary>
        public string DisplayObjectId;

        /// <summary>Create from node.</summary>
        public ShowDisplayStep(ShowDisplayNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            DisplayData = node.StepDisplayData;
            DisplaySettings = node.DisplaySettings;
            DisplayObjectId = node.DisplayObject != null ? node.DisplayObject.GetGuidString() : null;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            // actual display shown by Utility.Display.Displayer bound via GraphReferencesManager
            OnStepCompleted();
        }
    }

    /// <summary>"AND Group": completes when all/required grouped child steps complete.</summary>
    [Serializable]
    public class AndGroupStep : StepExecutionBase
    {
        /// <summary>Grouped steps.</summary>
        public List<StepBase> GroupedSteps = new List<StepBase>();

        /// <summary>Require every grouped step.</summary>
        public bool CompleteAll = true;

        /// <summary>Required count when not completing all.</summary>
        public int StepsToComplete;

        private int completedCount;

        /// <summary>Create from node.</summary>
        public AndGroupStep(AndGroupStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            CompleteAll = node.CompleteAll;
            StepsToComplete = node.StepsToComplete;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            completedCount = 0;
            foreach (var step in GroupedSteps)
            {
                if (step != null)
                    step.StepCompleted += OnChildCompleted;
            }
            foreach (var step in GroupedSteps)
                step?.OnEnter();
        }

        private void OnChildCompleted(StepBase child)
        {
            completedCount++;
            int needed = CompleteAll ? GroupedSteps.Count : StepsToComplete;
            if (completedCount >= needed)
                OnStepCompleted();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            foreach (var step in GroupedSteps)
                if (step != null)
                    step.StepCompleted -= OnChildCompleted;
        }
    }

    /// <summary>A chosen branch of a fail-handler node; the entry step of a fail flow.</summary>
    [Serializable]
    public class FailHandlerStep : StepExecutionBase
    {
        /// <summary>True for the default fail handler.</summary>
        public bool IsDefault;

        /// <summary>Fail index assigned by <see cref="Graph.FailHandlerIndexUpdater"/>.</summary>
        public string FailIndex;
    }

    /// <summary>Conditional step: defers output choice to a resolver.</summary>
    [Serializable]
    public class ConditionalStepBase : StepBase
    {
        /// <summary>Candidate output sets; picked by the resolver.</summary>
        public List<EnumToStepMapping> OutputMappings = new List<EnumToStepMapping>();

        /// <summary>Pick outputs for a chosen enum index.</summary>
        public virtual List<StepBase> ChooseOutput(int enumIndex)
        {
            var mapping = OutputMappings.FirstOrDefault(m => m.Enum == enumIndex);
            return mapping?.Steps ?? new List<StepBase>();
        }
    }

    /// <summary>Step that completes with a correct or incorrect outcome, choosing between two output sets.</summary>
    [Serializable]
    public class CorrectIncorrectStepBase : StepExecutionBase
    {
        /// <summary>Steps reached on a correct outcome.</summary>
        [SerializeField]
        private List<StepBase> correctStepOutputs;

        /// <summary>Steps reached on an incorrect outcome.</summary>
        [SerializeField]
        private List<StepBase> incorrectStepOutputs;

        /// <summary>Actions run on a correct outcome.</summary>
        [SerializeField]
        protected List<ActionBase> correctActions = new List<ActionBase>();

        /// <summary>Actions run on an incorrect outcome.</summary>
        [SerializeField]
        protected List<ActionBase> incorrectActions = new List<ActionBase>();

        private CorrectIncorrectStepBaseNode node;

        /// <summary>Whether the outcome has been decided, and its value.</summary>
        public bool? Outcome { get; private set; }

        /// <summary>Steps wired to the correct branch.</summary>
        public List<StepBase> CorrectStepOutputs
        {
            get => correctStepOutputs ??= new List<StepBase>();
            set => correctStepOutputs = value;
        }

        /// <summary>Steps wired to the incorrect branch.</summary>
        public List<StepBase> IncorrectStepOutputs
        {
            get => incorrectStepOutputs ??= new List<StepBase>();
            set => incorrectStepOutputs = value;
        }

        /// <summary>Create from node.</summary>
        public CorrectIncorrectStepBase(CorrectIncorrectStepBaseNode node)
        {
            this.node = node;
            if (node != null)
            {
                GUID = node.GUID;
                Name = node.name;
            }
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public CorrectIncorrectStepBase() { }

        /// <inheritdoc/>
        public override void Initialize(GraphData graph)
        {
            base.Initialize(graph);
            var mode = GameModeManager.CurrentMode;
            correctActions = node?.GetNodesOnPort<ActionNode>("OnFinishActionsCorrect")
                .Where(n => n.IsIncludedInMode(mode)).Select(n => n.Create()).ToList() ?? correctActions;
            incorrectActions = node?.GetNodesOnPort<ActionNode>("OnFinishActionsIncorrect")
                .Where(n => n.IsIncludedInMode(mode)).Select(n => n.Create()).ToList() ?? incorrectActions;
        }

        /// <summary>Restrict the outputs to the correct branch and complete.</summary>
        public virtual void OnCorrectEvent()
        {
            Outcome = true;
            OutputSteps = CorrectStepOutputs.ToList();
            ExecuteActions(correctActions);
            OnStepCompleted();
        }

        /// <summary>Restrict the outputs to the incorrect branch and complete.</summary>
        public virtual void OnIncorrectEvent()
        {
            Outcome = false;
            OutputSteps = IncorrectStepOutputs.ToList();
            ExecuteActions(incorrectActions);
            OnStepCompleted();
        }

        /// <inheritdoc/>
        public override List<ActionBase> GetAllActions()
        {
            var all = base.GetAllActions();
            all.AddRange(correctActions);
            all.AddRange(incorrectActions);
            return all;
        }
    }

    /// <summary>Multiple-choice question step; answers come from <see cref="QuizAnswer"/> components.</summary>
    [Serializable]
    public class QuestionStep : CorrectIncorrectStepBase
    {
        private GameObject questionPrefab;

        [SerializeField]
        private PlacerSettings settings;

        [SerializeField]
        private Data.DisplayData displayData;

        private DisplayObjectAction displayAction;

        [SerializeField]
        private Data.AnswerData answerData;

        /// <summary>The node this step was created from.</summary>
        public QuestionNode QuestionNode { get; set; }

        /// <summary>Create from node.</summary>
        public QuestionStep(QuestionNode node) : base(node)
        {
            QuestionNode = node;
            questionPrefab = node.QuestionPrefab;
            settings = node.UseDefaultSettings ? null : node.DisplaySettings;
            displayData = node.QuestionData;
            answerData = new Data.AnswerData
            {
                Prefab = node.AnswerPrefab,
                RandomOrder = node.RandomOrder,
                DisplayCount = node.AnswerCount,
                Answers = new List<Data.Answer>(node.Answers)
            };
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public QuestionStep() { }

        /// <summary>Show the question UI and listen for an answer.</summary>
        public override void OnEnter()
        {
            base.OnEnter();
            ShowQuestion();
        }

        /// <summary>Spawn/show the question display object.</summary>
        protected virtual void ShowQuestion()
        {
            var display = QuestionNode?.QuestionDisplay;
            if (display != null)
                display.SetActive(true);
        }

        /// <summary>Record an answer choice; completes the step on the matching branch.</summary>
        public virtual void AnswerQuestion(bool correct)
        {
            if (correct)
                OnCorrectEvent();
            else
                OnIncorrectEvent();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            var display = QuestionNode?.QuestionDisplay;
            if (display != null)
                display.SetActive(false);
            base.OnExit();
        }
    }

    /// <summary>Step that plays a <see cref="UnityEngine.Playables.PlayableDirector"/> timeline.</summary>
    [Serializable]
    public class PlayTimelineStep : StepExecutionBase
    {
        /// <summary>Director resolved from the node's scene reference.</summary>
        public UnityEngine.Playables.PlayableDirector Director;

        /// <summary>Timeline asset override.</summary>
        public UnityEngine.Timeline.TimelineAsset Timeline;

        /// <summary>Create from node.</summary>
        public PlayTimelineStep(PlayTimelineStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            Director = node.PlayableDirector;
            Timeline = node.TimelineAsset;
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public PlayTimelineStep() { }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (Director == null)
            {
                OnStepCompleted();
                return;
            }
            Utility.TimelinePlayer.Play(Director, Timeline, onComplete: OnStepCompleted);
        }
    }
}
