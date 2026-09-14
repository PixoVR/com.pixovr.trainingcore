using System;
using System.Collections.Generic;

namespace PixoVR.TrainingCore.Events
{
    /// <summary>
    /// Central event bus. Routes events to per-subject observers plus an optional "global"
    /// subject, keeps a history of published events, and queues events suppressed from the
    /// network while a session is syncing (<see cref="UnsyncedEvents"/>).
    /// </summary>
    public sealed class EventBus
    {
        private static EventBus instance;

        /// <summary>Process-wide bus instance.</summary>
        public static EventBus Instance => instance ??= new EventBus();

        /// <summary>Well-known subject id that receives a copy of every global publish.</summary>
        public const string GlobalSubjectId = "GlobalSubject";

        private readonly Dictionary<string, Subject> subjects = new Dictionary<string, Subject>();

        private readonly List<InteractionEventArgs> history = new List<InteractionEventArgs>();

        /// <summary>Every event ever published through this bus (in order).</summary>
        public IReadOnlyList<InteractionEventArgs> History => history;

        /// <summary>Events published with <c>toNetwork: false</c> — replayed after a network sync.</summary>
        public List<InteractionEventArgs> UnsyncedEvents { get; } = new List<InteractionEventArgs>();

        /// <summary>Fired after an event is published when <c>toNetwork</c> was true — the NetworkManager broadcasts from this.</summary>
        public event Action<InteractionEventArgs> OnPublished;

        /// <summary>Register a subject so observers can subscribe by id.</summary>
        public void Register(Subject subject)
        {
            if (subject != null)
                subjects[subject.Id] = subject;
        }

        /// <summary>Unregister a subject (observers stop receiving its events).</summary>
        public void Unregister(Subject subject)
        {
            if (subject != null)
                subjects.Remove(subject.Id);
        }

        /// <summary>Unregister by id.</summary>
        public void Unregister(string subjectId) => subjects.Remove(subjectId);

        /// <summary>Subscribe an observer to a subject id; creates the subject entry if missing.</summary>
        public void Subscribe(string subjectId, IEventObserver observer)
        {
            if (!subjects.TryGetValue(subjectId, out var subject))
            {
                subject = new Subject(subjectId);
                subjects[subjectId] = subject;
            }
            subject.Attach(observer);
        }

        /// <summary>Unsubscribe an observer from a subject id.</summary>
        public void Unsubscribe(string subjectId, IEventObserver observer)
        {
            if (subjects.TryGetValue(subjectId, out var subject))
                subject.Detach(observer);
        }

        /// <summary>
        /// Publish an event: notify the subject's observers, optionally the global subject,
        /// record history, queue for network sync when suppressed, and fire <see cref="OnPublished"/>
        /// when the event should go to the network.
        /// </summary>
        public void Publish(string subjectId, InteractionEventArgs args, bool alsoGlobal = true, bool toNetwork = true, bool isSyncReplay = false)
        {
            if (args == null)
                return;
            if (string.IsNullOrEmpty(args.SubjectId))
                args.SubjectId = subjectId;

            history.Add(args);

            if (subjects.TryGetValue(subjectId, out var subject))
                subject.Notify(args);

            if (alsoGlobal && subjectId != GlobalSubjectId && subjects.TryGetValue(GlobalSubjectId, out var global))
                global.Notify(args);

            if (!toNetwork && !isSyncReplay)
                UnsyncedEvents.Add(args);
            else
                OnPublished?.Invoke(args);
        }

        /// <summary>Clear subjects, history and the unsynced queue.</summary>
        public void Reset()
        {
            subjects.Clear();
            history.Clear();
            UnsyncedEvents.Clear();
            OnPublished = null;
        }
    }
}
