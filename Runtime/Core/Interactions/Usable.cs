using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.GameModes;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>"Use" interaction: publishes <see cref="UseInteractionEventArgs"/> at a fixed rate while held.</summary>
    [AddComponentMenu("TrainingCore/Interactions/Usable")]
    [RequireComponent(typeof(ObservableSubject))]
    public class Usable : InteractableBase
    {
        /// <summary>Colliders in this mask are ignored as use locations.</summary>
        [SerializeField]
        private LayerMask ignoreUseMask;

        /// <summary>Fired when a use is undone.</summary>
        public UnityEvent OnUndo;

        /// <summary>Seconds between published use events.</summary>
        public float InvokeEventsRepeatRate = 0.05f;

        /// <summary>True while the object is being used.</summary>
        public bool Using { get; private set; }

        private Collider usedOn;
        private float startUseTime;

        private void OnEnable() => GameModeManager.OnFail += OnFailHandler;

        private void OnDisable()
        {
            GameModeManager.OnFail -= OnFailHandler;
            if (Using)
                OnStopUsing();
        }

        private void OnFailHandler(System.Collections.Generic.List<Flow.StepBase> currentSteps, string failureReason, int handlerIndex)
        {
            if (Using)
                OnStopUsing();
        }

        /// <summary>Begin using, optionally on a target collider.</summary>
        public void OnStartUsing(Collider location = null)
        {
            if (location != null && IsInIgnoreMask(location))
                return;
            if (Using)
                OnStopUsing();
            usedOn = location;
            Using = true;
            startUseTime = Time.time;
            InvokeRepeating(nameof(InvokeUseEvent), 0f, InvokeEventsRepeatRate);
        }

        /// <summary>Stop using.</summary>
        public void OnStopUsing()
        {
            Using = false;
            usedOn = null;
            CancelInvoke();
        }

        private void InvokeUseEvent()
        {
            Publish(new UseInteractionEventArgs(Subject, usedOn != null ? usedOn.transform : null,
                Time.time - startUseTime));
        }

        private bool IsInIgnoreMask(Collider other) =>
            (ignoreUseMask.value & (1 << other.gameObject.layer)) != 0;

        /// <summary>Undo a use (fires <see cref="OnUndo"/>).</summary>
        public void UndoUse(UseInteractionEventArgs args) => OnUndo?.Invoke();
    }
}
