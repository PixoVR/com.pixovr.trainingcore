using System.Collections.Generic;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Events
{
    /// <summary>A captured transform used for undo/reset.</summary>
    public struct SavedTransform
    {
        /// <summary>World position.</summary>
        public Vector3 Position;
        /// <summary>World rotation.</summary>
        public Quaternion Rotation;
        /// <summary>Local scale.</summary>
        public Vector3 Scale;

        public SavedTransform(Transform t)
        {
            Position = t.position;
            Rotation = t.rotation;
            Scale = t.localScale;
        }

        /// <summary>Applies the stored transform to <paramref name="t"/>.</summary>
        public void ApplyTo(Transform t)
        {
            t.SetPositionAndRotation(Position, Rotation);
            t.localScale = Scale;
        }
    }

    /// <summary>
    /// Runtime identity + event target for every interactable object. The <see cref="Id"/> is the
    /// object's guid; it owns a <see cref="Subject"/> registered with <see cref="EventBus"/> while
    /// enabled, and a transform history used for undo.
    /// </summary>
    [RequireComponent(typeof(GuidComponent))]
    public sealed class ObservableSubject : MonoBehaviour
    {
        private Subject subject;

        /// <summary>Guid string identity of this object.</summary>
        public string Id => gameObject.GetGuidString();

        /// <summary>The observable subject registered with the event bus.</summary>
        public Subject Subject => subject;

        /// <summary>Transforms pushed during interactions (undo support).</summary>
        public Stack<SavedTransform> SubjectTransformHistory { get; private set; }

        /// <summary>The snapzone currently holding this object, if any.</summary>
        public Snapzone CurrentSnapzone { get; internal set; }

        private void Awake()
        {
            subject = new Subject(Id);
            SubjectTransformHistory = new Stack<SavedTransform>();
            SubjectTransformHistory.Push(new SavedTransform(transform));
        }

        private void OnEnable() => EventBus.Instance.Register(subject);

        private void OnDisable() => EventBus.Instance.Unregister(subject);

        /// <summary>Marks the zone this object starts in.</summary>
        public void SetStartingSnapzone(Snapzone snapzone) => CurrentSnapzone = snapzone;

        /// <summary>Push a transform snapshot onto the history.</summary>
        public void AddToTransformHistory(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SubjectTransformHistory.Push(new SavedTransform { Position = position, Rotation = rotation, Scale = scale });
        }

        /// <summary>Push the current transform onto the history.</summary>
        public void AddCurrentTransformToHistory() => SubjectTransformHistory.Push(new SavedTransform(transform));

        /// <summary>Drop the newest history entry.</summary>
        public void RemoveLastEntry() => SubjectTransformHistory.Pop();

        /// <summary>Pop and return the newest history entry.</summary>
        public SavedTransform GetLastEntry() => SubjectTransformHistory.Pop();
    }
}
