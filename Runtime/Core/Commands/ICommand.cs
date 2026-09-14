using UnityEngine;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>An undoable world-change produced by an interaction event.</summary>
    public interface ICommand
    {
        /// <summary>The step number this command belongs to (for grouped undo).</summary>
        int StepId { get; }

        /// <summary>Subject id the command acts on (may be empty for global commands).</summary>
        string SubjectId { get; }

        /// <summary>Apply the change.</summary>
        void Execute();

        /// <summary>Revert the change.</summary>
        void Unexecute();
    }
}
