using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>
    /// Base for interaction middlemen: bridges a concrete <c>I*Behaviour</c> implementation to the
    /// <see cref="EventBus"/> and <see cref="CommandHistory"/>.
    /// </summary>
    [RequireComponent(typeof(ObservableSubject))]
    public abstract class InteractableBase : MonoBehaviour
    {
        /// <summary>The subject identity of this interactable.</summary>
        protected ObservableSubject Subject { get; private set; }

        /// <summary>Guid string id shortcut.</summary>
        public string SubjectId => gameObject.GetGuidString();

        protected virtual void Awake()
        {
            Subject = GetComponent<ObservableSubject>();
            if (Subject == null)
                Subject = gameObject.AddComponent<ObservableSubject>();
        }

        /// <summary>Record the event's command (if any) then publish the event on the bus.</summary>
        protected void Publish(InteractionEventArgs args, bool alsoGlobal = true, bool toNetwork = true)
        {
            var command = args?.ToCommand();
            if (command != null)
                CommandHistory.Instance.Record(command);
            EventBus.Instance.Publish(Subject.Id, args, alsoGlobal, toNetwork);
        }
    }
}
