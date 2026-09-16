using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.HandMenu;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>"Blank Step": stays pending until <see cref="StepBase.Complete"/> is called externally.</summary>
    [Serializable]
    public class BlankStep : StepExecutionBase
    {
        /// <summary>Create from node.</summary>
        public BlankStep(BlankStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
        }

        /// <inheritdoc/>
        public override void OnEnter() => base.OnEnter();

        /// <inheritdoc/>
        public override void SkipForwards() { }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => "Wait";
    }

    /// <summary>"Fade Step": runs a screen fade, completing when it finishes.</summary>
    [Serializable]
    public class FadeStep : StepExecutionBase
    {
        /// <summary>Fade configuration.</summary>
        [SerializeField]
        private FadeSettings settings;

        /// <summary>Create from node.</summary>
        public FadeStep(FadeStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            settings = node.Settings ?? new FadeSettings();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!FadeManager.InstanceExists)
            {
                Log.Warning("FadeStep: no FadeManager in scene", LogCategory.Flow);
                OnStepCompleted();
                return;
            }
            FadeManager.Instance.FadeCanvasGroup(settings, OnStepCompleted);
        }

        /// <inheritdoc/>
        public override void SkipForwards()
        {
            if (FadeManager.InstanceExists)
            {
                var instant = new FadeSettings(settings) { FadeDuration = 0f };
                FadeManager.Instance.FadeCanvasGroup(instant);
            }
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => $"Fade to {settings?.TargetAlpha ?? 0f}";
    }

    /// <summary>"Open/Close Hand Menu": completes when the menu reaches the wanted state.</summary>
    [Serializable]
    public class HandMenuStep : StepExecutionBase, IEventObserver
    {
        /// <summary>Required open state.</summary>
        [SerializeField]
        private bool stateRequired;

        private string menuId;

        /// <summary>Create from node.</summary>
        public HandMenuStep(HandMenuStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            stateRequired = node.ShouldOpen;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            var menu = HandMenuBase.Instance;
            if (menu == null)
            {
                Log.Warning("HandMenuStep: no HandMenu in scene", LogCategory.Flow);
                OnStepCompleted();
                return;
            }
            if (menu.isOpen == stateRequired)
            {
                OnStepCompleted();
                return;
            }
            menuId = menu.ID;
            if (!string.IsNullOrEmpty(menuId))
                EventBus.Instance.Subscribe(menuId, this);
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (args is HandMenuStateChangeEventArgs state && state.State == stateRequired)
                OnStepCompleted();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (!string.IsNullOrEmpty(menuId))
                EventBus.Instance.Unsubscribe(menuId, this);
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => stateRequired ? "Open the hand menu" : "Close the hand menu";
    }

    /// <summary>"Input Action": completes when the bound input action is performed.</summary>
    [Serializable]
    public class InputActionStep : StepExecutionBase
    {
        /// <summary>Bound input action.</summary>
        [SerializeField]
        private InputActionReference inputAction;

        private Action<InputAction.CallbackContext> performed;

        /// <summary>Create from node.</summary>
        public InputActionStep(InputActionStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            inputAction = node.Input;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            var action = inputAction?.action;
            if (action == null)
            {
                Log.Warning("InputActionStep: no InputActionReference bound", LogCategory.Flow);
                return;
            }
            performed = _ => OnStepCompleted();
            action.Enable();
            action.performed += performed;
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            var action = inputAction?.action;
            if (action != null && performed != null)
                action.performed -= performed;
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => $"Perform {inputAction?.action?.name ?? "input action"}";
    }

    /// <summary>"Move To Position": completes when the camera comes within range of the target.</summary>
    [Serializable]
    public class MoveToPositionStep : StepExecutionBase
    {
        /// <summary>Literal target position.</summary>
        [SerializeField]
        private Vector3 location;

        /// <summary>Completion radius.</summary>
        [SerializeField]
        private float validDistance;

        /// <summary>Read position from the scene transform.</summary>
        [SerializeField]
        private bool useTransform;

        /// <summary>Bound location transform.</summary>
        [SerializeField]
        private Transform locationTransform;

        /// <summary>Create from node.</summary>
        public MoveToPositionStep(MoveToPositionStepNode node)
        {
            GUID = node.GUID;
            Name = node.name;
            IsSkipPoint = node.IsSkipPoint;
            location = node.Location;
            validDistance = node.Size;
            useTransform = node.UseTransform;
            locationTransform = node.LocationTransform;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (!GraphFlowManager.InstanceExists)
            {
                Log.Warning("MoveToPositionStep: no GraphFlowManager to poll with", LogCategory.Flow);
                return;
            }
            GraphFlowManager.Instance.Tick += OnTick;
            subscribed = true;
            OnTick();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            UnsubscribeTick();
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            UnsubscribeTick();
            base.UnregisterListeners();
        }

        private bool subscribed;

        private void UnsubscribeTick()
        {
            if (!subscribed)
                return;
            subscribed = false;
            if (GraphFlowManager.InstanceExists)
                GraphFlowManager.Instance.Tick -= OnTick;
        }

        private void OnTick()
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            var target = useTransform && locationTransform != null ? locationTransform.position : location;
            if (Vector3.Distance(camera.transform.position, target) <= validDistance)
            {
                UnsubscribeTick();
                OnStepCompleted();
            }
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => "Move to the marked position";
    }

    /// <summary>"Info Point Step": completes correct/incorrect off an <see cref="InfoPointBase"/>.</summary>
    [Serializable]
    public class InfoPointStep : CorrectIncorrectStepBase
    {
        /// <summary>Bound info point.</summary>
        [SerializeField]
        private InfoPointBase infoPoint;

        /// <summary>Fired when the info point interaction completes (step, correct).</summary>
        public event Action<StepBase, bool> OnInfoPointCompleted;

        /// <summary>Fired when the info point reverts.</summary>
        public event Action OnInfoPointReverted;

        /// <summary>Create from node.</summary>
        public InfoPointStep(InfoPointStepNode node) : base(node)
        {
            infoPoint = node.InfoPoint;
            IsSkipPoint = node.IsSkipPoint;
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (infoPoint == null)
            {
                Log.Warning("InfoPointStep: no InfoPoint bound", LogCategory.Flow);
                OnStepCompleted();
                return;
            }
            infoPoint.CompleteInteraction += OnInteractionCompleted;
            infoPoint.OnReverted += OnRevertHandler;
            infoPoint.Open();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            if (infoPoint != null)
            {
                infoPoint.CompleteInteraction -= OnInteractionCompleted;
                infoPoint.OnReverted -= OnRevertHandler;
            }
        }

        private void OnInteractionCompleted(bool correct)
        {
            OnInfoPointCompleted?.Invoke(this, correct);
            if (correct)
                OnCorrectEvent();
            else
                OnIncorrectEvent();
        }

        private void OnRevertHandler() => OnInfoPointReverted?.Invoke();

        /// <inheritdoc/>
        public override void SkipForwardOnExit() => infoPoint?.Close();

        /// <inheritdoc/>
        public override string GetDefaultDescription() => "Interact with the info point";
    }

    /// <summary>"Info Point Step Group": completes correct/incorrect off a group of info points.</summary>
    [Serializable]
    public class GroupedInfoPointsStep : CorrectIncorrectStepBase, IGroupStepContainer
    {
        /// <summary>Grouped steps (the info points).</summary>
        public List<StepBase> GroupedSteps = new List<StepBase>();

        /// <summary>Pick a random subset to enable.</summary>
        [SerializeField]
        private bool selectRandomPoints;

        /// <summary>Subset size when selecting randomly.</summary>
        [SerializeField]
        private int numberOfPointsToSelect;

        /// <summary>Wait for the confirm button before resolving.</summary>
        [SerializeField]
        private bool requireButtonPressOnComplete;

        [SerializeField]
        private Button onCompleteButtonReference;

        private readonly List<StepBase> chosen = new List<StepBase>();
        private readonly Dictionary<StepBase, bool> outcomes = new Dictionary<StepBase, bool>();
        private bool waitingForButton;

        /// <summary>Create from node.</summary>
        public GroupedInfoPointsStep(GroupedInfoPointsStepNode node) : base(node)
        {
            IsSkipPoint = node.IsSkipPoint;
            selectRandomPoints = node.SelectRandomInfoPoints;
            numberOfPointsToSelect = node.NumberOfInfoPointsToEnable;
            requireButtonPressOnComplete = node.RequireButtonPressOnComplete;
            onCompleteButtonReference = node.CompleteButton;
        }

        /// <inheritdoc/>
        public void SetGroupedSteps(List<StepBase> steps) => GroupedSteps = steps ?? new List<StepBase>();

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            chosen.Clear();
            outcomes.Clear();
            waitingForButton = false;
            chosen.AddRange(ChoosePoints());
            if (chosen.Count == 0)
            {
                Log.Warning("GroupedInfoPointsStep: no grouped steps", LogCategory.Flow);
                OnCorrectEvent();
                return;
            }
            foreach (var step in chosen.OfType<InfoPointStep>())
                step.OnInfoPointCompleted += OnInfoPointCompletedHandler;
            foreach (var step in chosen)
                step?.OnEnter();
        }

        private List<StepBase> ChoosePoints()
        {
            var pool = GroupedSteps.Where(s => s != null).ToList();
            if (!selectRandomPoints || numberOfPointsToSelect <= 0 || pool.Count <= numberOfPointsToSelect)
                return pool;
            var picked = new List<StepBase>();
            var remaining = new List<StepBase>(pool);
            while (picked.Count < numberOfPointsToSelect && remaining.Count > 0)
            {
                var item = RandomManager.InstanceExists
                    ? RandomManager.Instance.GetRandomItem(GUID ?? string.Empty, remaining)
                    : remaining[UnityEngine.Random.Range(0, remaining.Count)];
                picked.Add(item);
                remaining.Remove(item);
            }
            return picked;
        }

        private void OnInfoPointCompletedHandler(StepBase step, bool correct)
        {
            outcomes[step] = correct;
            if (outcomes.Count < chosen.Count)
                return;
            if (requireButtonPressOnComplete && onCompleteButtonReference != null)
            {
                waitingForButton = true;
                onCompleteButtonReference.onClick.AddListener(OnConfirmPressed);
                return;
            }
            Resolve();
        }

        private void OnConfirmPressed()
        {
            if (!waitingForButton)
                return;
            waitingForButton = false;
            Resolve();
        }

        private void Resolve()
        {
            bool allCorrect = chosen.All(s => outcomes.TryGetValue(s, out var c) && c);
            if (allCorrect)
                OnCorrectEvent();
            else
                OnIncorrectEvent();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            foreach (var step in chosen.OfType<InfoPointStep>())
                step.OnInfoPointCompleted -= OnInfoPointCompletedHandler;
            if (onCompleteButtonReference != null)
                onCompleteButtonReference.onClick.RemoveListener(OnConfirmPressed);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            foreach (var step in chosen)
                step?.OnExit();
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void SkipForwardOnExit()
        {
            foreach (var step in chosen.OfType<InfoPointStep>())
                step.SkipForwardOnExit();
            base.SkipForwardOnExit();
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => "Complete the info points";
    }

    /// <summary>"Show Display Group": shows the display once every grouped step completes.</summary>
    [Serializable]
    public class ShowDisplayGroupStep : ShowDisplayStep, IGroupStepContainer
    {
        /// <summary>Grouped steps.</summary>
        public List<StepBase> GroupedSteps = new List<StepBase>();

        /// <summary>Require every grouped step.</summary>
        public bool CompleteAll = true;

        /// <summary>Required count when not completing all.</summary>
        public int StepsToComplete;

        private int completedCount;
        private bool displayShown;

        /// <summary>Create from node.</summary>
        public ShowDisplayGroupStep(ShowDisplayGroupStepNode node) : base(node)
        {
            CompleteAll = node.AndSettings?.NeedToCompleteAll ?? true;
            StepsToComplete = node.AndSettings?.NumberOfStepsToComplete ?? 0;
        }

        private int RequiredCount(int total)
        {
            if (CompleteAll || StepsToComplete <= 0)
                return total;
            return Mathf.Min(StepsToComplete, total);
        }

        /// <inheritdoc/>
        public void SetGroupedSteps(List<StepBase> steps) => GroupedSteps = steps ?? new List<StepBase>();

        /// <inheritdoc/>
        public override void OnEnter()
        {
            if (displayShown)
                return;
            completedCount = 0;
            var steps = GroupedSteps.Where(s => s != null).ToList();
            if (steps.Count == 0)
            {
                displayShown = true;
                base.OnEnter();
                return;
            }
            foreach (var step in steps)
                step.StepCompleted += OnGroupedStepCompleted;
            foreach (var step in steps)
                step.OnEnter();
        }

        private void OnGroupedStepCompleted(StepBase step)
        {
            step.StepCompleted -= OnGroupedStepCompleted;
            completedCount++;
            if (completedCount < RequiredCount(GroupedSteps.Count(s => s != null)))
                return;
            if (displayShown)
                return;
            displayShown = true;
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void UnregisterListeners()
        {
            foreach (var step in GroupedSteps)
                if (step != null)
                    step.StepCompleted -= OnGroupedStepCompleted;
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            UnregisterListeners();
            foreach (var step in GroupedSteps)
                step?.OnExit();
            displayShown = false;
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void SkipForwardOnExit()
        {
            foreach (var step in GroupedSteps)
                step?.OnSkipForwards();
            base.SkipForwardOnExit();
        }

        /// <inheritdoc/>
        public override void SkipBackwards()
        {
            foreach (var step in GroupedSteps)
                step?.SkipBackwards();
            base.SkipBackwards();
        }

        /// <inheritdoc/>
        public override string GetDefaultDescription() => "Complete the group and show the display";
    }
}
