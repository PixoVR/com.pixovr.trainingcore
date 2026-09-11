using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Graph;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Runtime representation of a parsed <see cref="TrainingGraph"/>.</summary>
    [Serializable]
    public class GraphData
    {
        /// <summary>All fail-handler flow roots.</summary>
        [NonSerialized]
        public List<FailHandlerStep> FailureHandlerSteps = new List<FailHandlerStep>();

        /// <summary>Default fail-handler GUID.</summary>
        [SerializeField]
        private string defaultFailHandlerGuid = "";

        /// <summary>Main-flow root step.</summary>
        public StepBase mainFlowRoot;

        /// <summary>The default fail handler root.</summary>
        public FailHandlerStep DefaultFailureHandler =>
            FailureHandlerSteps.FirstOrDefault(h => h != null && h.GUID == defaultFailHandlerGuid)
            ?? FailureHandlerSteps.FirstOrDefault(h => h != null && h.IsDefault);

        /// <summary>Assign the default handler guid (parser-internal).</summary>
        internal void SetDefaultFailHandlerGuid(string guid) => defaultFailHandlerGuid = guid;

        /// <summary>Create a fresh iterator over this data.</summary>
        public virtual GraphIterator GetIterator() => new GraphIterator(this);
    }

    /// <summary>Walks a <see cref="GraphData"/>: tracks current/visited steps and completion.</summary>
    [Serializable]
    public class GraphIterator
    {
        /// <summary>GUIDs of the currently active steps.</summary>
        public List<string> currentStepGuids = new List<string>();

        /// <summary>GUIDs of every visited step.</summary>
        public List<string> visitedStepGuids = new List<string>();

        /// <summary>The parsed data.</summary>
        public GraphData Data { get; private set; }

        /// <summary>Whether the flow has started.</summary>
        public bool Started { get; private set; }

        /// <summary>Currently active steps.</summary>
        public List<StepBase> CurrentSteps { get; private set; } = new List<StepBase>();

        /// <summary>Every visited step.</summary>
        public List<StepBase> VisitedNodes { get; private set; } = new List<StepBase>();

        /// <summary>Fired when a step completes.</summary>
        public event Action<StepBase> StepCompleted;

        /// <summary>Fired when the active step set changes.</summary>
        public event Action CurrentNodeChanged;

        private readonly List<Action<StepBase>> onStepEntered = new List<Action<StepBase>>();
        private readonly List<Action<StepBase>> onStepExited = new List<Action<StepBase>>();

        /// <summary>Create an iterator over data.</summary>
        public GraphIterator(GraphData graphData) => Data = graphData;

        /// <summary>Activate the root step.</summary>
        public void StartIterator()
        {
            Started = true;
            SetCurrentSteps(new List<StepBase> { Data.mainFlowRoot });
        }

        /// <summary>Restart from the root.</summary>
        public virtual void Restart()
        {
            Started = true;
            CurrentSteps.Clear();
            VisitedNodes.Clear();
            currentStepGuids.Clear();
            visitedStepGuids.Clear();
            SetCurrentSteps(new List<StepBase> { Data.mainFlowRoot });
        }

        /// <summary>Advance all current steps to their outputs.</summary>
        public void NextSteps()
        {
            var next = CurrentSteps.SelectMany(s => s?.OutputSteps ?? new List<StepBase>())
                .Where(s => s != null).Distinct().ToList();
            SetCurrentSteps(next);
        }

        /// <summary>Rewind all current steps to their inputs.</summary>
        public void PreviousSteps()
        {
            var prev = CurrentSteps.SelectMany(s => s?.InputSteps ?? new List<StepBase>())
                .Where(s => s != null).Distinct().ToList();
            SetCurrentSteps(prev);
        }

        /// <summary>Force the active step set.</summary>
        public virtual void SetCurrentSteps(List<StepBase> steps)
        {
            foreach (var s in CurrentSteps.Where(s => s != null))
            {
                s.OnExit();
                foreach (var cb in onStepExited)
                    cb(s);
            }

            CurrentSteps = steps ?? new List<StepBase>();
            currentStepGuids = CurrentSteps.Select(s => s?.GUID).ToList();

            foreach (var s in CurrentSteps.Where(s => s != null))
            {
                if (!visitedStepGuids.Contains(s.GUID))
                    visitedStepGuids.Add(s.GUID);
                if (!VisitedNodes.Contains(s))
                    VisitedNodes.Add(s);
                s.StepCompleted += OnStepCompleted;
                s.OnEnter();
                foreach (var cb in onStepEntered)
                    cb(s);
            }

            CurrentNodeChanged?.Invoke();
        }

        private void OnStepCompleted(StepBase step)
        {
            step.StepCompleted -= OnStepCompleted;
            StepCompleted?.Invoke(step);
            NextSteps();
        }

        /// <summary>Register a callback for step entry.</summary>
        public void AddOnStepEntered(Action<StepBase> callback)
        {
            if (callback != null && !onStepEntered.Contains(callback))
                onStepEntered.Add(callback);
        }

        /// <summary>Register a callback for step exit.</summary>
        public void AddOnStepExited(Action<StepBase> callback)
        {
            if (callback != null && !onStepExited.Contains(callback))
                onStepExited.Add(callback);
        }
    }

    /// <summary>Persisted flow state for save/rebind.</summary>
    [Serializable]
    public class FlowSavedData
    {
        /// <summary>Runtime type of the active flow.</summary>
        public Graph.SerializableSystemType ActiveFlowType;

        /// <summary>Saved normal flow.</summary>
        public NormalFlow NormalFlow;

        /// <summary>Currently active flow.</summary>
        [NonSerialized]
        public FlowBase ActiveFlow;

        /// <summary>Serialized form of <see cref="ActiveFlow"/>.</summary>
        public string ActiveFlowSerialized;

        /// <summary>Serialized form of the normal flow.</summary>
        public string NormalFlowSerialized;

        /// <summary>Runtime normal flow (non-serialized alias).</summary>
        [NonSerialized]
        public FlowBase RuntimeNormalFlow;

        /// <summary>Flag set by save/load.</summary>
        [NonSerialized]
        public bool FromSaveFile;

        /// <summary>After deserialization, rebuild non-serialized members.</summary>
        public void Rebind()
        {
            ActiveFlowType.TryRebindManagedType();
            if (NormalFlow != null)
                RuntimeNormalFlow = NormalFlow;
        }
    }

    /// <summary>A walkable flow (normal or fail-handler).</summary>
    [Serializable]
    public class FlowBase
    {
        /// <summary>The iterator driving this flow.</summary>
        public GraphIterator FlowIterator;

        /// <summary>Fired when the current step set changes.</summary>
        public event Action OnFlowChanged;

        /// <summary>Fired when the flow completes.</summary>
        public event Action OnFlowCompleted;

        /// <summary>Fired before the current step set changes (skipped steps).</summary>
        public event Action<StepGroup> PreCurrentNodeChanged;

        /// <summary>Display name.</summary>
        public virtual string Name { get; set; } = "Flow";

        /// <summary>Active steps.</summary>
        public virtual List<StepBase> CurrentSteps => FlowIterator?.CurrentSteps;

        /// <summary>Create a flow over data.</summary>
        public FlowBase(GraphData data) => FlowIterator = data?.GetIterator();

        /// <summary>Create a flow over an existing iterator.</summary>
        public FlowBase(GraphIterator iterator) => FlowIterator = iterator;

        /// <summary>Wire the iterator's change notification.</summary>
        public virtual void InitializeIterator()
        {
            if (FlowIterator != null)
                FlowIterator.CurrentNodeChanged += () => OnFlowChanged?.Invoke();
        }

        /// <summary>Start at the root.</summary>
        public virtual void Start()
        {
            FlowIterator?.StartIterator();
            OnFlowChanged?.Invoke();
        }

        /// <summary>Jump to a step by GUID.</summary>
        public virtual bool SkipToStep(string guid, bool instant = false)
        {
            var target = FlowIterator?.VisitedNodes?.FirstOrDefault(s => s.GUID == guid);
            if (target == null && FlowIterator != null)
            {
                var all = CollectAll(FlowIterator.Data?.mainFlowRoot);
                target = all.FirstOrDefault(s => s.GUID == guid);
            }
            if (target == null)
                return false;
            PreCurrentNodeChanged?.Invoke(new StepGroup(FlowIterator.CurrentSteps));
            FlowIterator.SetCurrentSteps(new List<StepBase> { target });
            return true;
        }

        /// <summary>Advance one step.</summary>
        public virtual void SkipToNext()
        {
            if (FlowIterator == null)
                return;
            PreCurrentNodeChanged?.Invoke(new StepGroup(FlowIterator.CurrentSteps));
            foreach (var s in FlowIterator.CurrentSteps.ToList())
                s?.SkipForwards();
            FlowIterator.NextSteps();
        }

        /// <summary>Go back one step.</summary>
        public virtual void SkipToBack()
        {
            if (FlowIterator == null)
                return;
            PreCurrentNodeChanged?.Invoke(new StepGroup(FlowIterator.CurrentSteps));
            foreach (var s in FlowIterator.CurrentSteps.ToList())
                s?.SkipBackwards();
            FlowIterator.PreviousSteps();
        }

        /// <summary>Mark the flow finished.</summary>
        public virtual void Complete() => OnFlowCompleted?.Invoke();

        /// <summary>Depth-first collection of the whole flow.</summary>
        public static List<StepBase> CollectAll(StepBase root)
        {
            var seen = new List<StepBase>();
            var stack = new Stack<StepBase>();
            if (root != null)
                stack.Push(root);
            while (stack.Count > 0)
            {
                var s = stack.Pop();
                if (s == null || seen.Contains(s))
                    continue;
                seen.Add(s);
                foreach (var o in s.OutputSteps)
                    if (o != null)
                        stack.Push(o);
            }
            return seen;
        }
    }

    /// <summary>The module's main flow.</summary>
    [Serializable]
    public class NormalFlow : FlowBase
    {
        /// <inheritdoc/>
        public override string Name { get; set; } = "Normal";

        /// <summary>Create over data.</summary>
        public NormalFlow(GraphData data) : base(data) { }

        /// <summary>Create over an iterator.</summary>
        public NormalFlow(GraphIterator it) : base(it) { }
    }

    /// <summary>A fail-handler flow; completing it resumes the normal flow.</summary>
    [Serializable]
    public class FailHandlerFlow : FlowBase
    {
        /// <summary>The fail-handler root step.</summary>
        public FailHandlerStep Handler;

        /// <summary>The flow this handler returns to.</summary>
        [NonSerialized]
        public NormalFlow ReturnFlow;

        /// <inheritdoc/>
        public override string Name => "FailHandler";

        /// <summary>Create over a handler root.</summary>
        public FailHandlerFlow(FailHandlerStep handler, NormalFlow returnFlow)
            : base(new GraphIterator(new GraphData { mainFlowRoot = handler }))
        {
            Handler = handler;
            ReturnFlow = returnFlow;
        }

        /// <summary>Complete: return to the normal flow.</summary>
        public override void Complete() => base.Complete();
    }

    /// <summary>Base for steps that run during a skip (invisible to the user).</summary>
    [Serializable]
    public abstract class SkippingStepBase : StepBase
    {
        /// <summary>Arbitrary serialized data.</summary>
        public object objectData;
    }

    /// <summary>A skipped step that must still be "completed" so downstream state is consistent.</summary>
    [Serializable]
    public class SkippingCompleteStep : SkippingStepBase
    {
        /// <summary>The wrapped step.</summary>
        public StepBase InternalStep;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            InternalStep?.OnEnter();
            InternalStep?.Complete();
        }
    }

    /// <summary>A skipped step that does nothing.</summary>
    [Serializable]
    public class SkippingOnlyStep : SkippingStepBase
    {
        /// <inheritdoc/>
        public override void OnEnter() { }
    }

    /// <summary>Resolves dynamic conditional steps at parse time.</summary>
    public class ConditionalStepResolver
    {
        /// <summary>All conditionals under management.</summary>
        public List<StepBase> ConditionalSteps = new List<StepBase>();

        /// <summary>Steps whose output depends on a mode.</summary>
        public List<StepBase> ModeDependentSteps = new List<StepBase>();

        /// <summary>Register a conditional.</summary>
        public void AddConditional(StepBase step)
        {
            if (step != null && !ConditionalSteps.Contains(step))
                ConditionalSteps.Add(step);
        }

        /// <summary>Register a mode-dependent step.</summary>
        public void AddModeDependentStep(StepBase step)
        {
            if (step != null && !ModeDependentSteps.Contains(step))
                ModeDependentSteps.Add(step);
        }

        /// <summary>Rewire conditionals for the current mode.</summary>
        public virtual void Resolve(GameMode mode)
        {
            // Dynamic conditionals resolve at runtime via ChooseOutput; nothing to rewire statically.
        }
    }
}
