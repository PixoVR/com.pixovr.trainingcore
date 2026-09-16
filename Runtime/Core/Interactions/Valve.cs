using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Valve interaction: delegates rotation to an <see cref="IValveBehaviour"/> and reports turn events.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Valve : InteractableBase
    {
        /// <summary>Fully open rotation (degrees).</summary>
        [HideInInspector]
        public float OpenRotation = 0f;

        /// <summary>Fully closed rotation (degrees).</summary>
        [HideInInspector]
        public float ClosedRotation = 360f;

        /// <summary>Current rotation (degrees).</summary>
        [HideInInspector]
        public float CurrentRotation = 0f;

        /// <summary>Lower bound of the fail padding band.</summary>
        [HideInInspector]
        public float MinFailPadding;

        /// <summary>Upper bound of the fail padding band.</summary>
        [HideInInspector]
        public float MaxFailPadding;

        /// <summary>Degrees around the current rotation tolerated before counting as failure.</summary>
        public float FailurePadding = 40f;

        /// <summary>Valve implementation; auto-fetched from a sibling component in Awake.</summary>
        public IValveBehaviour ValveBehaviour;

        /// <summary>Whether to use <see cref="CustomFailReason"/> instead of the default.</summary>
        public bool HasCustomFailReason;

        /// <summary>Custom fail reason text.</summary>
        public string CustomFailReason;

        /// <summary>Guid string id.</summary>
        public string Id => Subject != null ? Subject.Id : null;

        protected override void Awake()
        {
            base.Awake();
            if (ValveBehaviour == null)
            {
                ValveBehaviour = GetComponent<IValveBehaviour>();
                if (ValveBehaviour == null)
                    Utility.Log.Error("Valve requires a component implementing IValveBehaviour", LogCategory.Interaction);
            }
        }

        private void OnEnable() => SetFailurePadding();

        /// <summary>Called by the valve implementation when rotation changes.</summary>
        public void OnValveTurnEvent(float rotation)
        {
            CurrentRotation = rotation;
            Publish(new ValveTurnEventArgs(this, rotation, WithinFailPaddingRange(rotation),
                HasCustomFailReason ? CustomFailReason : null));
        }

        /// <summary>Set absolute rotation; <paramref name="inverse"/> requests an animated return.</summary>
        public void SetRotation(float value, bool inverse = false)
        {
            CurrentRotation = value;
            ValveBehaviour?.SetRotation(value, inverse);
        }

        /// <summary>Freeze or unfreeze the valve.</summary>
        public void SetFreeze(bool state) => ValveBehaviour?.SetFreeze(state);

        /// <summary>Recompute the fail padding band around the current rotation.</summary>
        public void SetFailurePadding()
        {
            MinFailPadding = CurrentRotation - FailurePadding;
            MaxFailPadding = CurrentRotation + FailurePadding;
        }

        private bool WithinFailPaddingRange(float value) => value > MinFailPadding && value < MaxFailPadding;
    }
}
