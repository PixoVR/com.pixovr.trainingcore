using System;
using System.Collections.Generic;

namespace PixoVR.TrainingCore.Events
{
    /// <summary>Marker interface for anything that wants to observe a subject's events.</summary>
    public interface IEventObserver
    {
        /// <summary>Called with each event published to the observed subject.</summary>
        void OnEvent(InteractionEventArgs args);
    }

    /// <summary>An observable target identified by a guid string; holds the observer list.</summary>
    public sealed class Subject
    {
        /// <summary>Guid string identifying this subject.</summary>
        public string Id { get; }

        private readonly List<IEventObserver> observers = new List<IEventObserver>();

        /// <summary>Current observers (read-only view).</summary>
        public IReadOnlyList<IEventObserver> Observers => observers;

        /// <summary>Raised inside <see cref="Notify"/> for each event delivered.</summary>
        public event Action<InteractionEventArgs> OnNotified;

        public Subject(string id)
        {
            Id = id;
        }

        /// <summary>Attach an observer.</summary>
        public void Attach(IEventObserver observer)
        {
            if (observer != null && !observers.Contains(observer))
                observers.Add(observer);
        }

        /// <summary>Detach an observer.</summary>
        public void Detach(IEventObserver observer) => observers.Remove(observer);

        /// <summary>Deliver an event to all attached observers.</summary>
        public void Notify(InteractionEventArgs args)
        {
            OnNotified?.Invoke(args);
            for (int i = observers.Count - 1; i >= 0; i--)
                observers[i].OnEvent(args);
        }
    }
}
