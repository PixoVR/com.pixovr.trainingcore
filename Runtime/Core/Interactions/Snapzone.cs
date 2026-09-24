using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>A zone a <see cref="Snappable"/> snaps into; delegates to an <see cref="ISnapBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Snapzone : InteractableBase
    {
        /// <summary>Id referenced by snap steps.</summary>
        public int SnapzoneID = 0;

        /// <summary>Snap implementation; auto-fetched from a sibling component in Awake.</summary>
        public ISnapBehaviour SnapBehaviour;

        /// <summary>True while the zone is free.</summary>
        public bool IsFree => SnapBehaviour == null || SnapBehaviour.IsFree();

        protected override void Awake()
        {
            base.Awake();
            if (SnapBehaviour == null)
            {
                SnapBehaviour = GetComponent<ISnapBehaviour>();
                if (SnapBehaviour == null)
                    Utility.Log.Error("Snapzone requires a component implementing ISnapBehaviour", LogCategory.Interaction);
            }
        }

        /// <summary>The snappable currently occupying the zone, if any.</summary>
        public Snappable Occupant { get; private set; }

        /// <summary>Mark a pre-placed object as occupying the zone without publishing a snap event.</summary>
        public void SetStartingOccupant(GameObject snappedObject)
        {
            Occupant = snappedObject != null ? snappedObject.GetComponent<Snappable>() : null;
        }

        /// <summary>Entry point from the snap implementation when an object lands in the zone.</summary>
        public void OnObjectSnapped(GameObject snappedObject)
        {
            var snappable = snappedObject.GetComponent<Snappable>();
            if (snappable != null)
                Occupant = snappable;
            snappable?.OnSnappedObjectEvent(this);
        }

        /// <summary>Entry point from the snap implementation when the occupant leaves the zone.</summary>
        public void OnObjectUnsnapped(GameObject unsnappedObject)
        {
            var subject = unsnappedObject.GetComponent<ObservableSubject>();
            if (subject != null && subject.CurrentSnapzone == this)
                subject.CurrentSnapzone = null;
            if (Occupant != null && (unsnappedObject.GetComponent<Snappable>() == Occupant || Occupant.gameObject == unsnappedObject))
                Occupant = null;
        }

        /// <summary>Snap an object in; unsnaps it from any previous zone first.</summary>
        public void Snap(GameObject objectToSnap)
        {
            var subject = objectToSnap.GetComponent<ObservableSubject>();
            if (subject == null || SnapBehaviour == null)
                return;
            if (subject.CurrentSnapzone != null)
                subject.CurrentSnapzone.Unsnap(subject.gameObject);
            subject.CurrentSnapzone = this;
            SnapBehaviour.Snap(objectToSnap);
        }

        /// <summary>Release an object from this zone.</summary>
        public void Unsnap(GameObject gameObject)
        {
            var subject = gameObject.GetComponent<ObservableSubject>();
            if (subject != null && subject.CurrentSnapzone == this)
                subject.CurrentSnapzone = null;
            SnapBehaviour?.Unsnap(gameObject);
        }

        /// <summary>Whether the occupant can be pulled out beyond a detach range.</summary>
        public bool HasDetachRange() => SnapBehaviour != null && SnapBehaviour.HasDetachRange();
    }
}
