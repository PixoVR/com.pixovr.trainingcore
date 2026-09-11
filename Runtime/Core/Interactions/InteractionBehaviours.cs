using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Underlying grab implementation (XRI, custom physics, etc.) a <see cref="Grabbable"/> delegates to.</summary>
    public interface IGrabBehaviour
    {
        /// <summary>Force the object out of the grabber's hand.</summary>
        void ForceUngrab();
    }

    /// <summary>Underlying snap implementation a <see cref="Snapzone"/> delegates to.</summary>
    public interface ISnapBehaviour
    {
        /// <summary>Callback signature for <see cref="OnDetachRangeExit"/>.</summary>
        delegate void SnapEventHandler(ISnapBehaviour sender);

        /// <summary>Fired when a snapped object leaves the detach range.</summary>
        event SnapEventHandler OnDetachRangeExit;

        /// <summary>Snap the object into the zone.</summary>
        void Snap(GameObject snappedObject);

        /// <summary>Release the object from the zone.</summary>
        void Unsnap(GameObject snappedObject);

        /// <summary>True while the zone has no occupant.</summary>
        bool IsFree();

        /// <summary>True when the zone allows pulling an occupant out beyond a distance.</summary>
        bool HasDetachRange();

        /// <summary>Reposition an occupant to the snap pose.</summary>
        void PositionToSnapzone(GameObject snappedObject);
    }

    /// <summary>Underlying teleport implementation a <see cref="Teleporter"/> delegates to.</summary>
    public interface ITeleportBehaviour
    {
        /// <summary>Perform the teleport.</summary>
        void Teleport();

        /// <summary>Undo the teleport (return to previous pose).</summary>
        void Unexecute();
    }

    /// <summary>Underlying valve implementation a <see cref="Valve"/> delegates to.</summary>
    public interface IValveBehaviour
    {
        /// <summary>Set the valve's absolute rotation in degrees; <paramref name="inverse"/> animates back.</summary>
        void SetRotation(float value, bool inverse = true);

        /// <summary>Freeze/unfreeze the valve.</summary>
        void SetFreeze(bool state);
    }
}
