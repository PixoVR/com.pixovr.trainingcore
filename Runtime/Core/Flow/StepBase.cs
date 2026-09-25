using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>
    /// Runtime twin of a step node: wired by <see cref="Graph.GraphParser"/>, driven by a flow.
    /// Public field names mirror the legacy runtime for save-data compatibility.
    /// </summary>
    [Serializable]
    public class StepBase
    {
        /// <summary>Steps that lead into this one.</summary>
        public List<StepBase> InputSteps = new List<StepBase>();

        /// <summary>Node GUID this step was parsed from.</summary>
        public string GUID;

        /// <summary>Whether this step is a valid skip destination point.</summary>
        public bool IsSkipPoint;

        /// <summary>Owning parsed graph.</summary>
        [NonSerialized]
        public GraphData Graph;

        /// <summary>Dotted step number ("3", "3.1").</summary>
        public string StepNumber = string.Empty;

        /// <summary>Display name.</summary>
        public string Name = string.Empty;

        /// <summary>Steps reachable from this one.</summary>
        public List<StepBase> OutputSteps { get; set; } = new List<StepBase>();

        /// <summary>Actions undone when this step completes.</summary>
        public List<ActionBase> UndoCompleteActionList { get; set; } = new List<ActionBase>();

        /// <summary>Actions undone when this step starts.</summary>
        public List<ActionBase> UndoStartActionList { get; set; } = new List<ActionBase>();

        /// <summary>Fired when the step is entered.</summary>
        public event Action<StepBase> StepStarted;

        /// <summary>Fired when the step completes.</summary>
        public event Action<StepBase> StepCompleted;

        /// <summary>Integer part of <see cref="StepNumber"/>.</summary>
        public int GetMainStepNumber()
        {
            int dot = StepNumber.IndexOf('.');
            var s = dot > 0 ? StepNumber.Substring(0, dot) : StepNumber;
            return int.TryParse(s, out var n) ? n : 0;
        }

        /// <summary>Bind to the parsed graph (override to resolve references).</summary>
        public virtual void Initialize(GraphData graph) => Graph = graph;

        /// <summary>Add an input step.</summary>
        public void AddInput(StepBase step)
        {
            if (step != null && !InputSteps.Contains(step))
                InputSteps.Add(step);
        }

        /// <summary>Add an output step.</summary>
        public void AddOuput(StepBase step)
        {
            if (step != null && !OutputSteps.Contains(step))
                OutputSteps.Add(step);
        }

        /// <summary>Undo every action in the list.</summary>
        protected void UndoActions(List<ActionBase> actionList)
        {
            if (actionList == null)
                return;
            foreach (var action in actionList)
                action?.Undo();
        }

        /// <summary>Register an action to undo when this step starts.</summary>
        public void RegisterUndoOnStart(ActionBase action)
        {
            if (action != null && !UndoStartActionList.Contains(action))
                UndoStartActionList.Add(action);
        }

        /// <summary>Register an action to undo when this step completes.</summary>
        public void RegisterUndoOnComplete(ActionBase action)
        {
            if (action != null && !UndoCompleteActionList.Contains(action))
                UndoCompleteActionList.Add(action);
        }

        /// <summary>Unregister a start-undo action.</summary>
        public void UnRegisterUndoOnStart(ActionBase action) => UndoStartActionList.Remove(action);

        /// <summary>Unregister a complete-undo action.</summary>
        public void UnRegisterUndoOnComplete(ActionBase action) => UndoCompleteActionList.Remove(action);

        /// <summary>Enter the step: undoes start-actions and fires <see cref="StepStarted"/>.</summary>
        public virtual void OnEnter()
        {
            Utility.Log.Info($"Step entered: {Name} [{GUID}]", Utility.LogCategory.Flow);
            UndoActions(UndoStartActionList);
            StepStarted?.Invoke(this);
        }

        /// <summary>Default completion path: undoes complete-actions and fires <see cref="StepCompleted"/>.</summary>
        protected virtual void OnStepCompleted()
        {
            UndoActions(UndoCompleteActionList);
            StepCompleted?.Invoke(this);
        }

        /// <summary>Public completion entry point for event handlers.</summary>
        public void Complete() => OnStepCompleted();

        /// <summary>Called when the flow leaves this step.</summary>
        public virtual void OnExit() { }

        /// <summary>Called when the flow fails on this step.</summary>
        public virtual void OnFail() { }

        /// <summary>Route a forwards-skip through the step's hooks.</summary>
        public void OnSkipForwards()
        {
            SkipForwardOnEnter();
            SkipForwardOnExit();
        }

        /// <summary>Skip hook: called when a forward skip lands on this step.</summary>
        public virtual void SkipForwardOnEnter() => UndoActions(UndoStartActionList);

        /// <summary>Skip hook: called when a forward skip leaves this step.</summary>
        public virtual void SkipForwardOnExit()
        {
            SkipForwards();
            UndoActions(UndoCompleteActionList);
        }

        /// <summary>Skip hook: called when skipping backwards over this step.</summary>
        public virtual void SkipBackwards() { }

        /// <summary>Skip hook: called when skipping forwards over this step.</summary>
        public virtual void SkipForwards() { }

        /// <summary>True if <paramref name="step"/> is an input.</summary>
        public bool HasInput(StepBase step) => InputSteps.Contains(step);

        /// <summary>True if <paramref name="step"/> is an output.</summary>
        public bool HasOutput(StepBase step) => OutputSteps.Contains(step);

        /// <summary>Output steps (overridden by conditional/group steps).</summary>
        public virtual List<StepBase> GetStepOutputs() => OutputSteps;
    }

    /// <summary>
    /// Step with start/complete actions, fail-exception data and a fail-handler override.
    /// </summary>
    [Serializable]
    public class StepExecutionBase : StepBase
    {
        /// <summary>Actions run on step entry.</summary>
        public List<ActionBase> StartActions = new List<ActionBase>();

        /// <summary>Actions run on step completion.</summary>
        public List<ActionBase> CompleteActions = new List<ActionBase>();

        /// <summary>Parent group step when nested inside a group node.</summary>
        public StepExecutionBase GroupParent;

        /// <summary>Fail exceptions attached to this step.</summary>
        [NonSerialized]
        public List<Exceptions.FailExceptionBase> FailExceptions = new List<Exceptions.FailExceptionBase>();

        /// <summary>Use the graph's default fail handler.</summary>
        public bool UseDefaultFailhandler;

        /// <summary>Explicit fail-handler step GUID.</summary>
        public string FailHandlerGuid;

        /// <summary>This step can never fail.</summary>
        public bool NeverFail;

        /// <summary>User-authored description.</summary>
        public string Description = string.Empty;

        /// <summary>Play <see cref="CompleteSoundEffect"/> when the step completes.</summary>
        public bool PlaySoundOnComplete;

        /// <summary>Clip played on completion when enabled.</summary>
        public AudioClip CompleteSoundEffect;

        /// <summary>Run start-actions on entry, complete-actions on completion.</summary>
        public override void OnEnter()
        {
            RegisterUndoStepPointsForActions(StartActions);
            base.OnEnter();
            ExecuteActions(StartActions);
            FailureDetectionManager.Instance.AddStepExceptions(this);
        }

        /// <summary>Fail path: honours <see cref="NeverFail"/>.</summary>
        public override void OnFail()
        {
            if (NeverFail)
                return;
            base.OnFail();
            foreach (var action in StartActions)
                action?.OnStepFailed();
        }

        /// <summary>See the interface/base contract.</summary>
        protected override void OnStepCompleted()
        {
            if (PlaySoundOnComplete && CompleteSoundEffect != null && Utility.AudioManager.InstanceExists)
                Utility.AudioManager.Instance.Play(CompleteSoundEffect);
            RegisterUndoStepPointsForActions(CompleteActions);
            ExecuteActions(CompleteActions);
            NotifyStartActionOnStepCompleted();
            base.OnStepCompleted();
        }

        /// <summary>See the interface/base contract.</summary>
        public override void OnExit()
        {
            UnregisterListeners();
            base.OnExit();
            FailureDetectionManager.Instance.RemoveStepExceptions(this);
        }

        /// <summary>Detach event subscriptions (override in event-driven steps).</summary>
        public virtual void UnregisterListeners() { }

        /// <summary>See the interface/base contract.</summary>
        public override void SkipForwardOnEnter()
        {
            RegisterUndoStepPointsForActions(StartActions);
            SkipForwardsActions(StartActions);
            base.SkipForwardOnEnter();
            FailureDetectionManager.Instance.AddStepExceptions(this);
        }

        /// <summary>See the interface/base contract.</summary>
        public override void SkipForwardOnExit()
        {
            RegisterUndoStepPointsForActions(CompleteActions);
            UnregisterListeners();
            base.SkipForwardOnExit();
            if (StartActions != null)
                foreach (var action in StartActions)
                    action?.OnStepForward();
            NotifyStartActionOnStepCompleted();
            SkipForwardsActions(CompleteActions);
            FailureDetectionManager.Instance.RemoveStepExceptions(this);
        }

        /// <summary>See the interface/base contract.</summary>
        public override void SkipBackwards()
        {
            UnregisterUndoStepPointsFor(StartActions);
            UnregisterUndoStepPointsFor(CompleteActions);
            SkipBackwardActions();
            base.SkipBackwards();
        }

        /// <summary>Register each action's undo entry points on their target steps.</summary>
        protected void RegisterUndoStepPointsForActions(IEnumerable<ActionBase> actions)
        {
            if (actions == null)
                return;
            foreach (var action in actions)
            {
                if (action?.UndoOnStepEntryPoints == null)
                    continue;
                foreach (var entry in action.UndoOnStepEntryPoints)
                {
                    var target = Graph?.FindStepByGuid(entry?.NodeGUID);
                    if (target == null)
                        continue;
                    if (entry.Options == UndoActionEntryOption.OnStart)
                        target.RegisterUndoOnStart(action);
                    else
                        target.RegisterUndoOnComplete(action);
                }
            }
        }

        /// <summary>Undo the undo-entry-point registrations.</summary>
        protected void UnregisterUndoStepPointsFor(IEnumerable<ActionBase> actions)
        {
            if (actions == null)
                return;
            foreach (var action in actions)
            {
                if (action?.UndoOnStepEntryPoints == null)
                    continue;
                foreach (var entry in action.UndoOnStepEntryPoints)
                {
                    var target = Graph?.FindStepByGuid(entry?.NodeGUID);
                    if (target == null)
                        continue;
                    if (entry.Options == UndoActionEntryOption.OnStart)
                        target.UnRegisterUndoOnStart(action);
                    else
                        target.UnRegisterUndoOnComplete(action);
                }
            }
        }

        /// <summary>All actions attached to this step.</summary>
        public virtual List<ActionBase> GetAllActions() =>
            StartActions.Concat(CompleteActions).ToList();

        /// <summary>Default human-readable description.</summary>
        public virtual string GetDefaultDescription() => Name;

        /// <summary>See the interface/base contract.</summary>
        public override string ToString() => $"{StepNumber}: {Name}";

        /// <summary>Execute a list of actions.</summary>
        protected void ExecuteActions(List<ActionBase> actionList)
        {
            if (actionList == null)
                return;
            foreach (var action in actionList)
                action?.Act();
        }

        private void SkipForwardsActions(List<ActionBase> actionList)
        {
            if (actionList == null)
                return;
            foreach (var action in actionList)
            {
                action?.Act();
                action?.OnStepForward();
            }
        }

        private void SkipBackwardActions()
        {
            foreach (var action in GetAllActions())
                action?.OnStepBackward();
        }

        private void NotifyStartActionOnStepCompleted()
        {
            foreach (var action in StartActions)
                action?.OnStepCompleted();
        }
    }

    /// <summary>Typed list wrapper used where a grouped set of steps is stored.</summary>
    [Serializable]
    public class StepGroup : IList<StepBase>
    {
        /// <summary>Grouped steps.</summary>
        public List<StepBase> Steps = new List<StepBase>();

        /// <summary>Empty group.</summary>
        public StepGroup() { }

        /// <summary>Group from a list.</summary>
        public StepGroup(List<StepBase> steps) => Steps = steps ?? new List<StepBase>();

        /// <summary>See the interface/base contract.</summary>
        public StepBase this[int index] { get => Steps[index]; set => Steps[index] = value; }
        /// <summary>See the interface/base contract.</summary>
        public int Count => Steps.Count;
        /// <summary>See the interface/base contract.</summary>
        public bool IsReadOnly => false;
        /// <summary>See the interface/base contract.</summary>
        public void Add(StepBase step) => Steps.Add(step);
        /// <summary>See the interface/base contract.</summary>
        public void Clear() => Steps.Clear();
        /// <summary>See the interface/base contract.</summary>
        public bool Contains(StepBase item) => Steps.Contains(item);
        /// <summary>See the interface/base contract.</summary>
        public void CopyTo(StepBase[] array, int index) => Steps.CopyTo(array, index);
        /// <summary>See the interface/base contract.</summary>
        public IEnumerator<StepBase> GetEnumerator() => Steps.GetEnumerator();
        /// <summary>See the interface/base contract.</summary>
        public int IndexOf(StepBase item) => Steps.IndexOf(item);
        /// <summary>See the interface/base contract.</summary>
        public void Insert(int index, StepBase item) => Steps.Insert(index, item);
        /// <summary>See the interface/base contract.</summary>
        public bool Remove(StepBase item) => Steps.Remove(item);
        /// <summary>See the interface/base contract.</summary>
        public void RemoveAt(int index) => Steps.RemoveAt(index);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Steps.GetEnumerator();
    }

    /// <summary>A step that hosts a set of grouped child steps (set at parse time).</summary>
    public interface IGroupStepContainer
    {
        /// <summary>Assign the grouped steps.</summary>
        void SetGroupedSteps(List<StepBase> steps);
    }

    /// <summary>Serializable list of visited step GUIDs (used in FlowSavedData).</summary>
    [Serializable]
    public class VisitedStepGuidList : IEnumerable<string>
    {
        /// <summary>Guids visited in one flow pass.</summary>
        public List<string> StepsVisited = new List<string>();

        /// <summary>See the interface/base contract.</summary>
        public IEnumerator<string> GetEnumerator() => StepsVisited.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => StepsVisited.GetEnumerator();
    }
}
