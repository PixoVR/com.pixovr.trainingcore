using System;
using UnityEngine;
using UnityEngine.Playables;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>One-shot completion watcher for a <see cref="PlayableDirector"/> (Hold wrap
    /// never raises <c>stopped</c>, so completion is polled like Luminous TimelineMiddleman).</summary>
    internal sealed class TimelineCompletionWatcher : MonoBehaviour
    {
        private PlayableDirector director;
        private Action onComplete;
        private bool fired;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            if (director != null)
                director.stopped += OnDirectorStopped;
        }

        private void OnDestroy()
        {
            if (director != null)
                director.stopped -= OnDirectorStopped;
        }

        /// <summary>Arm the watcher with a completion callback.</summary>
        public void Arm(Action callback)
        {
            onComplete = callback;
            fired = false;
        }

        /// <summary>Disarm the watcher.</summary>
        public void Disarm() => onComplete = null;

        private void LateUpdate()
        {
            if (director == null || onComplete == null || fired)
                return;
            if (director.extrapolationMode == DirectorWrapMode.Loop)
                return;
            if (director.state == PlayState.Playing && director.time + 0.001 >= director.duration)
                Fire();
        }

        private void OnDirectorStopped(PlayableDirector d)
        {
            if (fired || onComplete == null)
                return;
            Fire();
        }

        private void Fire()
        {
            fired = true;
            var cb = onComplete;
            onComplete = null;
            cb?.Invoke();
        }
    }
}
