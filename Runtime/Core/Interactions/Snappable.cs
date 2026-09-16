using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Marks an object as snappable into a <see cref="Snapzone"/>. Serialized <see cref="SnapId"/> matches the Luminous field name.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ObservableSubject))]
    public class Snappable : InteractableBase
    {
        /// <summary>Id referenced by snap-type graph steps.</summary>
        public int SnapId;

        /// <summary>Fired (editor-time) whenever a SnapId may have changed.</summary>
        public static Action<Snappable> OnSnapIdChanged;

        protected override void Awake()
        {
            if (Application.isPlaying)
                base.Awake();
        }

        private void OnValidate()
        {
            OnSnapIdChanged?.Invoke(this);
            SnappableRegistry.Add(this);
        }

        private void Reset()
        {
            SnapId = GetUniqueSnapId();
            SnappableRegistry.Add(this);
        }

        private void OnDestroy() => SnappableRegistry.Remove(this);

        /// <summary>Returns the lowest unused SnapId in the registry.</summary>
        public int GetUniqueSnapId()
        {
            var list = SnappableRegistry.SnappableList;
            if (list.Count <= 0)
                return 1;
            int candidate = 1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != this && list[i].SnapId != candidate && list[i].SnapId != ++candidate)
                    return candidate;
            }
            return list[list.Count - 1].SnapId + 1;
        }

        /// <summary>Called by a <see cref="Snapzone"/> when this object snapped in.</summary>
        public void OnSnappedObjectEvent(Snapzone snapzone)
        {
            Publish(new SnapInteractionEventArgs(Subject, this, snapzone), alsoGlobal: true, toNetwork: false);
            Subject.CurrentSnapzone = snapzone;
        }
    }
}
