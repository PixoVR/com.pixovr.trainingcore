using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Utility;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>
    /// Executed/undone command stacks. <see cref="UndoStep(int)"/> groups by <see cref="ICommand.StepId"/>.
    /// Consecutive <see cref="UseObjectCommand"/>s on the same subject+location merge (keep the longest).
    /// </summary>
    public sealed class CommandHistory
    {
        private static CommandHistory instance;

        /// <summary>Process-wide history.</summary>
        public static CommandHistory Instance => instance ??= new CommandHistory();

        private readonly Stack<ICommand> executed = new Stack<ICommand>();
        private readonly Stack<ICommand> undone = new Stack<ICommand>();

        /// <summary>Commands on the executed stack (newest last).</summary>
        public IReadOnlyCollection<ICommand> Executed => executed;

        /// <summary>Commands on the redo stack.</summary>
        public IReadOnlyCollection<ICommand> Undone => undone;

        /// <summary>Fired after a command is recorded.</summary>
        public event Action<ICommand> OnRecorded;

        /// <summary>Fired when undo/redo crosses a step boundary.</summary>
        public event Action<int> OnStepChanged;

        /// <summary>Execute a command and record it.</summary>
        public void ExecuteAndRecord(ICommand command)
        {
            if (command == null)
                return;
            command.Execute();
            Record(command);
        }

        /// <summary>Print the executed history, oldest first.</summary>
        public string PrintHistory()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in executed.Reverse())
                sb.AppendLine($"[{c.StepId}] {c.GetType().Name} {c}");
            var text = sb.ToString();
            Log.Info(text, LogCategory.GameManagerLogic);
            return text;
        }

        /// <summary>Record a command that was already executed.</summary>
        public void Record(ICommand command)
        {
            if (command == null)
            {
                Log.Error("Trying to record a null command", LogCategory.GameManagerLogic);
                return;
            }

            if (executed.Count > 0 && command is UseObjectCommand next && executed.Peek() is UseObjectCommand prev
                && prev.UsedSubject == next.UsedSubject && prev.InteractionLocation == next.InteractionLocation
                && prev.Duration < next.Duration)
            {
                executed.Pop();
            }

            executed.Push(command);
            OnRecorded?.Invoke(command);
        }

        /// <summary>True while an undo is executing (events published during it are marked <see cref="Events.InteractionEventArgs.IgnoreFailure"/>).</summary>
        public bool IsUndoing { get; private set; }

        /// <summary>Undo the most recent command.</summary>
        public void Undo()
        {
            if (!executed.Any())
            {
                Log.Error("No commands to undo", LogCategory.GameManagerLogic);
                return;
            }
            var c = executed.Pop();
            IsUndoing = true;
            try
            {
                c.Unexecute();
            }
            finally
            {
                IsUndoing = false;
            }
            undone.Push(c);
            OnStepChanged?.Invoke(c.StepId);
        }

        /// <summary>Undo all commands of the most recent step.</summary>
        public void UndoLastStep()
        {
            if (executed.Any())
                UndoStep(executed.Peek().StepId);
        }

        /// <summary>Undo every command whose StepId equals <paramref name="stepId"/>.</summary>
        public void UndoStep(int stepId)
        {
            while (executed.Any() && executed.Peek().StepId == stepId)
                Undo();
        }

        /// <summary>Undo until (and including) the given step is reached.</summary>
        public void UndoUntil(int stepId)
        {
            while (executed.Any() && executed.Peek().StepId != stepId)
                UndoStep(executed.Peek().StepId);
        }

        /// <summary>Re-execute the most recently undone command.</summary>
        public void Redo()
        {
            if (!undone.Any())
                return;
            var c = undone.Pop();
            c.Execute();
            executed.Push(c);
            OnStepChanged?.Invoke(c.StepId);
        }

        /// <summary>Redo all undone commands sharing the top step id.</summary>
        public void RedoStep()
        {
            if (!undone.Any())
                return;
            int stepId = undone.Peek().StepId;
            do
            {
                Redo();
            } while (undone.Any() && undone.Peek().StepId == stepId);
        }

        /// <summary>Clear both stacks.</summary>
        public void Reset()
        {
            executed.Clear();
            undone.Clear();
        }
    }
}
