using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Tap interaction: fires repeated tap events while held past the configured minimum duration.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Tappable : InteractableBase
    {
        private bool isTouched;
        private float duration;
        private float remainingTime;
        private const float invokeRepetitionRate = 0.3f;

        private void OnEnable() => GameModes.GameModeManager.OnFail += OnFail;

        private void OnDisable() => GameModes.GameModeManager.OnFail -= OnFail;

        private void OnFail(List<Flow.StepBase> steps, string reason, int handlerIndex) => OnEndTap();

        private void Update()
        {
            if (!isTouched)
                return;
            duration += Time.deltaTime;
            remainingTime -= Time.deltaTime;
            if (duration >= (TrainingConfig.Instance?.MinTapDuration ?? 0f) && remainingTime <= 0f)
            {
                Publish(new TapInteractionEventArgs(Subject, duration));
                remainingTime = invokeRepetitionRate;
            }
        }

        /// <summary>Begin a tap (called by the input layer).</summary>
        public void OnStartTap()
        {
            if (isTouched)
                return;
            isTouched = true;
            remainingTime = 0f;
            duration = 0f;
        }

        /// <summary>End the tap.</summary>
        public void OnEndTap() => isTouched = false;
    }
}
