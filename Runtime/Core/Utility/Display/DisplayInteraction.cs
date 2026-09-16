using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Emits a <see cref="Events.DisplayInteractionEventArgs"/> when invoked (e.g. by a UI button).</summary>
    [RequireComponent(typeof(Events.ObservableSubject))]
    public class DisplayInteraction : MonoBehaviour
    {
        [SerializeField]
        private Events.ObservableSubject subject;

        private void Awake()
        {
            subject = GetComponent<Events.ObservableSubject>();
        }

        /// <summary>Record the interaction command and publish the event.</summary>
        public void OnInteractionEvent()
        {
            var args = new Events.DisplayInteractionEventArgs(subject != null ? subject.Id : null, null);
            var command = args.ToCommand();
            if (command != null)
                Commands.CommandHistory.Instance.Record(command);
            Events.EventBus.Instance.Publish(args.SubjectId, args);
        }
    }
}
