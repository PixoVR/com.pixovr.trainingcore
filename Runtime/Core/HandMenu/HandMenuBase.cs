using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Utility.Display;
using UnityEngine;

namespace PixoVR.TrainingCore.HandMenu
{
    /// <summary>Abstract hand-menu controller driving a <see cref="Displayer"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public abstract class HandMenuBase : MonoBehaviour
    {
        [SerializeField]
        protected Displayer displayer;

        /// <summary>Event subject for this menu.</summary>
        public ObservableSubject Subject;

        /// <summary>Whether the menu is open.</summary>
        public bool isOpen { get; set; }

        /// <summary>The active hand-menu instance.</summary>
        public static HandMenuBase Instance { get; set; }

        /// <summary>This menu's id (the subject id).</summary>
        public string ID => Subject != null ? Subject.Id : null;

        protected virtual void Awake()
        {
            if (Subject == null)
                Subject = GetComponent<ObservableSubject>();
            Instance = this;
        }

        /// <summary>Set the open state.</summary>
        public void SetStateTo(bool open)
        {
            isOpen = open;
            if (open)
                OpenMenu();
            else
                CloseMenu();
            OnHandMenuStateChanged();
        }

        /// <summary>Toggle the open state.</summary>
        public void Toggle() => SetStateTo(!isOpen);

        /// <summary>Open the menu.</summary>
        public virtual void OpenMenu() => isOpen = true;

        /// <summary>Revert the open state without invoking open/close again.</summary>
        public void UndoStateTo(bool state) => isOpen = state;

        /// <summary>Called after the open state changes.</summary>
        public virtual void OnHandMenuStateChanged() { }

        /// <summary>Close the menu.</summary>
        public virtual void CloseMenu() => isOpen = false;

        /// <summary>Push display data to the menu displayer.</summary>
        public void SetText(DisplayData displayData)
        {
            if (displayer != null)
                displayer.DisplayTextData(displayData);
        }

        /// <summary>The data currently shown.</summary>
        public DisplayData GetDisplayedData() => displayer != null ? displayer.GetDisplayedData() : null;

        /// <summary>Show or hide the body text element.</summary>
        public void SetBodyTextState(bool state)
        {
            if (displayer != null)
                displayer.SetBodyTextState(state);
        }
    }
}
