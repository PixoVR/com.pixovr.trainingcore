using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Identity;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Simple info point completed by a key press or explicit call.</summary>
    public class InfoPointCoreImplementation : InfoPointBase
    {
        /// <summary>Complete as correct rather than incorrect.</summary>
        public bool CompleteCorrectly = true;

        /// <summary>Keyboard shortcut that completes the interaction.</summary>
        public KeyCode KeyToPress;

        private void Update()
        {
            if (KeyToPress != KeyCode.None && Input.GetKeyDown(KeyToPress))
                OnInteractionCompleted(CompleteCorrectly);
        }

        /// <inheritdoc/>
        public override void Close() => base.Close();
    }
}
