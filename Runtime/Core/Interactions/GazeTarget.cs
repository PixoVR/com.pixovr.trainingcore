using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Gaze interaction target: accumulates gaze time and fires events at thresholds.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class GazeTarget : InteractableBase
    {
        /// <summary>Seconds of gaze before the first event fires.</summary>
        public float ContinuousDuration = 1f;

        /// <summary>Reset accumulated gaze time on look-away (Luminous DurationSettings.IsContinuous).</summary>
        public bool ResetGazeOnExit = true;

        private bool gazing;
        private float gazeTime;
        private float publishTimer;

        private void Update()
        {
            if (!gazing)
                return;
            gazeTime += Time.deltaTime;
            publishTimer += Time.deltaTime;
            if (publishTimer >= 0.2f)
            {
                publishTimer = 0f;
                Publish(new GazeInteractionEventArgs(Subject, gazeTime));
            }
        }

        /// <summary>Begin gaze.</summary>
        public void OnGazeEnter()
        {
            gazing = true;
        }

        /// <summary>End gaze.</summary>
        public void OnGazeExit()
        {
            gazing = false;
            if (ResetGazeOnExit)
                gazeTime = 0f;
        }
    }
}
