using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>UnityEvent bridge for generic action nodes: Execute/Undo callbacks.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GenericActionTrigger : InteractableBase
    {
        /// <summary>Serialized subject reference (Luminous field name kept for migration).</summary>
        [SerializeField]
        private ObservableSubject subject;

        /// <summary>Fired when the owning action executes.</summary>
        public UnityEngine.Events.UnityEvent Execute;

        /// <summary>Fired when the owning action is undone.</summary>
        public UnityEngine.Events.UnityEvent Undo;

        /// <summary>Invoke <see cref="Execute"/>.</summary>
        public void Act() => Execute?.Invoke();

        /// <summary>Invoke <see cref="Undo"/>.</summary>
        public void OnUndo() => Undo?.Invoke();
    }
}
