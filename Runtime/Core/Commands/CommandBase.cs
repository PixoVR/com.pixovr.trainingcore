using System;
using PixoVR.TrainingCore.Flow;
using UnityEngine;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Base command: stamps <see cref="StepId"/> from <see cref="StepCounter.Current"/> at creation.</summary>
    [Serializable]
    public abstract class CommandBase : ICommand
    {
        
        public int StepId { get; private set; }

        
        public string SubjectId { get; protected set; }

        /// <summary>Id of the target this command acts on (object guid; may differ from subject).</summary>
        public string TargetId;

        protected CommandBase() : this(string.Empty) { }

        protected CommandBase(string subjectId)
        {
            SubjectId = subjectId;
            StepId = StepCounter.Current;
        }

        
        public abstract void Execute();

        
        public abstract void Unexecute();

        /// <summary>Optional per-frame update while the command is active.</summary>
        public virtual void Update() { }

        /// <summary>A command that does the exact opposite, when one exists.</summary>
        public virtual CommandBase GetInverse() => null;
    }
}
