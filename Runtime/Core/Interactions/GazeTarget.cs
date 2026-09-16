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

        private bool gazing;
        private float gazeTime;
        private bool fired;

        private void Update()
        {
            if (!gazing || fired)
                return;
            gazeTime += Time.deltaTime;
            if (gazeTime >= ContinuousDuration)
            {
                fired = true;
                Publish(new GazeInteractionEventArgs(Subject, gazeTime));
            }
        }

        /// <summary>Begin gaze.</summary>
        public void OnGazeEnter()
        {
            gazing = true;
            gazeTime = 0f;
            fired = false;
        }

        /// <summary>End gaze.</summary>
        public void OnGazeExit()
        {
            gazing = false;
            gazeTime = 0f;
            fired = false;
        }
    }
}
