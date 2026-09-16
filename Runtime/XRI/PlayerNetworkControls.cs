using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>Player interaction toggle driven by the step flow (replaces PlayerNetworkControlsOpenXR).</summary>
    public class PlayerNetworkControls : MonoBehaviour, IPlayerInteractionControls
    {
        /// <summary>Direct interactors enabled/disabled together.</summary>
        public List<XRDirectInteractor> DirectInteractorsToToggle = new List<XRDirectInteractor>();

        /// <summary>GameObjects enabled/disabled together.</summary>
        public List<GameObject> ObjectsToToggle = new List<GameObject>();

        /// <summary>Fired when interaction state changes.</summary>
        public Interactions.UnityBoolEvent OnSetInteractionState = new Interactions.UnityBoolEvent();

        /// <summary>Enable or disable player interaction.</summary>
        public virtual void SetInteractionState(bool state)
        {
            foreach (var interactor in DirectInteractorsToToggle)
                if (interactor != null)
                    interactor.enabled = state;
            foreach (var obj in ObjectsToToggle)
                if (obj != null)
                    obj.SetActive(state);
            OnSetInteractionState?.Invoke(state);
        }
    }
}
