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
    /// <summary>Tracks <see cref="LostObject"/>s and resets them when they fall out of range (replaces LostObjectManager).</summary>
    public class LostObjectManager : Utility.SingletonBehaviour<LostObjectManager>
    {
        /// <summary>Vertical offset below the manager that counts as out of range.</summary>
        public float VerticalResetOffsetDistance = -3f;

        /// <summary>Tracked objects.</summary>
        public List<LostObject> TrackedObjects { get; } = new List<LostObject>();

        /// <summary>Register an object for fall-reset tracking.</summary>
        public void TrackObject(LostObject obj)
        {
            if (obj != null && !TrackedObjects.Contains(obj))
                TrackedObjects.Add(obj);
        }

        /// <summary>Remove an object from tracking.</summary>
        public void UntrackObject(LostObject obj) => TrackedObjects.Remove(obj);

        /// <summary>Test hook: process tracked objects once.</summary>
        public void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Process tracked objects for <paramref name="deltaTime"/> seconds (separated for testability).</summary>
        public void Tick(float deltaTime)
        {
            foreach (var obj in TrackedObjects)
            {
                if (obj == null || obj.IsResetting)
                    continue;
                if (obj.ResetWhenOutOfRange &&
                    obj.transform.position.y < transform.position.y + VerticalResetOffsetDistance - obj.OutOfRangeDistance)
                {
                    obj.ResetObject();
                    continue;
                }
                if (obj.ResetWhenDropped && !obj.IsHeld && obj.HasMoved && !obj.IsSanpped)
                {
                    obj.DroppedResetTimer += deltaTime;
                    if (obj.DroppedResetTimer >= obj.DroppedWaitTime)
                        obj.ResetObject();
                }
                else
                {
                    obj.DroppedResetTimer = 0f;
                }
            }
        }
    }
}
