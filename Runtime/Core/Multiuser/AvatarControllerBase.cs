using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>High-level connection state.</summary>
    public enum ConnectionState
    {
        /// <summary>Not connected.</summary>
        Disconnected,
        /// <summary>Connection attempt in progress (legacy misspelling kept for migration parity).</summary>
        AtteptingConnection,
        /// <summary>Connected to the lobby.</summary>
        Connected
    }

    /// <summary>A connected user in a room.</summary>
    public class MultiuserPlayer
    {
        /// <summary>Transport-level user id.</summary>
        public int Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>True when the player is the session instructor/lead.</summary>
        public bool IsInstructor;
        /// <summary>Object-visible state (instructor-controlled).</summary>
        public bool IsActive = true;
        /// <summary>Audio-muted state.</summary>
        public bool IsMuted;
        /// <summary>Hidden from the user list.</summary>
        public bool IsHidden;
        /// <summary>Highlighted in the user list.</summary>
        public bool IsHighlighted;
        /// <summary>Being spot-checked by the instructor.</summary>
        public bool IsSpotChecked;
        /// <summary>Valve-name overlay enabled.</summary>
        public bool HasValveNamesActive;
        /// <summary>Platform identifier.</summary>
        public Data.PlayerPlatform Platform;

        /// <summary>Create a player.</summary>
        public MultiuserPlayer(int id, Data.PlayerPlatform platform, string name = "", bool instructor = false, bool isActive = true, bool isChecked = false)
        {
            Id = id;
            Platform = platform;
            Name = name;
            IsInstructor = instructor;
            IsActive = isActive;
            IsSpotChecked = isChecked;
        }
    }

    /// <summary>Instructor-facing per-user controls.</summary>
    public interface InstructorControls
    {
        /// <summary>Set a user's object-visible state.</summary>
        void SetActiveState(MultiuserPlayer player, bool targetState);
        /// <summary>Toggle a user's object-visible state.</summary>
        void ToggleActiveState(MultiuserPlayer player);
        /// <summary>Remove a user from the session.</summary>
        void Remove(MultiuserPlayer player);
        /// <summary>Set a user's audio-muted state.</summary>
        void SetAudioState(MultiuserPlayer player, bool state);
        /// <summary>Set a user's avatar-visible state.</summary>
        void SetAvatarState(MultiuserPlayer player, bool state);
        /// <summary>Toggle instructor spot-check on a user.</summary>
        void SetPlayerSpotCheckStatus(MultiuserPlayer player, bool state);
        /// <summary>Enable/disable valve-name overlays for a user.</summary>
        void SetValveNamesState(MultiuserPlayer player, bool state);
        /// <summary>Mute/unmute all users.</summary>
        void SetAudioStateAll(bool muted);
        /// <summary>Show/hide all avatars.</summary>
        void SetAvatarStateAll(bool visible);
        /// <summary>Set highlight state for a user.</summary>
        void SetHighlightState(MultiuserPlayer player, bool state);

        /// <summary>Fired when a user's active state changes.</summary>
        event Action<MultiuserPlayer, bool> OnActiveUserChanged;
        /// <summary>Fired when a user's audio state changes.</summary>
        event Action<MultiuserPlayer, bool> OnPlayerAudioStateChanged;
        /// <summary>Fired when a user's avatar state changes.</summary>
        event Action<MultiuserPlayer, bool> OnPlayerAvatarStateChanged;
        /// <summary>Fired when all users' audio state changes.</summary>
        event Action<bool> OnAudioStateAll;
        /// <summary>Fired when all users' avatar state changes.</summary>
        event Action<bool> OnAvatarStateAll;
    }


    /// <summary>Freeze/unfreeze behaviour attached to the local player rig.</summary>
    public abstract class IFreezeBehaviour
    {
        /// <summary>UI event systems disabled while frozen.</summary>
        public UnityEngine.EventSystems.EventSystem[] EventSystems { get; set; }

        /// <summary>Whether this user is the newly joining user.</summary>
        public bool IsJoiningUser { get; set; }

        /// <summary>Freeze the scene.</summary>
        public abstract void Freeze();

        /// <summary>Unfreeze the scene.</summary>
        public abstract void Unfreeze();

        /// <summary>Collects all event systems for toggling.</summary>
        protected IFreezeBehaviour()
        {
            EventSystems = UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        }
    }

    /// <summary>Arguments passed to a step's interaction handler (multiuser path).</summary>
    public class StepInteractionArguments
    {
        /// <summary>Step number these arguments apply to.</summary>
        public string StepNumber;
        /// <summary>Originating event args.</summary>
        public InteractionEventArgs EventArgs;
        /// <summary>Serialized payload for custom data.</summary>
        public string Payload;
    }

    /// <summary>Network payload for a single replicated event (legacy wire shape).</summary>
    [Serializable]
    public class EventSyncData
    {
        /// <summary>Subject id the event targets.</summary>
        public string SubjectId;
        /// <summary>Event type discriminator (class name).</summary>
        public string EventType;
        /// <summary>Json-serialized event payload.</summary>
        public string Payload;
        /// <summary>Actor id that produced the event.</summary>
        public string ActorId;
        /// <summary>Unix-ms timestamp of the event.</summary>
        public long Timestamp;
        /// <summary>Serialized bytes for transport.</summary>
        public byte[] Data;
    }

    /// <summary>Observes step lifecycle updates pushed over the network.</summary>
    public interface IFlowEventHandler
    {
        /// <summary>Progress update for the current step.</summary>
        void OnStepUpdate();
        /// <summary>A step wants to execute with interaction arguments.</summary>
        void OnStepExecute(StepInteractionArguments arguments);
        /// <summary>Skip to the next step.</summary>
        void OnSkipToNextStep();
        /// <summary>Skip to the previous step.</summary>
        void OnSkipToPreviousStep();
        /// <summary>Skip to a specific step number.</summary>
        void OnSkipToStep(int stepNumber);
    }

    /// <summary>UnityEvent carrying a string.</summary>
    [Serializable]
    public class UnityStringEvent : UnityEngine.Events.UnityEvent<string> { }

    /// <summary>UnityEvent carrying a <see cref="MultiuserPlayer"/>.</summary>
    [Serializable]
    public class UnityMultiuserEvent : UnityEngine.Events.UnityEvent<MultiuserPlayer> { }

    /// <summary>Static accessors for the current connection/room.</summary>
    public static class NetworkEvents
    {
        /// <summary>Fired when the local connection state changes.</summary>
        public static Action<ConnectionState> OnConnectionStateChanged;
        /// <summary>Fired when the instructor changes.</summary>
        public static Action<MultiuserPlayer> OnInstructorChanged;
    }
}

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>Helper extensions for <see cref="MultiuserPlayer"/>.</summary>
    public static class MultiuserPlayerUtility
    {
        /// <summary>Indicates if a multiuser player is the local user.</summary>
        public static bool IsLocal(this MultiuserPlayer player)
        {
            var local = NetworkManager.Instance?.CurrentRoom?.GetLocalPlayer;
            return player != null && local != null && player.Id == local.Id;
        }
    }
}

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>Base controller for a networked avatar.</summary>
    public abstract class AvatarControllerBase : MonoBehaviour
    {
        /// <summary>Player id of this avatar.</summary>
        public int playerId;

        /// <summary>Set avatar state.</summary>
        public abstract void SetAvatarState(bool state);

        /// <summary>Check if this avatar belongs to the local player.</summary>
        public abstract bool IsMine();

        /// <summary>Set audio state.</summary>
        public abstract void SetAudioState(bool state);

        /// <summary>Set the highlight state.</summary>
        public abstract void SetHighlightState(bool state);
    }
}
