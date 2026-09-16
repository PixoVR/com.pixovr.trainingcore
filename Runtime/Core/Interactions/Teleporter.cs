using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Teleport target: delegates the actual teleport to an <see cref="ITeleportBehaviour"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public class Teleporter : InteractableBase
    {
        private ITeleportBehaviour teleportBehaviour;

        /// <summary>Guid string id.</summary>
        public string Id => Subject != null ? Subject.Id : null;

        protected override void Awake()
        {
            base.Awake();
            if (teleportBehaviour == null)
            {
                teleportBehaviour = GetComponent<ITeleportBehaviour>();
                if (teleportBehaviour == null)
                    Utility.Log.Error("Teleport requires a component implementing ITeleportBehaviour", LogCategory.Interaction);
            }
        }

        /// <summary>Teleport the player onto this target.</summary>
        public void Teleport() => teleportBehaviour?.Teleport();

        /// <summary>Undo the teleport.</summary>
        public void Unexecute() => teleportBehaviour?.Unexecute();

        /// <summary>Record + execute a teleport as a command (used when skipping over steps).</summary>
        public void SkipForwards()
        {
            var args = ConstructTeleportArgs();
            var command = args.ToCommand();
            if (command != null)
            {
                command.Execute();
                Commands.CommandHistory.Instance.Record(command);
            }
        }

        private TeleportEventArgs ConstructTeleportArgs()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return new TeleportEventArgs(this, player, player.transform.position, player.transform.rotation);
        }

        /// <summary>Called by the teleport implementation when an object arrives on this target.</summary>
        public void OnObjectEntered(GameObject teleportedObject, Vector3 previousPosition, Quaternion previousRotation)
        {
            Publish(new TeleportEventArgs(this, teleportedObject, previousPosition, previousRotation));
        }
    }
}
