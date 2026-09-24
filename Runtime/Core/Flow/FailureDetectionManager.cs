using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Flow.Exceptions;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Utility;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Marks an event args as convertible to a <see cref="FailExceptionBase"/>.</summary>
    public interface IFailExceptionConvertible
    {
        /// <summary>Build the matching fail exception for this event.</summary>
        FailExceptionBase ToFailException();
    }

    /// <summary>
    /// Watches all interaction events on the <see cref="EventBus"/> global subject and fails the
    /// module when an event occurs that no active fail exception allows. Port of Luminous
    /// FailureDetectionManager.
    /// </summary>
    public class FailureDetectionManager : IEventObserver
    {
        private static FailureDetectionManager instance;

        /// <summary>Shared instance.</summary>
        public static FailureDetectionManager Instance => instance ??= new FailureDetectionManager();

        /// <summary>Exceptions added at runtime via Add Global Exception actions.</summary>
        public List<FailExceptionBase> GlobalFailExceptions { get; } = new List<FailExceptionBase>();

        /// <summary>Exceptions removed at runtime via Remove Global Exception actions.</summary>
        public List<FailExceptionBase> GlobalFailures { get; } = new List<FailExceptionBase>();

        private readonly Dictionary<string, List<FailExceptionBase>> currentStepFailExceptions =
            new Dictionary<string, List<FailExceptionBase>>();

        private readonly Dictionary<string, List<FailExceptionBase>> persistentExceptionsHistory =
            new Dictionary<string, List<FailExceptionBase>>();

        private InteractionEventArgs failArgs;
        private bool waitingForFail;
        private int neverFailCounter;

        private static bool Active =>
            GraphFlowManager.InstanceExists && GraphFlowManager.Instance.CanFail;

        /// <summary>Begin observing events while <see cref="GraphFlowManager.CanFail"/>.</summary>
        public void StartDetecting()
        {
            if (Active)
                EventBus.Instance.Subscribe(EventBus.GlobalSubjectId, this);
        }

        /// <summary>Stop observing events.</summary>
        public void StopDetecting()
        {
            EventBus.Instance.Unsubscribe(EventBus.GlobalSubjectId, this);
            waitingForFail = false;
        }

        /// <summary>Register a step's authored fail exceptions (called on step entry / forward-skip entry).</summary>
        public void AddStepExceptions(StepExecutionBase step)
        {
            if (!Active || step == null)
                return;
            if (step.NeverFail)
            {
                neverFailCounter++;
                return;
            }
            if (!currentStepFailExceptions.ContainsKey(step.GUID))
                currentStepFailExceptions.Add(step.GUID, new List<FailExceptionBase>());
            if (persistentExceptionsHistory.TryGetValue(step.GUID, out var persisted))
                foreach (var ex in persisted)
                    if (!currentStepFailExceptions[step.GUID].Contains(ex))
                        currentStepFailExceptions[step.GUID].Add(ex);
            foreach (var ex in step.FailExceptions)
            {
                if (ex == null)
                    continue;
                if (!string.IsNullOrEmpty(ex.PersistGuid) && ex.PersistGuid != step.GUID)
                {
                    if (!currentStepFailExceptions.ContainsKey(ex.PersistGuid))
                        currentStepFailExceptions.Add(ex.PersistGuid, new List<FailExceptionBase>());
                    currentStepFailExceptions[ex.PersistGuid].Add(ex);
                    if (!persistentExceptionsHistory.ContainsKey(ex.PersistGuid))
                        persistentExceptionsHistory.Add(ex.PersistGuid, new List<FailExceptionBase>());
                    if (!persistentExceptionsHistory[ex.PersistGuid].Contains(ex))
                        persistentExceptionsHistory[ex.PersistGuid].Add(ex);
                }
                else
                {
                    currentStepFailExceptions[step.GUID].Add(ex);
                }
            }
        }

        /// <summary>Remove a step's exceptions (called on step exit / forward-skip exit).</summary>
        public void RemoveStepExceptions(StepExecutionBase step)
        {
            if (!Active || step == null)
                return;
            if (step.NeverFail)
                neverFailCounter--;
            else
                currentStepFailExceptions.Remove(step.GUID);
        }

        /// <summary>Add an exception to the global set; returns 0 inactive, 1 cancelled a failure, 2 added.</summary>
        public int AddGlobalException(FailExceptionBase exception)
        {
            if (!Active || exception == null)
                return 0;
            if (GlobalFailures.Any(ex => ex.ExactEquals(exception)))
            {
                GlobalFailures.RemoveAll(ex => ex.ExactEquals(exception));
                return 1;
            }
            GlobalFailExceptions.Add(exception);
            return 2;
        }

        /// <summary>Remove an exception from the global set; returns 0 inactive, 1 removed, 2 became a failure.</summary>
        public int RemoveGlobalException(FailExceptionBase exception)
        {
            if (!Active || exception == null)
                return 0;
            if (GlobalFailExceptions.Any(ex => ex.ExactEquals(exception)))
            {
                GlobalFailExceptions.RemoveAll(ex => ex.ExactEquals(exception));
                return 1;
            }
            GlobalFailures.Add(exception);
            return 2;
        }

        /// <inheritdoc/>
        public void OnEvent(InteractionEventArgs args)
        {
            if (!Active || waitingForFail || neverFailCounter > 0)
                return;
            if (args == null || args.IgnoreFailure || args.IsRemote)
                return;
            if (args.StepNumber != StepCounter.Current)
                return;
            if (args is not IFailExceptionConvertible convertible)
                return;
            if (HasException(convertible))
            {
                Log.Info($"[Fail] Fail exception allowed for {args}.", LogCategory.GameModeFailExceptions);
                return;
            }
            failArgs = args;
            waitingForFail = true;
            EventBus.Instance.OnAllObserversNotified += Fail;
        }

        private void Fail()
        {
            EventBus.Instance.OnAllObserversNotified -= Fail;
            waitingForFail = false;
            if (!Active)
                return;
            Log.Warning($"[Fail] Failure detected: {failArgs}", LogCategory.GameModeFailExceptions);
            if (GraphFlowManager.Instance?.CurrentSteps != null)
                foreach (var s in GraphFlowManager.Instance.CurrentSteps)
                    (s as StepExecutionBase)?.OnFail();
            GameModeManager.Fail(failArgs);
        }

        private bool HasException(IFailExceptionConvertible eventArgs)
        {
            var target = eventArgs.ToFailException();
            var stepExceptions = currentStepFailExceptions.SelectMany(kv => kv.Value).ToList();
            if (CheckExceptionInList(stepExceptions, target))
                return true;
            if (CheckExceptionInList(GlobalFailures, target))
                return false;
            return CheckExceptionInList(GlobalFailExceptions, target);
        }

        private static bool CheckExceptionInList(List<FailExceptionBase> exceptions, FailExceptionBase target)
        {
            var included = exceptions
                .Where(e => e != null && e.IsIncludedInMode(GameModeManager.CurrentMode)).ToList();
            if (included.Contains(target))
            {
                Log.Info($"[Fail] Found Fail Exception: {included.First(x => x.Equals(target))}",
                    LogCategory.GameModeFailExceptions);
                return true;
            }
            return included.Any(e => e.TypeException && e.GetType() == target.GetType());
        }
    }
}
