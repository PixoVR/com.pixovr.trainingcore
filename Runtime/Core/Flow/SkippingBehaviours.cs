using System;
using System.Linq;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Base for skip strategies driven off a <see cref="GraphIterator"/>.</summary>
    public abstract class SkippingBehaviourBase
    {
        /// <summary>The iterator being walked.</summary>
        protected GraphIterator iterator;

        /// <summary>Create over an iterator.</summary>
        public SkippingBehaviourBase(GraphIterator iterator) => this.iterator = iterator;

        /// <summary>True when any current step is a skip point.</summary>
        protected bool ReachedSkipPoint() => iterator?.CurrentSteps?.Any(s => s != null && s.IsSkipPoint) == true;

        /// <summary>Hook before a skip begins.</summary>
        protected virtual void OnStartSkip() { }

        /// <summary>Skip a single step boundary.</summary>
        public abstract void SkipOneStep(Action onComplete = null);

        /// <summary>Skip until the current main step number reaches <paramref name="targetStepNumber"/>.</summary>
        public abstract void SkipUntil(int targetStepNumber, Action onComplete = null);
    }

    /// <summary>Forwards skipping: runs each step's forwards-skip hooks then advances.</summary>
    public class ForwardSkippingBehaviour : SkippingBehaviourBase
    {
        /// <summary>Create over an iterator.</summary>
        public ForwardSkippingBehaviour(GraphIterator iterator) : base(iterator) { }

        /// <inheritdoc/>
        public override void SkipOneStep(Action onComplete = null)
        {
            if (iterator == null)
                return;
            OnStartSkip();
            foreach (var s in iterator.CurrentSteps.ToList())
                s?.SkipForwards();
            iterator.NextSteps();
            onComplete?.Invoke();
        }

        /// <inheritdoc/>
        public override void SkipUntil(int targetStepNumber, Action onComplete = null)
        {
            int guard = 0;
            while (iterator?.CurrentSteps?.Count > 0 &&
                   iterator.CurrentSteps.Max(s => s?.GetMainStepNumber() ?? 0) < targetStepNumber &&
                   guard++ < 1000)
                SkipOneStep();
            onComplete?.Invoke();
        }
    }

    /// <summary>Backwards skipping: runs backwards-skip hooks, rewinds, and can undo step actions.</summary>
    public class BackwardSkippingBehaviour : SkippingBehaviourBase
    {
        /// <summary>Create over an iterator.</summary>
        public BackwardSkippingBehaviour(GraphIterator iterator) : base(iterator) { }

        /// <inheritdoc/>
        public override void SkipOneStep(Action onComplete = null)
        {
            if (iterator == null)
                return;
            OnStartSkip();
            foreach (var s in iterator.CurrentSteps.ToList())
                s?.SkipBackwards();
            iterator.PreviousSteps();
            onComplete?.Invoke();
        }

        /// <summary>Undo every action of the current steps.</summary>
        public void UndoCurrentSteps()
        {
            foreach (var s in iterator?.CurrentSteps ?? new System.Collections.Generic.List<StepBase>())
                if (s is StepExecutionBase exec)
                    foreach (var action in exec.GetAllActions())
                        action?.Undo();
        }

        /// <inheritdoc/>
        public override void SkipUntil(int targetStepNumber, Action onComplete = null)
        {
            int guard = 0;
            while (iterator?.CurrentSteps?.Count > 0 &&
                   iterator.CurrentSteps.Min(s => s?.GetMainStepNumber() ?? 0) > targetStepNumber &&
                   guard++ < 1000)
                SkipOneStep();
            onComplete?.Invoke();
        }
    }
}
