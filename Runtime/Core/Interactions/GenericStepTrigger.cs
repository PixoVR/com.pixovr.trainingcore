using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>UnityEvent bridge used by generic step nodes: fires inspector-wired callbacks on step lifecycle.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GenericStepTrigger : InteractableBase
    {
        /// <summary>Serialized subject reference (Luminous field name kept for migration).</summary>
        [SerializeField]
        private ObservableSubject subject;

        /// <summary>Fired when the owning step starts.</summary>
        public UnityEngine.Events.UnityEvent StepEntered;

        /// <summary>Fired when the owning step completes.</summary>
        public UnityEngine.Events.UnityEvent StepCompleted;

        /// <summary>Fired when the owning step is skipped forwards.</summary>
        public UnityEngine.Events.UnityEvent SkippedForwards;

        /// <summary>Fired when the owning step is skipped backwards.</summary>
        public UnityEngine.Events.UnityEvent SkippedBackwards;

        /// <summary>Publish a complete event to end the owning step.</summary>
        public void CompleteStep() => Publish(new GenericInteractionEventArgs(SubjectId, "Complete"));

        /// <summary>Fire the skipped-forwards callbacks.</summary>
        public void SkipForwards() => SkippedForwards?.Invoke();

        /// <summary>Fire the skipped-backwards callbacks.</summary>
        public void SkipBackwards() => SkippedBackwards?.Invoke();

        /// <summary>Invoke <see cref="StepEntered"/>.</summary>
        public void OnStepEntered() => StepEntered?.Invoke();

        /// <summary>Invoke <see cref="StepCompleted"/>.</summary>
        public void OnStepCompleted() => StepCompleted?.Invoke();

        /// <summary>Invoke <see cref="SkippedForwards"/>.</summary>
        public void OnSkippedForwards() => SkippedForwards?.Invoke();

        /// <summary>Invoke <see cref="SkippedBackwards"/>.</summary>
        public void OnSkippedBackwards() => SkippedBackwards?.Invoke();
    }
}
