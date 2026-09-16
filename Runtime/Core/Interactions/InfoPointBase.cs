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
    /// <summary>Abstract info-point: an openable/closable UI anchor bound to an <see cref="ObservableSubject"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public abstract class InfoPointBase : MonoBehaviour
    {
        /// <summary>Close sibling info points when this one opens.</summary>
        public bool ShouldCloseAllOtherPoints = true;

        private ObservableSubject observableSubject;

        [SerializeField]
        protected bool IsOpen = false;

        /// <summary>Raised when the interaction completes; argument is correctness.</summary>
        public event Action<bool> CompleteInteraction;

        /// <summary>Raised when the open state is reverted.</summary>
        public event Action OnReverted;

        /// <summary>See the interface/base contract.</summary>
        protected virtual void Awake()
        {
            observableSubject = GetComponent<ObservableSubject>();
        }

        /// <summary>Report a completed interaction.</summary>
        protected void OnInteractionCompleted(bool correct) => CompleteInteraction?.Invoke(correct);

        /// <summary>Open the point, optionally closing others.</summary>
        public virtual void Open()
        {
            if (ShouldCloseAllOtherPoints)
                CloseOtherInfoPoints();
            IsOpen = true;
        }

        /// <summary>Close the point.</summary>
        public virtual void Close() => IsOpen = false;

        /// <summary>Toggle open state; returns the new state.</summary>
        public bool Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
            return IsOpen;
        }

        private void CloseOtherInfoPoints()
        {
            foreach (var point in FindObjectsOfType<InfoPointBase>())
            {
                if (point != this && point.IsOpen)
                    point.Close();
            }
        }

        /// <summary>Undo a previous completion.</summary>
        protected virtual void RevertCompletion() => OnReverted?.Invoke();
    }




    /// <summary>Bool UnityEvent used by info-point/quiz components.</summary>
    [Serializable]
    public class UnityBoolEvent : UnityEvent<bool> { }


}
