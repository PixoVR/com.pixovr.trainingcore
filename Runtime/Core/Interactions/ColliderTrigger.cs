using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Publishes a collision event when a matching collider enters/exits this trigger.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class ColliderTrigger : InteractableBase
    {
        /// <summary>Optional tag filter; empty accepts everything.</summary>
        public string TagFilter = string.Empty;

        private void OnTriggerEnter(Collider other)
        {
            if (Accepts(other))
                Publish(new CollisionInteractionEventArgs(Subject, other.gameObject, true));
        }

        private void OnTriggerExit(Collider other)
        {
            if (Accepts(other))
                Publish(new CollisionInteractionEventArgs(Subject, other.gameObject, false));
        }

        private bool Accepts(Collider other) =>
            string.IsNullOrEmpty(TagFilter) || other.CompareTag(TagFilter);
    }
}
