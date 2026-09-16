using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Grab interaction: delegates physics to an <see cref="IGrabBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    [RequireComponent(typeof(Snappable))]
    public class Grabbable : InteractableBase
    {
        /// <summary>Grab implementation; auto-fetched from a sibling component in Awake.</summary>
        public IGrabBehaviour GrabBehaviour;

        protected override void Awake()
        {
            if (GrabBehaviour == null)
            {
                GrabBehaviour = GetComponent<IGrabBehaviour>();
                if (GrabBehaviour == null)
                    LogOrError("Grab interaction requires a component implementing IGrabBehaviour");
            }
            base.Awake();
        }

        /// <summary>Called by the grab implementation when the object is grabbed.</summary>
        public void OnGrabbedObjectEvent() => Publish(new GrabInteractionEventArgs(Subject));

        /// <summary>Force release.</summary>
        public void Ungrab() => GrabBehaviour?.ForceUngrab();

        private void LogOrError(string message) => Utility.Log.Error(message, LogCategory.Interaction);
    }

    /// <summary>Static registry of all <see cref="Snappable"/> components (for unique SnapId assignment).</summary>
    public static class SnappableRegistry
    {
        /// <summary>All known snappables in the loaded scenes.</summary>
        public static readonly List<Snappable> SnappableList = new List<Snappable>();

        /// <summary>Register a snappable.</summary>
        public static void Add(Snappable s)
        {
            if (s != null && !SnappableList.Contains(s))
                SnappableList.Add(s);
        }

        /// <summary>Unregister a snappable.</summary>
        public static void Remove(Snappable s) => SnappableList.Remove(s);
    }









}
